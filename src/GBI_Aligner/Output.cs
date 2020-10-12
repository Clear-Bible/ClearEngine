using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Xml;

using Utilities;
using Trees;

namespace GBI_Aligner
{
    class Output
    {
        public static void WriteAlignment(
            List<MappedGroup> links,
            List<SourceWord> sourceWords, 
            List<TargetWord> targetWords, 
            ref Alignment2 align, 
            int k, 
            Dictionary<string, Gloss> glossTable, 
            Dictionary<string, List<TargetGroup>> groups
            )
        {

            // Create line object
            Line line = new Line();

            // Create the manuscript/source element
            line.manuscript = new Manuscript();
            line.manuscript.words = new ManuscriptWord[sourceWords.Count];
            for (int i = 0; i < sourceWords.Count; i++)
            {
                SourceWord sourceWord = (SourceWord)sourceWords[i];
                ManuscriptWord mWord = new ManuscriptWord();
                string id = sourceWord.ID;
                mWord.id = long.Parse(id);
                mWord.altId = sourceWord.AltID;
                mWord.text = sourceWord.Text;
                mWord.lemma = sourceWord.Lemma;
                mWord.strong = sourceWord.Strong;
                mWord.pos = sourceWord.Cat;
                mWord.morph = sourceWord.Morph;
                Gloss g = glossTable[id];
                mWord.gloss = g.Gloss1;
                mWord.gloss2 = g.Gloss2;
                line.manuscript.words[i] = mWord;
            }

            // Create the target/translation element
            line.translation = new Translation();
            line.translation.words = new TranslationWord[targetWords.Count];
            for (int i = 0; i < targetWords.Count; i++)
            {
                TargetWord targetWord = (TargetWord)targetWords[i];
                TranslationWord tWord = new TranslationWord();
                string id = targetWord.ID;
                tWord.id = long.Parse(id);
                tWord.altId = targetWord.AltID;
                tWord.text = targetWord.Text2;
                line.translation.words[i] = tWord;
            }

            // Create the links element
            Dictionary<string, int> primaryPositions = BuildPrimaryTable(groups);

            links = RemoveEmptyLinks(links);
            RestoreOriginalPositions(links, sourceWords);
            line.links = new List<Link>();
            for (int j = 0; j < links.Count; j++)
            {
                MappedGroup mappedGroup = (MappedGroup)links[j];

                int[] s = new int[mappedGroup.SourceNodes.Count];
                for (int i = 0; i < mappedGroup.SourceNodes.Count; i++)
                {
                    SourceNode sourceNode = mappedGroup.SourceNodes[i];
                    s[i] = sourceNode.Position;
                }

                if (mappedGroup.TargetNodes.Count > 1)
                {
                    mappedGroup.TargetNodes = ReorderNodes(mappedGroup.TargetNodes, primaryPositions);
                }
                int[] t = new int[mappedGroup.TargetNodes.Count];

                for (int i = 0; i < mappedGroup.TargetNodes.Count; i++)
                {
                    LinkedWord linkedWord = (LinkedWord)mappedGroup.TargetNodes[i];
                    t[i] = linkedWord.Word.Position;
                }

                double score = 0.0;
                if (mappedGroup.SourceNodes.Count > 1 || mappedGroup.TargetNodes.Count > 1)
                {
                    score = 0.9;
                }
                else
                {
                    LinkedWord LinkedWord = (LinkedWord)mappedGroup.TargetNodes[0];
                    score = Math.Exp(LinkedWord.Prob);
                }

                line.links.Add(new Link(){source=s, target=t, cscore=score}); // initial score
            }

            align.Lines[k] = line;
        }

 

        static List<MappedGroup> RemoveEmptyLinks(List<MappedGroup> links)
        {
            List<MappedGroup> trueLinks = new List<MappedGroup>();

            foreach(MappedGroup mg in links)
            {
                if (IsTrueLink(mg))
                {
                    trueLinks.Add(mg);
                }
            }

            return trueLinks;
        }

        static bool IsTrueLink(MappedGroup mg)
        {
            bool isTrue = true;

            foreach(LinkedWord lw in mg.TargetNodes)
            {
                if (lw.Word.IsFake)
                {
                    isTrue = false;
                    break;
                }
            }

            return isTrue;
        }

        static void RestoreOriginalPositions(List<MappedGroup> links, List<SourceWord> sourceWords)
        {
            Hashtable positionTable = new Hashtable();

            for (int i = 0; i < sourceWords.Count; i++)
            {
                SourceWord sourceWord = sourceWords[i];
                string id = sourceWord.ID;
                positionTable.Add(id, i);
            }

            for (int i = 0; i < links.Count; i++)
            {
                MappedGroup mappedGroup = links[i];
                List<SourceNode> sourceNodes = mappedGroup.SourceNodes;
                for (int j = 0; j < sourceNodes.Count; j++)
                {
                    SourceNode sourceNode = mappedGroup.SourceNodes[j];
                    string id = sourceNode.MorphID;
                    int position = (int)positionTable[id];
                    sourceNode.Position = position;
                }
            }
        }

        static Dictionary<string, int> BuildPrimaryTable(Dictionary<string, List<TargetGroup>> groups)
        {
            Dictionary<string, int> primaryTable = new Dictionary<string, int>();

            foreach (var groupEnum in groups)
            {
                List<TargetGroup> targetGroups = groupEnum.Value;
                foreach (TargetGroup tg in targetGroups)
                {
                    string tgText = tg.Text;
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
            foreach(LinkedWord lw in targetNodes)
            {
                targetNodes2.Add(lw);
            }

            return targetNodes2;
        }

        static string GetTargetText(List<LinkedWord> targetNodes)
        {
            string text = string.Empty;

            foreach(LinkedWord lw in targetNodes)
            {
                text += lw.Text + " ";
            }

            return text.Trim().ToLower();
        }
    }
}
