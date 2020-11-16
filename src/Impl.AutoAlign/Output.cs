using System;
using System.Collections.Generic;
using System.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;


    public class Output
    {
        public static void WriteAlignment(
            List<MappedGroup> links,
            List<SourceWord> sourceWords,
            List<TargetWord> targetWords,
            ref Alignment2 align,
            int k,
            Dictionary<string, Gloss> glossTable,
            GroupTranslationsTable_Old groups,
            Dictionary<string, WordInfo> wordInfoTable
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

            // Build map of target ID to position in source words array.
            Dictionary<string, int> positionTable =
                sourceWords.
                Select((sw, n) => new { sw.ID, n })
                .ToDictionary(
                    x => x.ID,
                    x => x.n);


            Line line = new Line()
            {
                manuscript = new Manuscript()
                {
                    words =
                        sourceWords
                        .Select(sourceWord =>
                            sourceWord.CreateManuscriptWord(
                                glossTable[sourceWord.ID],
                                wordInfoTable))
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
