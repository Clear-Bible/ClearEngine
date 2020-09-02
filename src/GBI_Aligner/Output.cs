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
            ArrayList links, // ArrayList(MappedGroup)
            ArrayList sourceWords, 
            ArrayList targetWords, 
            ref Alignment2 align, 
            int k, 
            Hashtable glossTable, 
            Hashtable groups
            )
        {
            Hashtable targetPositionMap = BuildTargetPositionMap(targetWords);

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
                Gloss g = (Gloss)glossTable[id];
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
            Hashtable primaryPositions = BuildPrimaryTable(groups);

            links = RemoveEmptyLinks(links);
            RestoreOriginalPositions(ref links, sourceWords);
            line.links = new List<Link>();
            for (int j = 0; j < links.Count; j++)
            {
                MappedGroup mappedGroup = (MappedGroup)links[j];

                int[] s = new int[mappedGroup.SourceNodes.Count];
                for (int i = 0; i < mappedGroup.SourceNodes.Count; i++)
                {
                    SourceNode sourceNode = (SourceNode)mappedGroup.SourceNodes[i];
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

        static Hashtable BuildTargetPositionMap(ArrayList targetWords)
        {
            Hashtable positionMap = new Hashtable();

            for (int i = 0; i < targetWords.Count; i++)
            {
                TargetWord targetWord = (TargetWord)targetWords[i];
                string tWord = targetWord.Text2;
                string targetID = targetWord.ID;
                if (!positionMap.ContainsKey(targetID))
                {
                    positionMap.Add(targetID, i);
                }
            }

            return positionMap;
        }

        static ArrayList RemoveEmptyLinks(ArrayList links)
        {
            ArrayList trueLinks = new ArrayList();

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

        static void RestoreOriginalPositions(ref ArrayList links, ArrayList sourceWords)
        {
            Hashtable positionTable = new Hashtable();

            for (int i = 0; i < sourceWords.Count; i++)
            {
                SourceWord sourceWord = (SourceWord)sourceWords[i];
                string id = sourceWord.ID;
                positionTable.Add(id, i);
            }

            for (int i = 0; i < links.Count; i++)
            {
                MappedGroup mappedGroup = (MappedGroup)links[i];
                ArrayList sourceNodes = mappedGroup.SourceNodes;
                for (int j = 0; j < sourceNodes.Count; j++)
                {
                    SourceNode sourceNode = (SourceNode)mappedGroup.SourceNodes[j];
                    string id = sourceNode.MorphID;
                    int position = (int)positionTable[id];
                    sourceNode.Position = position;
                }
            }
        }

        static Hashtable BuildPrimaryTable(Hashtable groups)
        {
            Hashtable primaryTable = new Hashtable();

            IDictionaryEnumerator groupEnum = groups.GetEnumerator();

            while (groupEnum.MoveNext())
            {
                ArrayList targetGroups = (ArrayList)groupEnum.Value;
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

        static ArrayList ReorderNodes(ArrayList targetNodes, Hashtable primaryPositions)
        {
            ArrayList targetNodes2 = new ArrayList();

            string targetText = GetTargetText(targetNodes);
            int primaryPosition = (int)primaryPositions[targetText];
            LinkedWord primaryWord = (LinkedWord)targetNodes[primaryPosition];
            targetNodes2.Add(primaryWord);
            targetNodes.Remove(primaryWord);
            foreach(LinkedWord lw in targetNodes)
            {
                targetNodes2.Add(lw);
            }

            return targetNodes2;
        }

        static string GetTargetText(ArrayList targetNodes)
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
