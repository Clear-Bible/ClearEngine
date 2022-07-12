using System.Xml.Linq;

using ClearBible.Engine.Corpora;
using ClearBible.Engine.SyntaxTree.Aligner.Legacy;
using ClearBible.Engine.SyntaxTree.Aligner.Translation;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;

using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.SyntaxTree.Aligner.Adapter
{
    internal class ZoneAlignmentAdapter
    {

        private static BookChapterVerseXElements? LastChapterVerseXElements_ = null;

        internal static IEnumerable<(TokenId sourceTokenId, TokenId targetTokenId, double score)> AlignZone(
            ParallelTextRow parallelTextRow, 
            ISyntaxTree syntaxTree, 
            SyntaxTreeWordAlignerHyperparameters hyperParameters)
        {
            try
            {
                parallelTextRow.SourceRefs.Cast<VerseRef>().ToList();
            }
            catch (InvalidCastException)
            {
                throw new InvalidTypeEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes refs that are not VerseRefs");
            }

            var books = parallelTextRow.SourceRefs
                .Select(r => ((VerseRef)r).Book)
                .Distinct();
            if (books.Count() != 1)
            {
                throw new InvalidDataEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes ref to either no book or more than one book");
            }

            var chapterNumbers = parallelTextRow.SourceRefs
                .Select(r => ((VerseRef)r).ChapterNum)
                .Distinct();
            if (chapterNumbers.Count() != 1)
            {
                throw new InvalidDataEngineException(message: $"TreeAligner.Adapters.AlignZone received a ParallelTextRow with a source segment that includes ref to either no chapterNum or more than one chapterNum");
            }

            var verseNumbers = parallelTextRow.SourceRefs
                .Select(r => ((VerseRef)r).VerseNum)
                .Distinct() // this results in an unordered enumerable. sort so getversesXelement doesn't have chance to combine tree in non-deterministic way.
                .OrderBy(i => i);


            if ( LastChapterVerseXElements_ == null 
                || 
                (!LastChapterVerseXElements_.Book.Equals(books.First()) || ( LastChapterVerseXElements_?.ChapterNumber != chapterNumbers.First() )) )
            {
                LastChapterVerseXElements_ = syntaxTree.GetVerseXElementsForBookChapter(books.First(), chapterNumbers.First());

            }

            XElement? versesXElementCombined = syntaxTree.GetVersesXElementsCombined(LastChapterVerseXElements_, verseNumbers);

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
                ?.Select(t => new Target(new TargetText(t.Text), new TargetLemma(t.Text.ToUpper()), t.TokenId.ToTargetId())) ?? null;
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
                    RelativePosition: x.position / totalTargetPoints))
                .ToList();


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
