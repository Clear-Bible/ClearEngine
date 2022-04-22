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
        private static TranslationModel ToTranslationModel(Dictionary<string, Dictionary<string, double>>? transMod)
        {
            if (transMod == null)
            {
                throw new InvalidDataEngineException(message: "translation model assumption is null");
            }
            return new TranslationModel(transMod
                .Select(kv => KeyValuePair.Create(new SourceLemma(kv.Key), kv.Value
                    .Select(v => KeyValuePair.Create(new TargetLemma(v.Key), new Score(v.Value)))
                    .ToDictionary(x => x.Key, x => x.Value)
                ))
                .ToDictionary(x => x.Key, x => x.Value)
            );
        }
        private static AlignmentModel ToAlignmentModel(List<IReadOnlyCollection<TokensAlignedWordPair>>? alignMod)
        {    
            if (alignMod == null)
            {
                throw new InvalidDataEngineException(message: "alignment model assumption is null");
            }

            //{book:D2}{chapter:D3}{verse:D3}{word:D3}{subsegment:D1}
            return new AlignmentModel(alignMod
                .SelectMany(c => c
                    .Select(p => KeyValuePair.Create(
                        new BareLink(
                            p.SourceToken?.TokenId.ToSourceId() ?? throw new InvalidDataEngineException(message: "Can't create AlignmentModel: sourceToken is null"),
                            p.TargetToken?.TokenId.ToTargetId() ?? throw new InvalidDataEngineException(message: "Can't create AlignmentModel: targetToken is null")),
                        new Score(p.AlignmentScore)))
                     )
                     .ToDictionary(x => x.Key, x => x.Value)
               );
        }
        internal static IEnumerable<(TokenId sourceTokenId, TokenId targetTokenId, double score)> AlignZone(
            ParallelTextRow parallelTextRow, 
            IManuscriptTree manuscriptTree, 
            ManuscriptTreeWordAlignerParams hyperParameters, 
            IList<SmtModel> smtModels,
            int indexPrimarySmtModel
            )
        {
            int smtTcIndex = 0;
            if (smtModels.Count > 2)
                throw new InvalidConfigurationEngineException(message: "more than two smt's provided to AlignZone");
            if (smtModels.Count == 2)
                smtTcIndex = indexPrimarySmtModel == 0 ? 1 : 0;
            if (smtModels.Count == 1)
            {
                smtTcIndex = 0;
            }

            var assumptions = new AutoAlignAssumptions(
                ToTranslationModel(smtModels[indexPrimarySmtModel].TranslationModel),
                ToTranslationModel(smtModels[smtTcIndex].TranslationModel),
                hyperParameters.useLemmaCatModel,
                hyperParameters.manTransModel,
                ToAlignmentModel(smtModels[smtTcIndex].AlignmentModel),
                ToAlignmentModel(smtModels[indexPrimarySmtModel].AlignmentModel),
                hyperParameters.useAlignModel,
                hyperParameters.puncs,
                hyperParameters.stopWords,
                hyperParameters.goodLinks,
                hyperParameters.goodLinkMinCount,
                hyperParameters.badLinks,
                hyperParameters.badLinkMinCount,
                hyperParameters.oldLinks,
                hyperParameters.sourceFunctionWords,
                hyperParameters.targetFunctionWords,
                hyperParameters.contentWordsOnly,
                hyperParameters.strongs,
                hyperParameters.maxPaths
                );
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

            List<MonoLink> monoLinks = ZoneAlignment.GetMonoLinks(
                versesXElementCombined,
                ZoneAlignment.GetSourcePoints(versesXElementCombined),
                ZoneAlignment.GetTargetPoints(targets.ToList()),
                assumptions);

            return monoLinks
                .OrderBy(ml => ml.SourcePoint.SourceID.AsCanonicalString)
                .Select(ml => (ml.SourcePoint.SourceID.ToTokenId(), 
                    ml.TargetBond.TargetPoint.TargetID.ToTokenId(), 
                    ml.TargetBond.Score));
        }
    }
}
