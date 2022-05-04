using System.Xml.Linq;

using ClearBible.Engine.Corpora;

using SIL.Machine.Corpora;
using SIL.Scripture;

using ClearBible.Engine.TreeAligner.Legacy;
using ClearBible.Engine.TreeAligner.Translation;
using ClearBible.Engine.Translation;
using static ClearBible.Engine.Persistence.FileGetBookIds;
using ClearBible.Engine.Exceptions;

namespace ClearBible.Engine.TreeAligner.Adapter
{
    internal class ZoneAlignmentAdapter
    {
        internal static IEnumerable<(TokenId sourceTokenId, TokenId targetTokenId, double score)> AlignZone(
            ParallelTextRow parallelTextRow, 
            IManuscriptTree manuscriptTree, 
            ManuscriptTreeWordAlignerHyperparameters hyperParameters, 
            int indexPrimarySmtModel
            )
        {
            try
            {
                parallelTextRow.SourceRefs.Cast<VerseRef>();
            }
            catch (InvalidCastException)
            {
                throw new InvalidTypeEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes refs that are not VerseRefs");
            }

            var books = parallelTextRow.SourceRefs
                .Select(r => ((VerseRef)r).Book)
                .Distinct();
            if (books.Count() > 1)
            {
                throw new InvalidDataEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes ref to more than one book");
            }
            if (books.Count() == 0)
            {
                throw new InvalidDataEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes ref without a book");
            }

            var chapterNumbers = parallelTextRow.SourceRefs
                .Select(r => ((VerseRef)r).ChapterNum)
                .Distinct();
            if (chapterNumbers.Count() > 1)
            {
                throw new InvalidDataEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes ref to more than one chapterNum");
            }
            if (chapterNumbers.Count() == 0)
            {
                throw new InvalidDataEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes ref without a chapterNum");
            }

            var verseNumbers = parallelTextRow.SourceRefs
                .Select(r => ((VerseRef)r).VerseNum)
                .Distinct();

            XElement? versesXElementCombined = manuscriptTree.GetVersesXElementsCombined(books.First(), chapterNumbers.FirstOrDefault(), verseNumbers);

            if (versesXElementCombined == null)
            {
                throw new InvalidTreeEngineException($"versesXElementCombined is null", new Dictionary<string, string>
                    {
                        {"book", books.First() },
                        {"chapter", chapterNumbers.First().ToString()},
                        {"verses", string.Join(" ", verseNumbers)}
                    });
            }

            if (parallelTextRow is not EngineParallelTextRow)
            {
                throw new InvalidConfigurationEngineException(message: "ParallelTextRow supplied is not a EngineParallelTextRow, which is required for extracting target points.");
            }

            //FIXME: CHECK THIS!
            IEnumerable<Target>? targets = ((EngineParallelTextRow)parallelTextRow).TargetTokens
                ?.Select(t => new Target(new TargetText(t.Text), new TargetLemma(t.Text), t.TokenId.ToTargetId())) ?? null;
            if (targets == null)
            {
                throw new InvalidConfigurationEngineException(message: "ParallelTextRow targets must be transformed to a TargetTextRow (.Transform(textRow => new TokensTextRow(textRow))) ");
            }

            double totalTargetPoints = targets.Count();
            var targetPoints = targets
                .Select((target, position) => new
                {
                    text = target.TargetText.Text,
                    lemma = target.TargetLemma.Text,
                    targetID = target.TargetID,
                    position
                })
                .GroupBy(x => x.text)
                .SelectMany(group =>
                    group.Select((x, groupIndex) => new
                    {
                        x.text,
                        x.targetID,
                        x.lemma,
                        x.position,
                        altID = $"{x.text}-{groupIndex + 1}"
                    }))
                .OrderBy(x => x.position)
                .Select(x => new TargetPoint(
                    Text: x.text,
                    // Lower: x.text.ToLower(),
                    Lemma: x.lemma,
                    TargetID: x.targetID,
                    AltID: x.altID,
                    Position: x.position,
                    RelativePosition: x.position / totalTargetPoints));


            List<MonoLink> monoLinks = ZoneAlignment.GetMonoLinks(
                versesXElementCombined,
                versesXElementCombined.GetSourcePoints(),
                targetPoints,
                hyperParameters);

            return monoLinks
                .OrderBy(ml => ml.SourcePoint.SourceID.AsCanonicalString)
                .Select(ml => (ml.SourcePoint.SourceID.ToTokenId(), 
                    ml.TargetBond.TargetPoint.TargetID.ToTokenId(), 
                    ml.TargetBond.Score));
        }
    }
}
