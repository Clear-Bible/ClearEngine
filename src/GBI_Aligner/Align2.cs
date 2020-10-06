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
            

            ArrayList conflicts = FindConflictingLinks(links);

            // conflicts :: ArrayList(ArrayList(MappedWords))

            // TIM Study
            //TimUtil.PrintArrayList("conflicts", conflicts);

            if (conflicts.Count > 0)
            {
                ResolveConflicts(conflicts, links, 1);
            }

            ArrayList linkedTargets = GetLinkedTargets(links);
            // linkedTargets :: ArrayList(MappedWords)
            // a copy of links with the fakes removed

            // TIM Study
            //TimUtil.PrintArrayList("linkedTargets", linkedTargets);


            Hashtable linksTable = CreateLinksTable(links);
            // linksTable :: Hashtable(sourceId => MappedWords)

            // TIM Study
            //TimUtil.PrintHashTable("linksTable", linksTable);

            for (int i = 0; i < links.Count; i++)
            {
                MappedWords link = (MappedWords)links[i];

                if (link.TargetNode.Word.IsFake)
                {
                    bool linked = false;
                    if (!linked)  // (always true)
                    { 
                        AlignWord(ref link, targetWords, linksTable, linkedTargets, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);
                    }
                }
            }

            conflicts = FindConflictingLinks(links);

            // TIM Study
            //TimUtil.PrintArrayList("conflicts", conflicts);

            if (conflicts.Count > 0)
            {
                ResolveConflicts(conflicts, links, 2);
            }

            return new ArrayList(links);
        }


        static void AlignWord(
            ref MappedWords link, // output goes here
            string[] targetWords,
            Hashtable linksTable, // Hashtable(sourceId => MappedWords)
            ArrayList linkedTargets, // ArrayList(MappedWords)
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

        static ArrayList GetTargetCandidates(MappedWords postNeighbor, string[] targetWords, ArrayList linkedTargets, ArrayList puncs, ArrayList targetFuncWords, bool contentWordsOnly)
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

        static ArrayList GetTargetCandidates(MappedWords preNeighbor, MappedWords postNeighbor, string[] targetWords, ArrayList linkedTargets, ArrayList puncBounds, ArrayList targetFuncWords, bool contentWordsOnly)
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
        static ArrayList GetLinkedSiblings(XmlNode treeNode, Hashtable linksTable, ref bool stopped)
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
                                MappedWords map = (MappedWords)linksTable[morphID];
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
        public static void ResolveConflicts(ArrayList conflicts, List<MappedWords> links, int pass)
        {
            ArrayList linksToRemove = new ArrayList();

            foreach (ArrayList conflict in conflicts)
            {
                ArrayList winners = FindWinners(conflict, pass);
                // winners :: ArrayList(MappedWord)
                if (winners.Count > 1)
                {
                    MappedWords winner = (MappedWords)winners[0];
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

        static ArrayList GetLinkedTargets(List<MappedWords> links)
        {
            ArrayList linkedTargets = new ArrayList();

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
                LinkedWord linkedWord = new LinkedWord();
                TargetWord tWord = new TargetWord();
                tWord.Text = string.Empty;
                tWord.Position = -1;
                tWord.IsFake = true;
                tWord.ID = "0";
                linkedWord.Word = tWord;
                linkedWord.Prob = -1000;
                linkedWord.Text = string.Empty;
                links.Add(linkedWord);
            }
            else
            {
                Object obj = path[0];
                string type = obj.GetType().ToString();
                if (type == "GBI_Aligner.Candidate")
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
                        LinkedWord linkedWord = new LinkedWord();
                        linkedWord.Word = tWord;
                        linkedWord.Prob = prob;
                        linkedWord.Text = tWord.Text;
                        links.Add(linkedWord);
                    }
                }
            }
        }

        // links :: ArrayList(MappedWords)
        // returns ArrayList(ArrayList(MappedWords)) where each member has length > 1
        //
        public static ArrayList FindConflictingLinks(List<MappedWords> links)
        {
            ArrayList conflicts = new ArrayList();

            Hashtable targetLinks = new Hashtable();
            // Hashtable("tWord-tPosition" => ArrayList(MappedWords))

            foreach(MappedWords link in links)
            {
                string tWord = link.TargetNode.Word.Text;
                string tPosition = link.TargetNode.Word.ID;
                if (tWord == string.Empty) continue;
                string key = tWord + "-" + tPosition;
                if (targetLinks.ContainsKey(key))
                {
                    ArrayList targets = (ArrayList)targetLinks[key];
                    targets.Add(link);
                }
                else
                {
                    ArrayList targets = new ArrayList();
                    targets.Add(link);
                    targetLinks.Add(key, targets);
                }    
            }

            IDictionaryEnumerator targetEnum = targetLinks.GetEnumerator();

            while (targetEnum.MoveNext())
            {
                ArrayList targets = (ArrayList)targetEnum.Value;
                if (targets.Count > 1)
                {
                    conflicts.Add(targets);
                }
            }

            return conflicts;
        }

 
        // conflict :: ArrayList(MappedWord)
        // returns ArrayList(MappedWord)
        //
        static ArrayList FindWinners(ArrayList conflict, int pass)
        {
            double bestProb = GetBestProb(conflict);
            double minDistance = GetMinDistance(conflict);
            ArrayList winners = new ArrayList();
            foreach (MappedWords mappedWord in conflict)
            {
                double prob = mappedWord.TargetNode.Prob;
                if (prob == bestProb)
                {
                    winners.Add(mappedWord);
                }
            }

            if (pass == 2 && winners.Count > 1)
            {
                foreach (MappedWords winner in winners)
                {
                    double distance = Math.Abs(winner.SourceNode.RelativePos - winner.TargetNode.Word.RelativePos);
                    if (distance == minDistance)
                    {
                        winners.Clear();
                        winners.Add(winner);
                        break;
                    }
                }
            }

            return winners;
        }



        static double GetBestProb(ArrayList conflict)
        {
            double bestProb = -1000;

            foreach(MappedWords mappedWord in conflict)
            {
                double prob = mappedWord.TargetNode.Prob;
                if (prob > bestProb) bestProb = prob;
            }

            return bestProb;
        }

 

        static double GetMinDistance(ArrayList conflict)
        {
            double minDistance = 100;

            foreach (MappedWords mappedWord in conflict)
            {
                double distance = Math.Abs(mappedWord.SourceNode.RelativePos - mappedWord.TargetNode.Word.RelativePos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }


        static Hashtable CreateLinksTable(List<MappedWords> links)
        {
            Hashtable linksTable = new Hashtable();

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
            ArrayList linkedTargets, 
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
