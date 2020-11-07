﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Miscellaneous;

    public class Groups
    {
        public static void AlignGroups(
            List<MappedGroup> links,
            List<SourceWord> sourceWords,
            List<TargetWord> targetWords,
            GroupTranslationsTable_Old groups,
            List<XElement> terminals
            )
        {
            SourceWord[] sWords = BuildSourceArray(sourceWords);
            TargetWord[] tWords = BuildTargetArray(targetWords);
            List<string[][]> mappedGroups = GetGroupLinks(sWords, tWords, groups);
            if (mappedGroups.Count > 0)
            {
                RemoveOldLinks(mappedGroups, ref links);
                foreach (string[][] group in mappedGroups)
                {
                    AddGroup(group, links, terminals, targetWords);
                }
            }
        }


        static List<string[][]> GetGroupLinks(SourceWord[] sourceWords, TargetWord[] targetWords, GroupTranslationsTable_Old groups)
        {
            List<string[][]> mappedGroups = new List<string[][]>();

            for (int i = 0; i < sourceWords.Length; i++)
            {
                bool foundMatch = false;

                SourceWord sw = (SourceWord)sourceWords[i];
                if (sw.Lemma == "οὐ")
                {
                    ;
                }

                if (i < sourceWords.Length - 2)
                {
                    string trigram = sourceWords[i].Lemma + " " + sourceWords[i + 1].Lemma + " " + sourceWords[i + 2].Lemma;
                    string trigramIDs = sourceWords[i].ID + " " + sourceWords[i + 1].ID + " " + sourceWords[i + 2].ID;
                    if (groups.ContainsSourceGroupKey(trigram))
                    {
                        GroupTranslations_Old targetGroups = groups.TranslationsForSourceGroup(trigram);
                        string match = FindTargetMatch(targetWords, targetGroups, 1);
                        if (match != string.Empty)
                        {
                            string[] sourceLinks = trigramIDs.Split(" ".ToCharArray());
                            string[] targetLinks = match.Split(" ".ToCharArray());
                            string[][] group = new string[][] { sourceLinks, targetLinks };
                            mappedGroups.Add(group);
                            i += 2;
                            foundMatch = true;
                        }
                    }
                }
                if (!foundMatch && i < sourceWords.Length - 1)
                {
                    string bigram = sourceWords[i].Lemma + " " + sourceWords[i + 1].Lemma;
                    string bigramIDs = sourceWords[i].ID + " " + sourceWords[i + 1].ID;
                    if (groups.ContainsSourceGroupKey(bigram))
                    {
                        GroupTranslations_Old targetGroups = groups.TranslationsForSourceGroup(bigram);
                        string match = FindTargetMatch(targetWords, targetGroups, 1);
                        if (match != string.Empty)
                        {
                            string[] sourceLinks = bigramIDs.Split(" ".ToCharArray());
                            string[] targetLinks = match.Split(" ".ToCharArray());
                            string[][] group = new string[][] { sourceLinks, targetLinks };
                            mappedGroups.Add(group);
                            i += 1;
                            foundMatch = true;
                        }
                    }
                }
                if (!foundMatch)
                {
                    string unigram = sourceWords[i].Lemma;
                    string unigramIDs = sourceWords[i].ID;
                    if (groups.ContainsSourceGroupKey(unigram))
                    {
                        GroupTranslations_Old targetGroups = groups.TranslationsForSourceGroup(unigram);
                        string match = FindTargetMatch(targetWords, targetGroups, 2);
                        if (match != string.Empty)
                        {
                            string[] sourceLinks = unigramIDs.Split(" ".ToCharArray());
                            string[] targetLinks = match.Split(" ".ToCharArray());
                            string[][] group = new string[][] { sourceLinks, targetLinks };
                            mappedGroups.Add(group);
                            foundMatch = true;
                        }
                    }
                }
            }

            return mappedGroups;
        }

        static string FindTargetMatch(TargetWord[] targetWords, GroupTranslations_Old targetGroups, int minLength)
        {
            string match = string.Empty;

            bool isSplit = false;
            int maxRange;
            foreach (GroupTranslation_Old targetGroup in targetGroups.AllTranslations)
            {
                if (targetGroup.TargetGroupAsText.Contains("~"))
                {
                    isSplit = true;
                    targetGroup.TargetGroupAsText = targetGroup.TargetGroupAsText.Replace(" ~ ", " ");
                }

                string[] words = targetGroup.TargetGroupAsText.Split(" ".ToCharArray());
                if (isSplit) maxRange = words.Length * 2;
                else maxRange = words.Length;

                int wordIndex = 0;
                int currRange = 0;
                bool inRange = false;
                List<int> targetWordsInGroup = new List<int>();

                for (int i = 0; i < targetWords.Length && wordIndex < words.Length; i++)
                {
                    TargetWord targetWord = targetWords[i];
                    if (targetWord.InGroup) continue;
                    string word = words[wordIndex].Trim();
                    if (targetWord.Text == word)
                    {
                        match += targetWord.ID + " ";
                        wordIndex++;
                        inRange = true;
                        targetWordsInGroup.Add(i);
                    }
                    else
                    {
                        match = string.Empty;
                        targetWordsInGroup.Clear();
                        wordIndex = 0;
                        inRange = false;
                        currRange = 0;
                    }
                    if (inRange) currRange++;
                    if (currRange == maxRange) break;
                }
                if (words.Length == targetWordsInGroup.Count)
                {
                    SetInGroup2(targetWords, targetWordsInGroup);
                    break;
                }
                else
                {
                    match = string.Empty;
                }
            }

            return match.Trim();
        }



        static void SetInGroup2(TargetWord[] targetWords, List<int> targetWordsInGroup)
        {
            for (int i = 0; i < targetWords.Length; i++)
            {
                if (targetWordsInGroup.Contains(i))
                {
                    TargetWord targetWord = targetWords[i];
                    targetWord.InGroup = true;
                }
            }
        }



        static void RemoveOldLinks(List<string[][]> mappedGroups, ref List<MappedGroup> links)
        {
            List<MappedGroup> cleanLinks = new List<MappedGroup>();

            List<string> sourceWordsInGroups = GetSourceWordsInGroups(mappedGroups);
            List<string> targetWordsInGroups = GetTargetWordsInGroups(mappedGroups);

            foreach (MappedGroup mg in links)
            {
                if (!InGroup(mg, sourceWordsInGroups, targetWordsInGroups))
                {
                    cleanLinks.Add(mg);
                }
            }

            links = cleanLinks;
        }

        static List<string> GetSourceWordsInGroups(List<string[][]> mappedGroups)
        {
            List<string> wordsInGroups = new List<string>();

            foreach (string[][] mg in mappedGroups)
            {
                string[] sourceWords = mg[0];
                for (int i = 0; i < sourceWords.Length; i++)
                {
                    wordsInGroups.Add(sourceWords[i]);
                }
            }

            return wordsInGroups;
        }

        static List<string> GetTargetWordsInGroups(List<string[][]> mappedGroups)
        {
            List<string> wordsInGroups = new List<string>();

            foreach (string[][] mg in mappedGroups)
            {
                string[] sourceWords = mg[1];
                for (int i = 0; i < sourceWords.Length; i++)
                {
                    wordsInGroups.Add(sourceWords[i]);
                }
            }

            return wordsInGroups;
        }

        static bool InGroup(MappedGroup mg, List<string> sourceWordsInGroups, List<string> targetWordsInGroups)
        {
            bool inGroup = false;

            foreach (SourceNode sNode in mg.SourceNodes)
            {
                if (sourceWordsInGroups.Contains(sNode.MorphID))
                {
                    inGroup = true;
                }
            }

            if (inGroup == false)
            {
                foreach (LinkedWord tNode in mg.TargetNodes)
                {
                    if (targetWordsInGroups.Contains(tNode.Word.ID))
                    {
                        inGroup = true;
                    }
                }
            }

            return inGroup;
        }

        static void AddGroup(string[][] group, List<MappedGroup> links, List<XElement> terminals, List<TargetWord> targets)
        {
            string[] sourceWords = group[0];
            string[] targetWords = group[1];
            MappedGroup mg = new MappedGroup();
            AddSourceNodes(sourceWords, mg.SourceNodes, terminals);
            AddTargetNodes(targetWords, mg.TargetNodes, targets);
            links.Add(mg);
        }

        static void AddSourceNodes(string[] sourceWords, List<SourceNode> sourceNodes, List<XElement> terminals)
        {
            for (int i = 0; i < sourceWords.Length; i++)
            {
                SourceNode node = GetSourceNode(sourceWords[i], terminals);
                sourceNodes.Add(node);
            }
        }

        static SourceNode GetSourceNode(string id, List<XElement> terminals)
        {
            SourceNode sNode = new SourceNode();
            XElement treeNode = LocateTreeNode(id, terminals);
            
            sNode.MorphID = id;
            sNode.English = treeNode.Attribute("English").Value;
            sNode.Lemma = treeNode.Attribute("UnicodeLemma").Value;
            sNode.Position = treeNode.AttrAsInt("Start");
            sNode.RelativePos = (double)sNode.Position / (double)terminals.Count;
            sNode.Category = treeNode.Attribute("Cat").Value;
            sNode.TreeNode = treeNode;

            return sNode;
        }

        static XElement LocateTreeNode(string id, List<XElement> terminals)
        {
            XElement treeNode = null;

            foreach (XElement terminal in terminals)
            {
                if (id.StartsWith(terminal.Attribute("morphId").Value))
                {
                    treeNode = terminal;
                    break;
                }
            }

            return treeNode;
        }

        static void AddTargetNodes(string[] targetWords, List<LinkedWord> targetNodes, List<TargetWord> targets)
        {
            for (int i = 0; i < targetWords.Length; i++)
            {
                LinkedWord node = GetTargetNode(targetWords[i], targets);
                targetNodes.Add(node);
            }
        }

        static LinkedWord GetTargetNode(string id, List<TargetWord> targets)
        {
            LinkedWord lw = new LinkedWord();
            TargetWord tWord = LocateTargetword(id, targets);
            lw.Prob = 1.0;
            lw.Text = tWord.Text;
            lw.Word = tWord;

            return lw;
        }

        static TargetWord LocateTargetword(string id, List<TargetWord> targets)
        {
            TargetWord tw = null;

            foreach (TargetWord target in targets)
            {
                if (id == target.ID)
                {
                    tw = target;
                    break;
                }
            }

            return tw;
        }

        static SourceWord[] BuildSourceArray(List<SourceWord> sourceWords)
        {
            SourceWord[] sourceArray = new SourceWord[sourceWords.Count];

            int i = 0;

            foreach (SourceWord sw in sourceWords)
            {
                sourceArray[i] = sw;
                i++;
            }

            return sourceArray;
        }

        static TargetWord[] BuildTargetArray(List<TargetWord> targetWords)
        {
            TargetWord[] targetArray = new TargetWord[targetWords.Count];

            int i = 0;

            foreach (TargetWord tw in targetWords)
            {
                targetArray[i] = tw;
                i++;
            }

            return targetArray;
        }



        public static List<MappedGroup> WordsToGroups(List<MappedWords> wordLinks)
        {
            List<MappedGroup> groupLinks = new List<MappedGroup>();

            foreach (MappedWords wordLink in wordLinks)
            {
                MappedGroup groupLink = new MappedGroup();
                groupLink.SourceNodes.Add(wordLink.SourceNode);
                groupLink.TargetNodes.Add(wordLink.TargetNode);
                groupLinks.Add(groupLink);
            }

            return groupLinks;
        }
    }

}
