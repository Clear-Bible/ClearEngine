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



            // Create the links element
            Dictionary<string, int> primaryPositions = BuildPrimaryTable(groups);
            // modified-target-group-text => primary-position

            links = GetLinksWithoutFakeWords(links);

            RestoreOriginalPositions(links, sourceWords);
            // Changes SourceNode.position to be the position in sourceWords.


            line.links = new List<Link>();

            foreach(MappedGroup mappedGroup in links)
            {
                int[] s =
                    mappedGroup.SourceNodes
                    .Select(sourceNode => sourceNode.Position)
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



        static List<MappedGroup> GetLinksWithoutFakeWords(List<MappedGroup> links)
        {
            return
                links
                .Where(mappedGroup =>
                    !mappedGroup.TargetNodes.Any(
                        linkedWord => linkedWord.Word.IsFake))
                .ToList();
        }


        static void RestoreOriginalPositions(
            List<MappedGroup> links,
            List<SourceWord> sourceWords)
        {
            Dictionary<string, int> positionTable =
                sourceWords.
                Select((sw, n) => new { sw.ID, n })
                .ToDictionary(
                    x => x.ID,
                    x => x.n);

            foreach (MappedGroup mappedGroup in links)
            {
                foreach (SourceNode sourceNode in mappedGroup.SourceNodes)
                {
                    sourceNode.Position =
                        positionTable[sourceNode.MorphID];
                }
            }
        }


        static Dictionary<string, int> BuildPrimaryTable(GroupTranslationsTable_Old groups)
        {
            Dictionary<string, int> primaryTable = new Dictionary<string, int>();

            foreach (GroupTranslations_Old groupTranslations in
                groups.AllEntries.Select(kvp => kvp.Value))
            {
                foreach (GroupTranslation_Old tg in groupTranslations.AllTranslations)
                {
                    string tgText = tg.TargetGroupAsText;
                    tgText = tgText.Replace(" ~ ", " ");
                    if (!primaryTable.ContainsKey(tgText))
                    {
                        primaryTable.Add(tgText, tg.PrimaryPosition);
                    }
                }
            }

            return primaryTable;
        }

        static List<LinkedWord> ReorderNodes(List<LinkedWord> targetNodes, Dictionary<string, int> primaryPositions)
        {
            List<LinkedWord> targetNodes2 = new List<LinkedWord>();

            string targetText = GetTargetText(targetNodes);
            int primaryPosition = primaryPositions[targetText];
            LinkedWord primaryWord = targetNodes[primaryPosition];
            targetNodes2.Add(primaryWord);
            targetNodes.Remove(primaryWord);
            foreach (LinkedWord lw in targetNodes)
            {
                targetNodes2.Add(lw);
            }

            return targetNodes2;
        }

        static string GetTargetText(List<LinkedWord> targetNodes)
        {
            string text = string.Empty;

            foreach (LinkedWord lw in targetNodes)
            {
                text += lw.Text + " ";
            }

            return text.Trim().ToLower();
        }
    }

}
