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

namespace GBI_Aligner
{
    class Align2
    {
        // returns ArrayList(MappedWords)
        //
        public static ArrayList AlignTheRest(
            Candidate topCandidate,
            ArrayList terminals, // :: ArrayList(XmlNode)
            string[] sourceWords, // lemmas
            string[] targetWords,  // lowercased tokens        
            Hashtable model, // translation model, Hashtable(source => Hashtable(target => probability))
            Hashtable preAlignment, // Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            ArrayList puncs,
            ArrayList stopWords,
            Hashtable goodLinks,
            int goodLinkMinCount,
            Hashtable badLinks,
            int badLinkMinCount,
            ArrayList sourceFuncWords, 
            ArrayList targetFuncWords,
            bool contentWordsOnly
            )
        {
            //Console.WriteLine("\nAlignTheRest\n\n");

            List<LinkedWord> linkedWords = new List<LinkedWord>();
            GetLinkedWords(topCandidate.Sequence, linkedWords, topCandidate.Prob);

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
                sourceLink.RelativePos = (double)sourceLink.Position / (double)sourceWords.Length;
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

            return new ArrayList(links);
        }


        static void AlignWord(
            ref MappedWords link, // output goes here
            string[] targetWords,
            Dictionary<string, MappedWords> linksTable,
            List<string> linkedTargets, 
            Hashtable model, // translation model, Hashtable(source => Hashtable(target => probability))
            Hashtable preAlignment, // Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            ArrayList puncs,
            ArrayList stopWords,
            Hashtable goodLinks,
            int goodLinkMinCount,
            Hashtable badLinks,
            int badLinkMinCount, 
            ArrayList sourceFuncWords,
            ArrayList targetFuncWords,
            bool contentWordsOnly
            )
        {
            // Console.WriteLine("AlignWord");
            // TimUtil.PrintAsJson("link", link);
            // Console.WriteLine("\n");

            if (stopWords.Contains(link.SourceNode.Lemma)) return;
            if (contentWordsOnly && sourceFuncWords.Contains(link.SourceNode.Lemma)) return;
            if (useAlignModel && preAlignment.ContainsKey(link.SourceNode.MorphID))
            {
                string targetID = (string)preAlignment[link.SourceNode.MorphID];
                if (linkedTargets.Contains(targetID))
                {
                    return;
                }
                string targetWord = GetTargetWord(targetID, targetWords);
                string pair = link.SourceNode.Lemma + "#" + targetWord;
                if (stopWords.Contains(link.SourceNode.Lemma) && !goodLinks.Contains(pair))
                {
                    return;
                }
                if (!(badLinks.Contains(pair) || puncs.Contains(targetWord) || stopWords.Contains(targetWord)))
                {
                    link.TargetNode.Text = targetWord;
                    link.TargetNode.Prob = 0;
                    link.TargetNode.Word.ID = targetID;
                    link.TargetNode.Word.IsFake = false;
                    link.TargetNode.Word.Text = targetWord;
                    link.TargetNode.Word.Position = GetPosition(targetID, targetWords);
                    return;
                }
            }
//            string cat = link.SourceNode.Category;
//            if (cat == "det" || cat == "conj" || cat == "art" || cat == "cj" || cat == "pron" || cat == "prep") return;
            bool stopped = false;
            ArrayList linkedSiblings = GetLinkedSiblings(link.SourceNode.TreeNode, linksTable, ref stopped);
            // linkedSiblings :: ArrayList(MappedWords)
            if (linkedSiblings.Count > 0)
            {
                MappedWords preNeighbor = GetPreNeighbor(link, linkedSiblings);
                MappedWords postNeighbor = GetPostNeighbor(link, linkedSiblings);
                ArrayList targetCandidates = new ArrayList();
                bool foundTarget = false;
                if (!(preNeighbor == null || postNeighbor == null))
                {
                    targetCandidates = GetTargetCandidates(preNeighbor, postNeighbor, targetWords, linkedTargets, puncs, targetFuncWords, contentWordsOnly);
                    // targetCandidates :: ArrayList(TargetWord)
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

        static string GetTargetWord(string targetID, string[] targetWords)
        {
            string targetWord = string.Empty;

            for (int i = 0; i < targetWords.Length; i++)
            {
                if (targetID == targetWords[i].Substring(targetWords[i].LastIndexOf("_") + 1))
                {
                    targetWord = targetWords[i].Substring(0, targetWords[i].LastIndexOf("_"));
                }
            }

            return targetWord;
        }

        static int GetPosition(string targetID, string[] targetWords)
        {
            int position = 0;

            for (int i = 0; i < targetWords.Length; i++)
            {
                if (targetID == targetWords[i].Substring(targetWords[i].LastIndexOf("_") + 1))
                {
                    return i;
                }
            }

            return position;
        }

        static ArrayList GetTargetCandidates(MappedWords postNeighbor, string[] targetWords, List<string> linkedTargets, ArrayList puncs, ArrayList targetFuncWords, bool contentWordsOnly)
        {
            ArrayList candidates = new ArrayList();

            int anchorPosition = postNeighbor.TargetNode.Word.Position;

            for (int i = anchorPosition - 1; i >= 0 && i >= anchorPosition - 3; i--)
            {
                string targetWord = targetWords[i];
                if (contentWordsOnly && targetFuncWords.Contains(targetWord)) continue;
                string word = Data.GetWord(targetWord);
                string id = GetTargetID(targetWord);
                if (linkedTargets.Contains(word)) continue;
                if (puncs.Contains(word)) break;
                TargetWord tWord = new TargetWord();
                tWord.ID = id;
                tWord.Position = i;
                tWord.Text = word;
                candidates.Add(tWord);
            }
            for (int i = anchorPosition + 1; i < targetWords.Length && i <= anchorPosition + 3; i++)
            {
                string targetWord = targetWords[i];
                if (contentWordsOnly && targetFuncWords.Contains(targetWord)) continue;
                string word = Data.GetWord(targetWord);
                string id = GetTargetID(targetWord);
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

        static ArrayList GetTargetCandidates(MappedWords preNeighbor, MappedWords postNeighbor, string[] targetWords, List<string> linkedTargets, ArrayList puncBounds, ArrayList targetFuncWords, bool contentWordsOnly)
        {
            ArrayList candidates = new ArrayList();

            int startPosition = preNeighbor.TargetNode.Word.Position;
            int endPosition = postNeighbor.TargetNode.Word.Position;

            for (int i = startPosition; i >= 0 && i < targetWords.Length && i < endPosition; i++)
            {
                if (i == startPosition) continue;
                if (i == endPosition) continue;
                string targetWord = targetWords[i];
                if (contentWordsOnly && targetFuncWords.Contains(targetWord)) continue;
                string word = Data.GetWord(targetWord);
                string id = GetTargetID(targetWord);
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

        // linksTable :: Hashtable(sourceId => MappedWords)
        // returns ArrayList(MappedWords)
        //
        static ArrayList GetLinkedSiblings(XmlNode treeNode, Dictionary<string, MappedWords> linksTable, ref bool stopped)
        {
            ArrayList linkedSiblings = new ArrayList();

            if (treeNode.ParentNode == null || treeNode.ParentNode.Name == "Tree") stopped = true;

            while (!stopped && treeNode.ParentNode != null && linkedSiblings.Count == 0)
            {
/*                int chunkLength = Int32.Parse(Utils.GetAttribValue(treeNode.ParentNode, "Length"));
                if (chunkLength > 2)
                {
                    stopped = true;
                    break;
                } */
                foreach(XmlNode childNode in treeNode.ParentNode.ChildNodes)
                {
                    if (childNode != treeNode)
                    {
                        ArrayList terminals = Terminals.GetTerminalXmlNodes(childNode);
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

        static MappedWords GetPreNeighbor(MappedWords unLinked, ArrayList linkedSiblings)
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

        static MappedWords GetPostNeighbor(MappedWords unLinked, ArrayList linkedSiblings)
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

        // conflicts :: ArrayList(ArrayList(MappedWords))
        // links :: ArrayList(MappedWords)
        // replaces some members of links with a special non-link MappedWords datum
        //
        public static void ResolveConflicts(List<List<MappedWords>> conflicts, List<MappedWords> links, int pass)
        {
            ArrayList linksToRemove = new ArrayList();

            foreach (List<MappedWords> conflict in conflicts)
            {
                List<MappedWords> winners = FindWinners(conflict, pass);

                if (winners.Count > 1)
                {
                    MappedWords winner = winners[0];
                    winners.Clear();
                    winners.Add(winner);
                }
                foreach (MappedWords map in conflict)
                {
                    if (!winners.Contains(map))
                    {
                        linksToRemove.Add(map);
                    }
                }
            }

            for (int i = 0; i < links.Count; i++)
            {
                MappedWords link = (MappedWords)links[i];
                if (linksToRemove.Contains(link))
                {
                    MappedWords nonLink = new MappedWords();
                    nonLink.SourceNode = link.SourceNode;
                    LinkedWord nonTarget = new LinkedWord();
                    nonTarget.Prob = -1000;
                    nonTarget.Word = Align.CreateFakeTargetWord();
                    nonLink.TargetNode = nonTarget;
                    links[i] = nonLink;
                }
            }
        }

        static List<string> GetLinkedTargets(List<MappedWords> links)
        {
            List<string> linkedTargets =
                new List<string>();

            foreach(MappedWords link in links)
            {
                if (!link.TargetNode.Word.IsFake)
                {
                    linkedTargets.Add(link.TargetNode.Word.ID);
                }
            }

            return linkedTargets;
        }

        static void GetLinkedWords(ArrayList path, List<LinkedWord> links, double prob)
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
                        GetLinkedWords(c.Sequence, links, c.Prob);
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

 
        // returns ArrayList(MappedWord)
        //
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


        static Dictionary<string, MappedWords> CreateLinksTable(List<MappedWords> links)
        {
            Dictionary<string, MappedWords> linksTable =
                new Dictionary<string, MappedWords>();

            foreach(MappedWords link in links)
            {
                if (link.TargetNode.Word.IsFake) continue;
                string sourceID = link.SourceNode.MorphID;
                linksTable.Add(sourceID, link);
            }

            return linksTable;
        }

        public static string GetTargetID (string target)
        {
            return target.Substring(target.LastIndexOf("_") + 1);
        }

        static LinkedWord GetTopCandidate(
            SourceNode sWord, 
            ArrayList tWords, 
            Hashtable model, 
            List<string> linkedTargets, 
            ArrayList puncs, 
            ArrayList stopWords, 
            Hashtable goodLinks,
            int goodLinkMinCount,
            Hashtable badLinks, 
            int badLinkMinCount
            )
        {
            Hashtable probs = new Hashtable();

            ArrayList topCandidates = new ArrayList();

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

                if (model.ContainsKey(sWord.Lemma))
                {
                    Hashtable translations = (Hashtable)model[sWord.Lemma];
                    if (translations.ContainsKey(tWord.Text))
                    {
                        double prob = (double)translations[tWord.Text];
                        if (prob >= 0.17)
                        {
                            probs.Add(tWord, Math.Log(prob));
                        }
                    }
                }
            }

            if (probs.Count > 0)
            {
                ArrayList candidates = Data.SortWordCandidates(probs);

                TargetWord topCandidate = (TargetWord)candidates[0];
                topCandidate.IsFake = false;

                LinkedWord linkedWord = new LinkedWord();
                linkedWord.Prob = (double)probs[topCandidate];
                linkedWord.Text = topCandidate.Text;
                linkedWord.Word = topCandidate;
                return linkedWord;
            }

            return null;
        }

        public static void FixCrossingLinks(ref ArrayList links)
        {
            Hashtable uniqueLemmaLinks = GetUniqueLemmaLinks(links);
            ArrayList crossingLinks = IdentifyCrossingLinks(uniqueLemmaLinks);
            SwapTargets(crossingLinks, ref links);
        }

        static Hashtable GetUniqueLemmaLinks(ArrayList links)
        {
            Hashtable uniqueLemmaLinks = new Hashtable();

            foreach(MappedGroup link in links)
            {
                if (link.SourceNodes.Count == 1 && link.TargetNodes.Count == 1)
                {
                    SourceNode sNode = (SourceNode)link.SourceNodes[0];
                    string lemma = sNode.Lemma;
                    if (uniqueLemmaLinks.ContainsKey(lemma))
                    {
                        ArrayList linkedNodes = (ArrayList)uniqueLemmaLinks[lemma];
                        linkedNodes.Add(link);
                    }
                    else
                    {
                        ArrayList linkedNodes = new ArrayList();
                        linkedNodes.Add(link);
                        uniqueLemmaLinks.Add(lemma, linkedNodes);
                    }
                }
            }

            return uniqueLemmaLinks;
        }

        static ArrayList IdentifyCrossingLinks(Hashtable uniqueLemmaLinks)
        {
            ArrayList crossingLinks = new ArrayList();

            IDictionaryEnumerator lemmaEnum = uniqueLemmaLinks.GetEnumerator();

            while (lemmaEnum.MoveNext())
            {
                string lemma = (string)lemmaEnum.Key;
                ArrayList links = (ArrayList)lemmaEnum.Value;
                if (links.Count == 2 && Crossing(links))
                {
                    CrossingLinks cl = new CrossingLinks();
                    cl.Link1 = (MappedGroup)links[0];
                    cl.Link2 = (MappedGroup)links[1];
                    crossingLinks.Add(cl);
                }
            }

            return crossingLinks;
        }

        static bool Crossing(ArrayList links)
        {
            MappedGroup link1 = (MappedGroup)links[0];
            MappedGroup link2 = (MappedGroup)links[1];
            SourceNode sWord1 = (SourceNode)link1.SourceNodes[0];
            LinkedWord tWord1 = (LinkedWord)link1.TargetNodes[0];
            SourceNode sWord2 = (SourceNode)link2.SourceNodes[0];
            LinkedWord tWord2 = (LinkedWord)link2.TargetNodes[0];
            if (tWord1.Word.Position < 0 || tWord2.Word.Position < 0) return false;
            if ( (sWord1.Position < sWord2.Position && tWord1.Word.Position > tWord2.Word.Position)
               || (sWord1.Position > sWord2.Position && tWord1.Word.Position < tWord2.Word.Position)
               )
            {
                return true;
            }

            return false;
        }

        static void SwapTargets(ArrayList crossingLinks, ref ArrayList links)
        {
            for (int i = 0; i < crossingLinks.Count; i++)
            {
                CrossingLinks cl = (CrossingLinks)crossingLinks[i];
                SourceNode sNode1 = (SourceNode)cl.Link1.SourceNodes[0];
                SourceNode sNode2 = (SourceNode)cl.Link2.SourceNodes[0];
                ArrayList TargetNodes0 = (ArrayList)cl.Link1.TargetNodes;
                cl.Link1.TargetNodes = cl.Link2.TargetNodes;
                cl.Link2.TargetNodes = TargetNodes0;
                for(int j = 0; j < links.Count; j++)
                {
                    MappedGroup mp = (MappedGroup)links[j];
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
        public ArrayList SourceNodes = new ArrayList();
        public ArrayList TargetNodes = new ArrayList();
    }

    public class CrossingLinks
    {
        public MappedGroup Link1;
        public MappedGroup Link2;
    }
}
