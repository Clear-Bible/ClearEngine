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
            Line line = new Line();

            line.manuscript = new Manuscript();

            line.manuscript.words =
                sourceWords
                .Select(sourceWord =>
                    sourceWord.CreateManuscriptWord(
                        glossTable[sourceWord.ID],
                        wordInfoTable))
                .ToArray();

            line.translation = new Translation();

            line.translation.words =
                targetWords
                .Select(targetWord => new TranslationWord()
                {
                    id = long.Parse(targetWord.ID),
                    altId = targetWord.AltID,
                    text = targetWord.Text2
                })
                .ToArray();


            Dictionary<string, int> primaryPositions =
                BuildPrimaryPositionTable(groups);

            // Get rid of fake links.
            links = 
                links
                .Where(mappedGroup =>
                    !mappedGroup.TargetNodes.Any(
                        linkedWord => linkedWord.Word.IsFake))
            .ToList();

            Dictionary<string, int> positionTable =
                sourceWords.
                Select((sw, n) => new { sw.ID, n })
                .ToDictionary(
                    x => x.ID,
                    x => x.n);


            line.links = new List<Link>();

            foreach(MappedGroup mappedGroup in links)
            {
                int[] s =
                    mappedGroup.SourceNodes
                    .Select(sourceNode =>
                        positionTable[sourceNode.MorphID])
                    .ToArray();

                if (mappedGroup.TargetNodes.Count > 1)
                {
                    mappedGroup.TargetNodes = ReorderNodes(
                        mappedGroup.TargetNodes,
                        primaryPositions);
                }

                int[] t =
                    mappedGroup.TargetNodes
                    .Select(linkedWord => linkedWord.Word.Position)
                    .ToArray();

                double score = 0.0;
                if (mappedGroup.SourceNodes.Count > 1 || mappedGroup.TargetNodes.Count > 1)
                {
                    score = 0.9;
                }
                else
                {
                    LinkedWord LinkedWord = mappedGroup.TargetNodes[0];
                    score = Math.Exp(LinkedWord.Prob);
                }

                line.links.Add(new Link() { source = s, target = t, cscore = score });
            }

            align.Lines[k] = line;
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


        static List<LinkedWord> ReorderNodes(
            List<LinkedWord> targetNodes,
            Dictionary<string, int> primaryPositions)
        {
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
