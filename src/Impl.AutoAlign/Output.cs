using System;
using System.Collections.Generic;
using System.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Impl.TreeService;

    public class Output
    {
        public static void WriteAlignment(
            List<MappedGroup> links,
            List<SourcePoint> sourcePoints,
            List<TargetWord> targetWords,
            ref Alignment2 align,
            int k,
            Dictionary<string, Gloss> glossTable,
            GroupTranslationsTable_Old groups
            )
        {
            // Build map of group key to position of primary
            // word within group.
            Dictionary<string, int> primaryPositions =
                BuildPrimaryPositionTable(groups);

            // Get rid of fake links.
            links =
                links
                .Where(mappedGroup =>
                    !mappedGroup.TargetNodes.Any(
                        linkedWord => linkedWord.Word.IsFake))
            .ToList();

            // Build map of source ID to position in source points list.
            Dictionary<string, int> positionTable =
                sourcePoints
                .ToDictionary(
                    sp => sp.SourceID.AsCanonicalString,
                    sp => sp.Position);

            Line line = new Line()
            {
                manuscript = new Manuscript()
                {
                    words =
                        sourcePoints
                        .Select(sp =>
                        {
                            string ID = sp.SourceID.AsCanonicalString;
                            Gloss gloss = glossTable[ID];

                            return new ManuscriptWord()
                            {
                                id = long.Parse(ID),
                                altId = sp.AltID,
                                text = sp.Terminal.Surface(),
                                lemma = sp.Terminal.Lemma(),
                                strong = sp.Terminal.Strong(),
                                pos = sp.Terminal.Category(),
                                morph = sp.Terminal.Analysis(),
                                gloss = gloss.Gloss1,
                                gloss2 = gloss.Gloss2
                            };
                        })
                        .ToArray()
                },

                translation = new Translation()
                {
                    words =
                        targetWords
                        .Select(targetWord => new TranslationWord()
                        {
                            id = long.Parse(targetWord.ID),
                            altId = targetWord.AltID,
                            text = targetWord.Text2
                        })
                        .ToArray()
                },

                links =
                    links
                    .Select(mappedGroup => new Link()
                    {
                        source =
                            mappedGroup.SourceNodes
                            .Select(sourceNode =>
                                positionTable[sourceNode.MorphID])
                            .ToArray(),

                        target =
                            WithPrimaryWordFirst(
                                mappedGroup.TargetNodes,
                                primaryPositions)
                            .Select(linkedWord => linkedWord.Word.Position)
                            .ToArray(),

                        cscore =
                            isNotOneToOne(mappedGroup)
                            ? 0.9
                            : Math.Exp(mappedGroup.TargetNodes[0].Prob)
                    })
                    .ToList()
            };

            align.Lines[k] = line;

            bool isNotOneToOne(MappedGroup mappedGroup) =>
                mappedGroup.SourceNodes.Count > 1 ||
                mappedGroup.TargetNodes.Count > 1;
        }


        static Dictionary<string, int> BuildPrimaryPositionTable(
            GroupTranslationsTable_Old groups)
        {
            return
                groups.AllEntries
                .Select(kvp => kvp.Value)
                .SelectMany(groupTranslations =>
                    groupTranslations.AllTranslations
                    .Select(tg => new
                    {
                        text = tg.TargetGroupAsText.Replace(" ~ ", " "),
                        position = tg.PrimaryPosition
                    }))
                .GroupBy(x => x.text)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().position);
        }


        static List<LinkedWord> WithPrimaryWordFirst(
            List<LinkedWord> targetNodes,
            Dictionary<string, int> primaryPositions)
        {
            if (targetNodes.Count <= 1) return targetNodes;

            string groupKey =
                string.Join(
                    " ",
                    targetNodes.Select(lw => lw.Text))
                .Trim()
                .ToLower();

            LinkedWord primaryWord = targetNodes[primaryPositions[groupKey]];

            return
                Enumerable.Empty<LinkedWord>()
                .Append(primaryWord)
                .Concat(
                    targetNodes
                    .Where(lw => lw != primaryWord))
                .ToList();
        }
    }

}
