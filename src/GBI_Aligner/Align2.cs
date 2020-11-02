using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections;

using Utilities;
using Trees;

using Newtonsoft.Json;
using System.IO;

using ClearBible.Clear3.Impl.Data;

namespace GBI_Aligner
{
    public class Align2
    {
        public static List<MappedWords> AlignTheRest(
            Candidate topCandidate,
            List<XmlNode> terminals, 
            int numberSourceWords,
            List<TargetWord> targetWords,
            TranslationModel_Old model, 
            Dictionary<string, string> preAlignment, // (bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            List<string> sourceFuncWords, 
            List<string> targetFuncWords,
            bool contentWordsOnly
            )
        {
            //Console.WriteLine("\nAlignTheRest\n\n");

            List<LinkedWord> linkedWords = new List<LinkedWord>();
            GetLinkedWords(topCandidate.Chain, linkedWords, topCandidate.Prob);

            // linkedWords has a LinkedWord for each target word found in
            // topCandidate.Sequence.  There is a LinkedWord datum with a dummy
            // TargetWord for zero-length sub-paths in topCandidate.sequence.

            List<MappedWords> links = new List<MappedWords>();
            for (int i = 0; i < terminals.Count; i++)
            {
                XmlNode terminal = (XmlNode)terminals[i];
                SourceNode sourceLink = new SourceNode();
                sourceLink.MorphID = Utils.GetAttribValue(terminal, "morphId");
                sourceLink.English = Utils.GetAttribValue(terminal, "English");
                sourceLink.Lemma = Utils.GetAttribValue(terminal, "UnicodeLemma");
                sourceLink.Category = Utils.GetAttribValue(terminal, "Cat");
                sourceLink.Position = Int32.Parse(Utils.GetAttribValue(terminal, "Start"));
                sourceLink.RelativePos = (double)sourceLink.Position / (double)numberSourceWords;
                if (sourceLink.MorphID.Length == 11) sourceLink.MorphID += "1";
                sourceLink.TreeNode = terminal;
                LinkedWord targetLink = linkedWords[i];
                // (looks like linkedWords and terminals are expected to be
                // in 1-to-1 correspondence.)
                MappedWords link = new MappedWords();
                link.SourceNode = sourceLink;
                link.TargetNode = targetLink;
                links.Add(link);
            }
            

            List<List<MappedWords>> conflicts = FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                ResolveConflicts(conflicts, links, 1);
            }



            #region Andi does not use this part anymore.

            List<string> linkedTargets = GetLinkedTargets(links);


            Dictionary<string, MappedWords> linksTable = CreateLinksTable(links);

            for (int i = 0; i < links.Count; i++)
            {
                MappedWords link = (MappedWords)links[i];

                if (link.TargetNode.Word.IsFake)
                {
                    AlignWord(ref link, targetWords, linksTable, linkedTargets, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);
                }
            }

            conflicts = FindConflictingLinks(links);

            if (conflicts.Count > 0)
            {
                ResolveConflicts(conflicts, links, 2);
            }

            #endregion



            return links;
        }


        public static void AlignWord(
            ref MappedWords link, // (target word is fake)
            List<TargetWord> targetWords,
            Dictionary<string, MappedWords> linksTable,  // source morphId => MappedWords, non-fake
            List<string> linkedTargets, // target word IDs from non-fake words
            TranslationModel_Old model, // translation model, (source => (target => probability))
            Dictionary<string, string> preAlignment, // (bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount, 
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly
            )
        {
            if (stopWords.Contains(link.SourceNode.Lemma)) return;
            if (contentWordsOnly && sourceFuncWords.Contains(link.SourceNode.Lemma)) return;
            if (useAlignModel && preAlignment.ContainsKey(link.SourceNode.MorphID))
            {
                string targetID = (string)preAlignment[link.SourceNode.MorphID];
                if (linkedTargets.Contains(targetID))
                {
                    return;
                }
                string targetWord = GetTargetWordTextFromID(targetID, targetWords);
                string pair = link.SourceNode.Lemma + "#" + targetWord;
                if (stopWords.Contains(link.SourceNode.Lemma) && !goodLinks.ContainsKey(pair))
                {
                    return;
                }
                if (!(badLinks.ContainsKey(pair) || puncs.Contains(targetWord) || stopWords.Contains(targetWord)))
                {
                    link.TargetNode.Text = targetWord;
                    link.TargetNode.Prob = 0;
                    link.TargetNode.Word.ID = targetID;
                    link.TargetNode.Word.IsFake = false;
                    link.TargetNode.Word.Text = targetWord;
                    link.TargetNode.Word.Position = GetTargetPositionFromID(targetID, targetWords);
                    return;
                }
            }

            bool stopped = false;
            List<MappedWords> linkedSiblings = GetLinkedSiblings(link.SourceNode.TreeNode, linksTable, ref stopped);

            if (linkedSiblings.Count > 0)
            {
                MappedWords preNeighbor = GetPreNeighbor(link, linkedSiblings);
                MappedWords postNeighbor = GetPostNeighbor(link, linkedSiblings);
                List<TargetWord> targetCandidates = new List<TargetWord>();
                bool foundTarget = false;
                if (!(preNeighbor == null || postNeighbor == null))
                {
                    targetCandidates = GetTargetCandidates(preNeighbor, postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    if (targetCandidates.Count > 0)
                    {
                        LinkedWord newTarget = GetTopCandidate(link.SourceNode, targetCandidates, model, linkedTargets, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount);
                        if (newTarget != null)
                        {
                            link.TargetNode = newTarget;
                            foundTarget = true;
                        }
                    }
                }
                else if (preNeighbor != null && !foundTarget)
                {
                    targetCandidates = GetTargetCandidates(preNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    if (targetCandidates.Count > 0)
                    {
                        LinkedWord newTarget = GetTopCandidate(link.SourceNode, targetCandidates, model, linkedTargets, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount);
                        if (newTarget != null)
                        {
                            link.TargetNode = newTarget;
                            foundTarget = true;
                        }
                    }
                }
                else if (postNeighbor != null && !foundTarget)
                {
                    targetCandidates = GetTargetCandidates(postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    if (targetCandidates.Count > 0)
                    {
                        LinkedWord newTarget = GetTopCandidate(link.SourceNode, targetCandidates, model, linkedTargets, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount);
                        if (newTarget != null)
                        {
                            link.TargetNode = newTarget;
                        }
                    }
                }
                
            }
        }


        static string GetTargetWordTextFromID(string targetID, List<TargetWord> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Text)
                .DefaultIfEmpty("")
                .First();
        }


        static int GetTargetPositionFromID(string targetID, List<TargetWord> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Position)
                .DefaultIfEmpty(0)
                .First();
        }


        static List<TargetWord> GetTargetCandidates(MappedWords postNeighbor, List<TargetWord> targetWords, List<string> linkedTargets, List<string> puncs, List<string> targetFuncWords, bool contentWordsOnly)
        {
            List<TargetWord> candidates = new List<TargetWord>();

            int anchorPosition = postNeighbor.TargetNode.Word.Position;

            for (int i = anchorPosition - 1; i >= 0 && i >= anchorPosition - 3; i--)
            {
                TargetWord targetWord = targetWords[i];
                string word = targetWord.Text;
                string id = targetWord.ID;
                if (contentWordsOnly && targetFuncWords.Contains(word)) continue;
                if (linkedTargets.Contains(word)) continue;
                if (puncs.Contains(word)) break;
                TargetWord tWord = new TargetWord();
                tWord.ID = id;
                tWord.Position = i;
                tWord.Text = word;
                candidates.Add(tWord);
            }
            for (int i = anchorPosition + 1; i < targetWords.Count && i <= anchorPosition + 3; i++)
            {
                TargetWord targetWord = targetWords[i];
                string word = targetWord.Text;
                string id = targetWord.ID;
                if (contentWordsOnly && targetFuncWords.Contains(word)) continue;
                if (linkedTargets.Contains(word)) continue;
                if (puncs.Contains(word)) break;
                TargetWord tWord = new TargetWord();
                tWord.ID = id;
                tWord.Position = i;
                tWord.Text = word;
                candidates.Add(tWord);
            }

            return candidates;
        }


        static List<TargetWord> GetTargetCandidates(MappedWords preNeighbor, MappedWords postNeighbor, List<TargetWord> targetWords, List<string> linkedTargets, List<string> puncBounds, List<string> targetFuncWords, bool contentWordsOnly)
        {
            List<TargetWord> candidates = new List<TargetWord>();

            int startPosition = preNeighbor.TargetNode.Word.Position;
            int endPosition = postNeighbor.TargetNode.Word.Position;

            for (int i = startPosition; i >= 0 && i < targetWords.Count && i < endPosition; i++)
            {
                if (i == startPosition) continue;
                if (i == endPosition) continue;
                TargetWord targetWord = targetWords[i];
                string word = targetWord.Text;
                string id = targetWord.ID;
                if (contentWordsOnly && targetFuncWords.Contains(word)) continue;
                if (linkedTargets.Contains(word)) continue;
                if (puncBounds.Contains(word)) break;
                TargetWord tWord = new TargetWord();
                tWord.ID = id;
                tWord.Position = i;
                tWord.Text = word;
                candidates.Add(tWord);
            }

            return candidates;
        }


        static List<MappedWords> GetLinkedSiblings(
            XmlNode treeNode,
            Dictionary<string, MappedWords> linksTable, // key is source morphId
            ref bool stopped)
        {
            List<MappedWords> linkedSiblings = new List<MappedWords>();

            if (treeNode.ParentNode == null || treeNode.ParentNode.Name == "Tree") stopped = true;

            while (!stopped && treeNode.ParentNode != null && linkedSiblings.Count == 0)
            {
                foreach(XmlNode childNode in treeNode.ParentNode.ChildNodes)
                {
                    if (childNode != treeNode)
                    {
                        List<XmlNode> terminals = Terminals.GetTerminalXmlNodes(childNode);
                        foreach(XmlNode terminal in terminals)
                        {
                            string morphID = Utils.GetAttribValue(terminal, "morphId");
                            if (morphID.Length == 11) morphID += "1";
                            if (linksTable.ContainsKey(morphID))
                            {
                                MappedWords map = linksTable[morphID];
                                linkedSiblings.Add(map);
                            }
                        }
                    }
                }

                if (linkedSiblings.Count == 0)
                {
                    linkedSiblings = GetLinkedSiblings(treeNode.ParentNode, linksTable, ref stopped);
                }

            }

            return linkedSiblings;
        }


        static MappedWords GetPreNeighbor(MappedWords unLinked, List<MappedWords> linkedSiblings)
        {
            MappedWords preNeighbor = null;

            int startPosition = Int32.Parse(Utils.GetAttribValue(unLinked.SourceNode.TreeNode, "Start"));
            int currDistance = 100;

            foreach(MappedWords map in linkedSiblings)
            {
                int position = Int32.Parse(Utils.GetAttribValue(map.SourceNode.TreeNode, "End"));
                if (position < startPosition)
                {
                    if (preNeighbor == null)
                    {
                        preNeighbor = map;
                        currDistance = startPosition - position;
                    }
                    else if ((startPosition - position) < currDistance)
                    {
                        preNeighbor = map;
                    }
                }
            }

            return preNeighbor;
        }


        static MappedWords GetPostNeighbor(MappedWords unLinked, List<MappedWords> linkedSiblings)
        {
            MappedWords postNeighbor = null;

            int endPosition = Int32.Parse(Utils.GetAttribValue(unLinked.SourceNode.TreeNode, "End"));

            foreach (MappedWords map in linkedSiblings)
            {
                int position = Int32.Parse(Utils.GetAttribValue(map.SourceNode.TreeNode, "End"));
                if (position > endPosition)
                {
                    postNeighbor = map;
                    break;
                }
            }

            return postNeighbor;
        }



        // replaces some members of links with a special non-link MappedWords datum
        //
        public static void ResolveConflicts(
            List<List<MappedWords>> conflicts,
            List<MappedWords> links,
            int pass)
        {
            List<MappedWords> linksToRemove = conflicts.
                SelectMany(conflict =>
                    conflict.Except(
                        FindWinners(conflict, pass).Where((_, n) => n == 0)))
                .ToList();

            for (int i = 0; i < links.Count; i++)
            {
                MappedWords link = links[i];
                if (linksToRemove.Contains(link))
                {
                    links[i] = new MappedWords
                    {
                        SourceNode = link.SourceNode,
                        TargetNode = new LinkedWord()
                        {
                            Prob = -1000,
                            Word = Align.CreateFakeTargetWord()
                        }
                    };
                }
            }
        }


        public static List<string> GetLinkedTargets(List<MappedWords> links)
        {
            return links
                .Where(mw => !mw.TargetNode.Word.IsFake)
                .Select(mw => mw.TargetNode.Word.ID)
                .ToList();
        }


        public static void GetLinkedWords(ArrayList path, List<LinkedWord> links, double prob)
        {
            ArrayList words = new ArrayList();

            if (path.Count == 0)
            {
                links.Add(new LinkedWord()
                {
                    Word = new TargetWord
                    {
                        Text = string.Empty,
                        Position = -1,
                        IsFake = true,
                        ID = "0"
                    },
                    Prob = -1000,
                    Text = string.Empty
                });
            }
            else
            {
                if (path[0] is Candidate)
                {
                    foreach (Candidate c in path)
                    {
                        GetLinkedWords(c.Chain, links, c.Prob);
                    }
                }
                else
                {
                    foreach (TargetWord tWord in path)
                    {
                        links.Add(new LinkedWord()
                        {
                            Word = tWord,
                            Prob = prob,
                            Text = tWord.Text
                        });
                    }
                }
            }
        }

        
        public static List<List<MappedWords>> FindConflictingLinks(List<MappedWords> links)
        {
            return links
                .Where(link => link.TargetNode.Word.Text != string.Empty)
                .GroupBy(link =>
                    $"{link.TargetNode.Word.Text}-{link.TargetNode.Word.ID}")
                .Where(group => group.Count() > 1)
                .Select(group => group.ToList())
                .ToList();
        }

 
  
        static List<MappedWords> FindWinners(List<MappedWords> conflict, int pass)
        {
            double prob(MappedWords mw) => mw.TargetNode.Prob;

            double distance(MappedWords mw) =>
                Math.Abs(mw.SourceNode.RelativePos -
                         mw.TargetNode.Word.RelativePos);

            // We know that conflict is not the empty list.

            double bestProb = conflict.Max(mw => prob(mw));           

            List<MappedWords> winners = conflict
                .Where(mw => mw.TargetNode.Prob == bestProb)
                .ToList();

            if (pass == 2 && winners.Count > 1)
            {
                double minDistance = conflict.Min(mw => distance(mw));

                MappedWords winner2 = winners
                    .Where(mw => distance(mw) == minDistance)
                    .FirstOrDefault();

                if (winner2 != null)
                {
                    winners = new List<MappedWords>();
                    winners.Add(winner2);
                }
            }

            return winners;
        }


        public static Dictionary<string, MappedWords> CreateLinksTable(List<MappedWords> links)
        {
            return links
                .Where(mw => !mw.TargetNode.Word.IsFake)
                .ToDictionary(mw => mw.SourceNode.MorphID, mw => mw);
        }


        public static string GetTargetID (string target)
        {
            return target.Substring(target.LastIndexOf("_") + 1);
        }

        static LinkedWord GetTopCandidate(
            SourceNode sWord, 
            List<TargetWord> tWords, 
            TranslationModel_Old model, 
            List<string> linkedTargets, 
            List<string> puncs, 
            List<string> stopWords, 
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks, 
            int badLinkMinCount
            )
        {
            Dictionary<TargetWord, double> probs = new Dictionary<TargetWord, double>();

            for (int i = 0; i < tWords.Count; i++)
            {
                TargetWord tWord = (TargetWord)tWords[i];
                string link = sWord.Lemma + "#" + tWord.Text;
                if (badLinks.ContainsKey(link) && (int)badLinks[link] >= badLinkMinCount)
                {
                    continue;
                }
                if (puncs.Contains(tWord.Text)) continue;
                if (stopWords.Contains(tWord.Text)) continue;
                if (stopWords.Contains(sWord.Lemma) && !(goodLinks.ContainsKey(link) && (int)goodLinks[link] >= goodLinkMinCount))
                {
                    continue;
                }

                if (linkedTargets.Contains(tWord.ID)) continue;

                if (model.ContainsSourceLemma(sWord.Lemma))
                {
                    Translations translations = model.TranslationsForSourceLemma(sWord.Lemma);
                    if (translations.ContainsTargetText(tWord.Text))
                    {
                        double prob = translations.ScoreForTargetText(tWord.Text);
                        if (prob >= 0.17)
                        {
                            probs.Add(tWord, Math.Log(prob));
                        }
                    }
                }
            }

            if (probs.Count > 0)
            {
                List<TargetWord> candidates = Data.SortWordCandidates(probs);

                TargetWord topCandidate = candidates[0];
                topCandidate.IsFake = false;

                LinkedWord linkedWord = new LinkedWord();
                linkedWord.Prob = probs[topCandidate];
                linkedWord.Text = topCandidate.Text;
                linkedWord.Word = topCandidate;
                return linkedWord;
            }

            return null;
        }

        public static void FixCrossingLinks(ref List<MappedGroup> links)
        {
            Dictionary<string, List<MappedGroup>> uniqueLemmaLinks =
                GetUniqueLemmaLinks(links);
            List<CrossingLinks> crossingLinks = IdentifyCrossingLinks(uniqueLemmaLinks);
            SwapTargets(crossingLinks, links);
        }
        

        // lemma => list of MappedGroup
        // where the MappedGroup has just one source and target node
        static Dictionary<string, List<MappedGroup>> GetUniqueLemmaLinks(List<MappedGroup> links)
        {
            return links
                .Where(link =>
                    link.SourceNodes.Count == 1 && link.TargetNodes.Count == 1)
                .GroupBy(link =>
                    link.SourceNodes[0].Lemma)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());
        }

        static List<CrossingLinks> IdentifyCrossingLinks(Dictionary<string, List<MappedGroup>> uniqueLemmaLinks)
        {
            return uniqueLemmaLinks.Values
                .Where(links => links.Count == 2 && Crossing(links))
                .Select(links => new CrossingLinks()
                {
                    Link1 = links[0],
                    Link2 = links[1]
                })
                .ToList();
        }

        static bool Crossing(List<MappedGroup> links)
        {
            MappedGroup link1 = links[0];
            MappedGroup link2 = links[1];
            SourceNode sWord1 = link1.SourceNodes[0];
            LinkedWord tWord1 = link1.TargetNodes[0];
            SourceNode sWord2 = link2.SourceNodes[0];
            LinkedWord tWord2 = link2.TargetNodes[0];
            if (tWord1.Word.Position < 0 || tWord2.Word.Position < 0) return false;
            if ( (sWord1.Position < sWord2.Position && tWord1.Word.Position > tWord2.Word.Position)
               || (sWord1.Position > sWord2.Position && tWord1.Word.Position < tWord2.Word.Position)
               )
            {
                return true;
            }

            return false;
        }

        static void SwapTargets(List<CrossingLinks> crossingLinks, List<MappedGroup> links)
        {
            for (int i = 0; i < crossingLinks.Count; i++)
            {
                CrossingLinks cl = crossingLinks[i];
                SourceNode sNode1 = cl.Link1.SourceNodes[0];
                SourceNode sNode2 = cl.Link2.SourceNodes[0];
                List<LinkedWord> TargetNodes0 = cl.Link1.TargetNodes;
                cl.Link1.TargetNodes = cl.Link2.TargetNodes;
                cl.Link2.TargetNodes = TargetNodes0;
                for(int j = 0; j < links.Count; j++)
                {
                    MappedGroup mp = links[j];
                    SourceNode sNode = (SourceNode)mp.SourceNodes[0];
                    if (sNode.MorphID == sNode1.MorphID) mp.TargetNodes = cl.Link1.TargetNodes;
                    if (sNode.MorphID == sNode2.MorphID) mp.TargetNodes = cl.Link2.TargetNodes;
                }
            }
        }
    }

    public class SourceNode
    {
        public string MorphID;
        public string Lemma;
        public string English;
        public XmlNode TreeNode;
        public int Position;
        public double RelativePos;
        public string Category;
    }

    public class LinkedWord
    {
        public TargetWord Word;
        public string Text;
        public double Prob;
    }

    public class MappedWords
    {
        public SourceNode SourceNode;
        public LinkedWord TargetNode;
    }

    public class MappedGroup
    {
        public List<SourceNode> SourceNodes = new List<SourceNode>();
        public List<LinkedWord> TargetNodes = new List<LinkedWord>();
    }

    public class CrossingLinks
    {
        public MappedGroup Link1;
        public MappedGroup Link2;
    }
}
