using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Xml;

using Newtonsoft.Json;
using Trees;
using Utilities;

namespace GBI_Aligner
{
    class Align
    {
        // align a set of verses
        public static Alignment2 AlignCorpus(
            string source,  // name of file with source IDs
            string sourceLemma,  // name of file with source lemma IDs
            string target, // name of tokens.txt file, after alignment
//			string targetLower,
            Hashtable model,  // translation model, Hashtable(source => Hashtable(target => probability))
            Hashtable manModel, // manually checked alignments
                                // Hashtable(source => Hashtable(target => Stats{ count, probability})
            Hashtable alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            Hashtable preAlignment, // Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            Hashtable groups, // comes from Data.LoadGroups("groups.txt")
                              //   of the form Hashtable(...source... => ArrayList(TargetGroup{...text..., primaryPosition}))
            string treeFolder,
            Hashtable bookNames,
            string jsonOutput,
            int maxPaths,
			ArrayList puncs,
            ArrayList stopWords,
            Hashtable goodLinks, // Hashtable(link => count)
            int goodLinkMinCount,
            Hashtable badLinks,
            int badLinkMinCount,
            Hashtable glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks, // verseID => (mWord.altId => tWord.altId)
            ArrayList sourceFuncWords, 
            ArrayList targetFuncWords,
            bool contentWordsOnly,
            Hashtable strongs
            )
        {
            List<string> sourceVerses = Data.GetVerses(sourceLemma, false);
            List<string> sourceVerses2 = Data.GetVerses(source, false);
            List<string> targetVerses = Data.GetVerses(target, true);
            List<string> targetVerses2 = Data.GetVerses(target, false);

            string prevChapter = string.Empty;

            Dictionary<string, XmlNode> trees = new Dictionary<string, XmlNode>();  // Hashtable(verseID => XmlNode)

            Alignment2 align = new Alignment2();  // The output goes here.
            align.Lines = new Line[sourceVerses.Count];

            for (int i = 0; i < sourceVerses.Count; i++)
            {
                if (i == 8)
                {
                    ;
                }
                string sourceVerse = (string)sourceVerses[i];  // lemmas
                string sourceVerse2 = (string)sourceVerses2[i]; // source IDs
                string targetVerse = (string)targetVerses[i];   // tokens, lowercase
                string targetVerse2 = (string)targetVerses2[i]; // tokens, not lowercase
                string chapterID = GetChapterID(sourceVerse);  // string with chapter number

                //Console.WriteLine($"sourceVerse: {sourceVerse}\n");
                //Console.WriteLine($"sourceVerse2: {sourceVerse2}\n");
                //Console.WriteLine($"targetVerse: {targetVerse}\n");
                //Console.WriteLine($"targetVerse2: {targetVerse2}\n");

                if (chapterID != prevChapter)
                {
                    trees.Clear();
                    // Get the trees for the current chapter; a verse can cross chapter boundaries
                    VerseTrees.GetChapterTree(chapterID, treeFolder, trees, bookNames);
                    string book = chapterID.Substring(0, 2);
                    string chapter = chapterID.Substring(2, 3);
                    string prevChapterID = book + Utils.Pad3((Int32.Parse(chapter) - 1).ToString());
                    VerseTrees.GetChapterTree(prevChapterID, treeFolder, trees, bookNames);
                    string nextChapterID = book + Utils.Pad3((Int32.Parse(chapter) + 1).ToString());
                    VerseTrees.GetChapterTree(nextChapterID, treeFolder, trees, bookNames);
                    prevChapter = chapterID;
                }

                // Align a single verse
                AlignVerse(sourceVerse, sourceVerse2, targetVerse, targetVerse2, model, manModel, alignProbs, preAlignment, useAlignModel, groups, trees, ref align, i, maxPaths, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, glossTable, oldLinks, sourceFuncWords, targetFuncWords, contentWordsOnly, strongs);
            }

            return align;
        }

        static void AlignVerse(
            string sourceVerse,  // string, lemmas
            string sourceVerse2, // string, sourceIDs
            string targetVerse,  // tokens, lowercase
            string targetVerse2, // tokens, not lowercase
            Hashtable model, // translation model, Hashtable(source => Hashtable(target => probability))
            Hashtable manModel, // manually checked alignments
                                // Hashtable(source => Hashtable(target => Stats{ count, probability})
            Hashtable alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            Hashtable preAlignment, // Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            Hashtable groups, // comes from Data.LoadGroups("groups.txt")
                              //   of the form Hashtable(...source... => ArrayList(TargetGroup{...text..., primaryPosition}))
            Dictionary<string, XmlNode> trees, // verseID => XmlNode
            ref Alignment2 align,  // Output goes here.
            int i,
            int maxPaths,
			ArrayList puncs,
            ArrayList stopWords,
            Hashtable goodLinks,
            int goodLinkMinCount,
            Hashtable badLinks,
            int badLinkMinCount,
            Hashtable glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,  // verseID => (mWord.altId => tWord.altId)
            ArrayList sourceFuncWords,
            ArrayList targetFuncWords,
            bool contentWordsOnly,
            Hashtable strongs
            )
        {  
            string[] sourceWords = sourceVerse.Split(" ".ToCharArray());   // lemmas
            string[] sourceWords2 = sourceVerse2.Split(" ".ToCharArray()); // source words
            string[] targetWords = targetVerse.Split(" ".ToCharArray());   // tokens, lowercase
            string[] targetWords2 = targetVerse2.Split(" ".ToCharArray()); // tokens, not lowercase

            int n = targetWords.Length;  // n = number of target tokens

            string sStartVerseID = GetVerseID(sourceWords[0]);  // bbcccvvv
            string sEndVerseID = GetVerseID(sourceWords[sourceWords.Length - 1]); // bbcccvvv

            XmlNode treeNode = GetTreeNode(sStartVerseID, sEndVerseID, trees);

            // TimUtil.PrintXmlNode(treeNode);

            Dictionary<string, WordInfo> wordInfoTable =
                Data.BuildWordInfoTable(treeNode);
           
            List<SourceWord> sWords = GetSourceWords(
                sourceWords,
                wordInfoTable);
           
            List<TargetWord> tWords = GetTargetWords(
                targetWords,
                targetWords2);

            Dictionary<string, string> idMap =
                OldLinks.CreateIdMap(sWords);

            string verseNodeID = Utils.GetAttribValue(treeNode, "nodeId");
            verseNodeID = verseNodeID.Substring(0, verseNodeID.Length - 1);
            string verseID = verseNodeID.Substring(0, 8);
            

            Dictionary<string, string> existingLinks =
                new Dictionary<string, string>();
            if (oldLinks.ContainsKey(verseID))  // verseID as obtained from tree
            {
                existingLinks = oldLinks[verseID];
                // mWord.altId => tWord.altId)
            }

            Dictionary<string, List<Candidate>> terminalCandidates =
                new Dictionary<string, List<Candidate>>();
            TerminalCandidates.GetTerminalCandidates(terminalCandidates, treeNode, tWords, model, manModel, alignProbs, useAlignModel, n, verseID, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, existingLinks, idMap, sourceFuncWords, contentWordsOnly, strongs);

            
            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();
            AlignNodes(treeNode, tWords, ref alignments, n, sourceWords.Length, maxPaths, terminalCandidates);

            List<Candidate> verseAlignment = alignments[verseNodeID];
            Candidate topCandidate = (Candidate)verseAlignment[0];

            string linkedWords = GetWords(topCandidate);

            List<XmlNode> terminals = Terminals.GetTerminalXmlNodes(treeNode);
            List<MappedWords> links = Align2.AlignTheRest(topCandidate, terminals, sourceWords, targetWords, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);

            List<MappedGroup> links2 = Groups.WordsToGroups(links);

            Groups.AlignGroups(ref links2, sWords, tWords, groups, terminals);
            Align2.FixCrossingLinks(ref links2);
            //            Output.WriteAlignment(links, sourceWords, targetWords2, ref align, i, wordInfoTable, groups);
            Output.WriteAlignment(links2, sWords, tWords, ref align, i, glossTable, groups);
        }

        static void AlignNodes(
            XmlNode treeNode,
            List<TargetWord> tWords,
            ref Dictionary<string, List<Candidate>> alignments, 
            int n, // number of target tokens
            int sLength, // number of source words
            int maxPaths,
            Dictionary<string, List<Candidate>> terminalCandidates
                // terminalCandidates :: HashTable(SourceWord.Id =>
                //     ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
            )
        {
            if (treeNode.NodeType.ToString() == "Text") // child of a terminal node
            {
                return;
            }

            foreach(XmlNode subTree in treeNode)
            {
                AlignNodes(subTree, tWords, ref alignments, n, sLength, maxPaths, terminalCandidates);
                // recursive call
            }

            string nodeID = Utils.GetAttribValue(treeNode, "nodeId");
            nodeID = nodeID.Substring(0, nodeID.Length - 1);
            if (nodeID == "40001003012004")
            {
                ;
            }

            if (treeNode.FirstChild.NodeType.ToString() == "Text") // terminal node
            {
                // Make a new SourceWord for this terminal node.
                // But all we end up using is the ID member.
                SourceWord sWord = new SourceWord();
                sWord.ID = Utils.GetAttribValue(treeNode, "morphId");
                if (sWord.ID.Length == 11)
                {
                    sWord.ID += "1";
                }
                sWord.Lemma = Utils.GetAttribValue(treeNode, "UnicodeLemma");
                sWord.Gloss = Utils.GetAttribValue(treeNode, "English");
                sWord.Category = Utils.GetAttribValue(treeNode, "Cat");
                sWord.Position = Int32.Parse(Utils.GetAttribValue(treeNode, "Start"));
                sWord.RelativePos = (double)sWord.Position / (double)sLength;

                List<Candidate> topCandidates =
                    terminalCandidates[sWord.ID];
                    // candidates that were found for this terminal:

                // (Doesn't do anything.)
                foreach (Candidate c in topCandidates)
                {
                    string linkedWords = GetWords(c);
                }

                alignments.Add(nodeID, topCandidates);
            }   
            else if (treeNode.ChildNodes.Count > 1)  // non-terminal with multiple children
            {
                List<string> sNodes = GetSourceNodes(treeNode);
                    // ::= List of morphId for terminal nodes under this node

                List<List<Candidate>> candidates = new List<List<Candidate>>();
                // list of candidate lists, one for each child, some might be empty

                foreach (XmlNode childNode in treeNode.ChildNodes)
                {
                    string childNodeID = Utils.GetAttribValue(childNode, "nodeId");
                    // John 1:1 first node: nodeId="430010010010171"
                    if (childNodeID.Length == 15)
                    {
                        childNodeID = childNodeID.Substring(0, childNodeID.Length - 1);
                    }
                    else
                    {
                        childNodeID = childNodeID.Substring(0, childNodeID.Length - 2);
                    }

                    List<Candidate> childCandidates = alignments[childNodeID];

                    if (childCandidates.Count == 0)
                    {
                        childCandidates = CreateEmptyCandidate();
                    }

                    // (doesn't do anything)
                    foreach (Candidate childCandidate in childCandidates)
                    {
                        string linkedWords = GetWords(childCandidate);
                        //                    sw.WriteLine(linkedWords);
                    }

                    candidates.Add(childCandidates);
                    
                }

                List<Candidate> topCandidates = ComputeTopCandidates(candidates, n, maxPaths, sNodes, treeNode);

                // doesn't actually do anything
                foreach (Candidate c in topCandidates)
                {
                    string linkedWords = GetWords(c);
//                    sw.WriteLine(linkedWords);
                }

                if (alignments.ContainsKey(nodeID))
                {
                    alignments.Remove(nodeID);
                    alignments.Add(nodeID, topCandidates);
                }
                else
                {
                    alignments.Add(nodeID, topCandidates);
                }
            }
        }


        // uses existing link if there is one
        // no candidates if it is not a content word
        // uses strongs if it is there
        // uses man trans model if it is there
        // uses model if it is there and it is not punctuation or a stop word
        //   and gets candidates of maximal probability
        //
        // returns ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })
        //
        public static List<Candidate> GetTopCandidates(
            SourceWord sWord,
            List<TargetWord> tWords,
            Hashtable model, // translation model, Hashtable(source => Hashtable(target => probability))
            Hashtable manModel, // manually checked alignments
                                // Hashtable(source => Hashtable(target => Stats{ count, probability})
            Hashtable alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            bool useAlignModel,
            int n, // number of target tokens (not actually used)
            ArrayList puncs,
            ArrayList stopWords,
            Hashtable goodLinks, // (not actually used)
            int goodLinkMinCount, // (not actually used)
            Hashtable badLinks,
            int badLinkMinCount,
            Dictionary<string, string> existingLinks, // mWord.altId => tWord.altId
            ArrayList sourceFuncWords,
            bool contentWordsOnly, // (not actually used)
            Hashtable strongs
            )
        {
            List<Candidate> topCandidates = new List<Candidate>();

            if (existingLinks.Count > 0 && sWord.AltID != null && existingLinks.ContainsKey(sWord.AltID))
            {
                string targetAltID = existingLinks[sWord.AltID];
                TargetWord target = OldLinks.GetTarget(targetAltID, tWords);
                if (target != null)
                {
                    Candidate c = new Candidate();
                    c.Prob = 0.0;
                    c.Sequence.Add(target);
                    topCandidates.Add(c);
                    return topCandidates;
                }
            }

            Hashtable probs = new Hashtable();
            // TargetWord => log of probability

            bool isContentWord = IsContentWord(sWord.Lemma, sourceFuncWords);
            if (!isContentWord) return topCandidates;

            if (strongs.ContainsKey(sWord.Strong))
            {
                Hashtable wordIds = (Hashtable)strongs[sWord.Strong];
                ArrayList matchingTwords = GetMatchingTwords(wordIds, tWords);
                foreach(TargetWord target in matchingTwords)
                {
                    Candidate c = new Candidate();
                    c.Prob = 0.0;
                    c.Sequence.Add(target);
                    topCandidates.Add(c);
                }
                return topCandidates;
            }

            if (manModel.ContainsKey(sWord.Lemma))
            {
                Hashtable translations = (Hashtable)manModel[sWord.Lemma];

                for (int i = 0; i < tWords.Count; i++)
                {
                    TargetWord tWord = (TargetWord)tWords[i];
                    if (translations.ContainsKey(tWord.Text))
                    {
                        Stats s = (Stats)translations[tWord.Text];
                        if (s.Prob < 0.2) s.Prob = 0.2;
                        probs.Add(tWord, Math.Log(s.Prob));
                    }
                }
            }
            else if (model.ContainsKey(sWord.Lemma))
            {
                Hashtable translations = (Hashtable)model[sWord.Lemma];
                // tWord.Text => double

                for (int i = 0; i < tWords.Count; i++)
                {
                    TargetWord tWord = (TargetWord)tWords[i];
                    string link = sWord.Lemma + "#" + tWord.Text;
                    if (badLinks.ContainsKey(link) && (int)badLinks[link] >= badLinkMinCount)
                    {
                        continue;
                    }
                    if (puncs.Contains(tWord.Text)) continue;
                    if (stopWords.Contains(sWord.Lemma)) continue;
                    if (stopWords.Contains(tWord.Text)) continue;

                    if (translations.ContainsKey(tWord.Text))
                    {
                        double prob = (double)translations[tWord.Text];
                        string idKey = sWord.ID + "-" + tWord.ID;
                        double adjustedProb;
                        if (useAlignModel)
                        {
                            if (alignProbs.ContainsKey(idKey))
                            {
                                double aProb = (double)alignProbs[idKey];
                                adjustedProb = prob + ((1.0 - prob) * aProb);
                            }
                            else
                            {
                                adjustedProb = prob * 0.6;
                            }
                        }
                        else
                        {
                            adjustedProb = prob;
                        }
                        if (isContentWord || prob >= 0.5)
                        {
                            probs.Add(tWord, Math.Log(adjustedProb));
                        }
                    }
                }
            }

            double bestProb = FindBestProb(probs);
            topCandidates = GetTopCandidate(bestProb, probs);
              // get candidates of maximal probability

 //           ArrayList candidates = Data.SortWordCandidates(probs);
 //           topCandidates = GetTopCandidates2(candidates, probs);
            foreach (Candidate c in topCandidates)
            {
                string linkedWords = GetWords(c);
            }

            return topCandidates;
        }

/*        public static bool IsContentWord(string cat)
        {
            if (cat == "noun" || cat == "verb" || cat == "adj" || cat == "Name") return true;
            else return false;
        } */

        public static bool IsContentWord(string lemma, ArrayList sourceFuncWords)
        {
            if (sourceFuncWords.Contains(lemma)) return false;

            return true;
        }

        static double FindBestProb(Hashtable probs)
        {
            double bestProb = -10.0;

            IDictionaryEnumerator probEnum = probs.GetEnumerator();

            while (probEnum.MoveNext())
            {
                double prob = (double)probEnum.Value;
                if (prob > bestProb) bestProb = prob;
            }

            return bestProb;
        }

        static List<Candidate> GetTopCandidate(double bestProb, Hashtable probs)
        {
            List<Candidate> bestCandidates = new List<Candidate>();

            IDictionaryEnumerator probEnum = probs.GetEnumerator();

            while (probEnum.MoveNext())
            {
                TargetWord tWord = (TargetWord)probEnum.Key;
                double prob = (double)probEnum.Value;
                if (prob == bestProb)
                {
                    Candidate c = new Candidate();
                    c.Prob = prob;
                    c.Sequence.Add(tWord);
                    bestCandidates.Add(c);
                }
            }

            return bestCandidates;
        }


        // childCandidateList = ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        // returns ArrayList(Candidate)
        //
        static List<Candidate> ComputeTopCandidates(
            List<List<Candidate>> childCandidateList,
            int n,
            int maxPaths,
            List<string> sNodes,
            XmlNode treeNode)
        {
            Dictionary<CandidateSequence, double> pathProbs =
                new Dictionary<CandidateSequence, double>();
            List<List<Candidate>> allPaths = CreatePaths(childCandidateList, maxPaths);
            List<List<Candidate>> paths = FilterPaths(allPaths);
            // paths = those where the candidates use different words
            if (paths.Count == 0)
            {
                List<Candidate> topPath = allPaths[0];
                paths.Add(topPath);
            }

            List<Candidate> topCandidates =
                new List<Candidate>();

            foreach (List<Candidate> path in paths)
            {
                double jointProb = ComputeJointProb(path); // sum of candidate probabilities in a path
                try
                {
                    pathProbs.Add(new CandidateSequence(path), jointProb);
                }
                catch
                {
                    Console.WriteLine("Hashtable out of memory.");
                    List<CandidateSequence> sortedCandidates2 = Sort.SortTableDoubleDesc(pathProbs);
                    int topN2 = sortedCandidates2.Count / 10;
                    if (topN2 < n) topN2 = n;
                    topCandidates = GetTopPaths2(sortedCandidates2, pathProbs);
                    return topCandidates;
                }
            }

            Dictionary<CandidateSequence, double> pathProbs2 =
                AdjustProbsByDistanceAndOrder(pathProbs);

            List<List<Candidate>> sortedCandidates = Data.SortPaths(pathProbs2);

            // (topN not actually used)
            int topN = sortedCandidates.Count / 10;
            if (topN < n) topN = n;

            topCandidates = GetTopPaths2(sortedCandidates, pathProbs);
            // topCandidates :: List<Candidate>
            // one for each path of maximal probability

            // (doesn't actually do anything)
            foreach (Candidate c in topCandidates)
            {
                string linkedWords = GetWords(c);
            }

            // (doesn't actually do anything)
            for (int i = 0; i < topCandidates.Count; i++)
            {
                Candidate c = (Candidate)topCandidates[i];
            }

            // (doesn't actually do anything)
            Candidate topCandidate = (Candidate)topCandidates[0];

            return topCandidates;
        }


        // pathProbs :: Hashtable(ArrayList(Candidate), jointProb)
        // returns a datum of the same type
        //
        static Dictionary<CandidateSequence, double>
            AdjustProbsByDistanceAndOrder(
                Dictionary<CandidateSequence, double> pathProbs)
        {
            Dictionary<CandidateSequence, double> pathProbs2 =
                new Dictionary<CandidateSequence, double>();

            List<Candidate> candidates = new List<Candidate>();

            foreach (var keyValuePair in pathProbs)
            {
                // Make a new candidate that has the path inside of it,
                // and put that new candidate on candidates.
                Candidate candidate = new Candidate();
                candidate.Sequence = keyValuePair.Key;
                candidate.Prob = keyValuePair.Value;
                candidates.Add(candidate);
            }

            int minimalDistance = 10000;
            foreach (Candidate c in candidates)
            {
                int distance = ComputeDistance(c.Sequence); // something about distance between words
                if (distance < minimalDistance) minimalDistance = distance;
            }

            if (minimalDistance > 0)
            {
                foreach (Candidate c in candidates)
                {
                    string linkedWords = GetWords(c);
                    if (linkedWords == "chaos-4 --1 vide-8")
                    {
                        ;
                    }
                    int distance = ComputeDistance(c.Sequence);
                    double distanceProb = Math.Log((double)minimalDistance / (double)distance);
                    double orderProb = ComputeOrderProb(c.Sequence);  // something about word order
                    double adjustedProb = c.Prob + c.Prob + distanceProb + orderProb / 2.0;
                    c.Prob = adjustedProb;
                    pathProbs2.Add(c.Sequence, adjustedProb);
                }
            }
            else
            {
                foreach (Candidate c in candidates)
                {
                    pathProbs2 = pathProbs;
                }
            }

            return pathProbs2;
        }

        // returns "text1-posn1 text2-posn2 ..."
        //
        public static string GetWords(Candidate c)
        {
            List<TargetWord> wordsInPath = new List<TargetWord>();
            GetWordsInPath(c.Sequence, wordsInPath);

            string words = string.Empty;

            foreach(TargetWord wordInPath in wordsInPath)
            {
                words += wordInPath.Text + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }

        public static string GetWordsInPath(List<Candidate> path)
        {
            List<TargetWord> wordsInPath = new List<TargetWord>();
            GetWordsInPath(new CandidateSequence(path), wordsInPath);

            string words = string.Empty;

            foreach (TargetWord wordInPath in wordsInPath)
            {
                words += wordInPath.Text + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }

        // paths :: ArrayList(ArrayList(Candidate))
        // returns the valid paths, which are ones where the candidates
        // use different words
        //
        static List<List<Candidate>> FilterPaths(List<List<Candidate>> paths)
        {
            List<List<Candidate>> filteredPaths = new List<List<Candidate>>();

            foreach(List<Candidate> path in paths)
            {
                if (IsValidPath(path))
                {
                    filteredPaths.Add(path);
                }
            }

            return filteredPaths;
        }

        // path is valid if candidates use different words
        static bool IsValidPath(List<Candidate> path)
        {
            string wordsInPath = GetWordsInPath(path);
            string[] words = wordsInPath.Split(" ".ToCharArray());
            ArrayList usedWords = new ArrayList();
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (word == "--1") continue;
                if (usedWords.Contains(word))
                {
                    return false;
                }
                else
                {
                    usedWords.Add(word);
                }
            }

            return true;
        }

        static double ComputeJointProb(List<Candidate> path)
        {
            double jointProb = 0.0;

            foreach(Candidate c in path)
            {
                jointProb += c.Prob;
            }

            return jointProb;
        }

        static int ComputeDistance(CandidateSequence path)
        {
            List<TargetWord> wordsInPath = new List<TargetWord>();
            GetWordsInPath(path, wordsInPath);
            int distance = 0;

            int position = GetInitialPosition(wordsInPath);

            for (int i = 0; i < wordsInPath.Count; i++)
            {
                TargetWord tw = wordsInPath[i];
                if (tw.Position == -1) continue;
                if (tw.Position == position) continue;
                distance += Math.Abs(position - tw.Position);
                position = tw.Position;
            }

            return distance;
        }

        static double ComputeOrderProb(CandidateSequence path)
        {
            List<TargetWord> wordsInPath = new List<TargetWord>();
            GetWordsInPath(path, wordsInPath);
            int violations = 0;
            int countedWords = 1;

            int position = GetInitialPosition(wordsInPath);

            for (int i = 0; i < wordsInPath.Count; i++)
            {
                TargetWord tw = wordsInPath[i];
                if (tw.Position == -1) continue;
                if (tw.Position == position) continue;
                if (tw.Position < position)
                {
                    violations++;
                }
                countedWords++;
                position = tw.Position;
            }

            double prob = 1.0 - (double)violations / (double)countedWords;
            return Math.Log(prob);
        }

        static int GetInitialPosition(List<TargetWord> wordsInPath)
        {
            int initialPosition = 0;

            foreach(TargetWord tWord in wordsInPath)
            {
                if (tWord.Position >= 0)
                {
                    initialPosition = tWord.Position;
                    break;
                }
            }

            return initialPosition;
        }

        public static void GetWordsInPath(CandidateSequence path, List<TargetWord> wordsInPath)
        {
            path.Match(
                targetWords =>
                {
                    foreach (TargetWord tWord in targetWords)
                    {
                        wordsInPath.Add(tWord);
                    }
                },
                candidates =>
                {
                    foreach (Candidate c in candidates)
                    {
                        GetWordsInPath(c.Sequence, wordsInPath);
                    }
                });
        }

        // childCandidateList = ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        // returns a list of paths, which also has the type
        // ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        static List<List<Candidate>> CreatePaths(List<List<Candidate>> childCandidatesList, int maxPaths)
        {
 //           int arcsLimit = 2000000;
            int maxArcs = GetMaxArcs(childCandidatesList); // product of all sub-list lengths
            int maxDepth = GetMaxDepth(childCandidatesList); // maximum sub-list length
            if (maxArcs > maxPaths || maxArcs <= 0)
            {
                double root = Math.Pow((double)maxPaths, 1.0 / childCandidatesList.Count);
                maxDepth = (int)root;
            }

            ArrayList depth_N_paths = new ArrayList();
            try
            {
                depth_N_paths = Create_Depth_N_paths(childCandidatesList, maxDepth);
            }
            catch
            {
                depth_N_paths = CreatePaths(childCandidatesList, maxPaths / 2);
            }

            return depth_N_paths;
        }


        // childCandidateList = ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        // returns a list of paths, which also has the type
        // ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        static ArrayList Create_Depth_N_paths(ArrayList childCandidatesList, int depth)
        {
            ArrayList paths = new ArrayList();

            if (childCandidatesList.Count > 1)
            {
                if (paths.Count > 16000000)  // seems like this can never happen ...
                {
                    return paths;
                }
                ArrayList headCandidates = (ArrayList)childCandidatesList[0];
                // ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })

                int headDepth = headCandidates.Count - 1;
                if (headDepth > depth)
                {
                    headDepth = depth;
                }
                // headDepth is one less than number of head candidates,
                // but truncated to depth

                ArrayList nHeadCandidates = Get_Nth_Candidate(headCandidates, headDepth);
                // nHeadCandidates = first headDepth members of headCandidates

                ArrayList tailCandidatesList = (ArrayList)childCandidatesList.Clone();
                tailCandidatesList.Remove(headCandidates);
                // tailCandidatesList = the remaining members of childCandidatesList

                ArrayList tailPaths = Create_Depth_N_paths(tailCandidatesList, depth);
                // (recursive call)

                for (int i = 0; i < nHeadCandidates.Count; i++) // for each member of nHeadCandidates
                {
                    Candidate nHeadCandidate = (Candidate)nHeadCandidates[i];
                    // nHeadCandidate :: Candidate{ Sequence ArrayList(TargetWord), Prob double }

                    for (int j = 0; j < tailPaths.Count; j++)  // for each tail path
                    { 
                        ArrayList tailPath = (ArrayList)tailPaths[j];
                        ArrayList path = CombinePath(nHeadCandidate, tailPath);
                        // path is copy of tailPath with nHeadCandidate prepended.

                        if (paths.Count > 16000000)
                        {
                            return paths;
                        }
                        paths.Add(path);
                    }
                }
            }
            else
            {
                if (paths.Count > 16000000)  // seems like this can never happen
                {
                    return paths;
                }
                ArrayList candidates = (ArrayList)childCandidatesList[0];
                for (int i = 0; i < candidates.Count && i <= depth; i++)
                {
                    Candidate candidate = (Candidate)candidates[i];
                    ArrayList path = new ArrayList();
                    path.Add(candidate);
                    paths.Add(path);
                }

                // Puts each candidate into its own path.
            }

            return paths;
        }

        static int GetMaxDepth(ArrayList childCandidatesList)
        {
            int max = 0;

            foreach(ArrayList candidates in childCandidatesList)
            {
                if (candidates.Count > max) max = candidates.Count;
            }

            return max;
        }

        static int GetMaxArcs(ArrayList childCandidatesList)
        {
            int max = 1;

            foreach (ArrayList candidates in childCandidatesList)
            {
                max *= candidates.Count;
            }

            return max;
        }

        // headCandidates :: ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })
        // result is copy of initial segment of the list, of length depth
        //
        static ArrayList Get_Nth_Candidate(ArrayList headCandidates, int depth)
        {
            ArrayList nCandidates = new ArrayList();

            for (int i = 0; i <= depth; i++)
            {
                Candidate c = (Candidate)headCandidates[i];
                nCandidates.Add(c);
            }

            return nCandidates;
        }

        // prepends headCandidate to a copy of tailPath to obtain result
        static ArrayList CombinePath(Candidate headCandidate, ArrayList tailPath)
        {
            ArrayList path = new ArrayList();

            path.Add(headCandidate);

            foreach (Candidate tailCandidate in tailPath)
            {
                path.Add(tailCandidate);
            }

            return path;
        }


        static List<Candidate> GetTopPaths2(
            List<CandidateSequence> paths,
            Dictionary<CandidateSequence, double> probs)
        {
            List<Candidate> topCandidates = new List<Candidate>();

            double topProb = 10;

            for (int i = 0; i < paths.Count; i++)
            {
                List<Candidate> path = paths[i];
                Candidate c = new Candidate();
                c.Prob = probs[path];
                c.Sequence = path;
                if (topProb == 10) topProb = c.Prob;
                if (c.Prob < topProb) break;
                topCandidates.Add(c);
            }

            return topCandidates;
        }

        static List<SourceWord> GetSourceWords(
            string[] words,
            Dictionary<string, WordInfo> wordInfoTable)
        {
            List<SourceWord> wordList = new List<SourceWord>();

            Hashtable wordCount = new Hashtable();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                SourceWord w = new SourceWord();
                w.ID = word.Substring(word.LastIndexOf("_") + 1);
                WordInfo wi = (WordInfo)wordInfoTable[w.ID];
                w.Text = wi.Surface;
                w.Lemma = wi.Lemma;
                w.Morph = wi.Morph;
                w.Cat = wi.Cat;
                w.Strong = wi.Lang + wi.Strong;
                
                if (wordCount.ContainsKey(w.Text))
                {
                    int count = (int)wordCount[w.Text];
                    count++;
                    w.AltID = w.Text + "-" + count;
                    wordCount[w.Text] = count;
                }
                else
                {
                    w.AltID = w.Text + "-" + 1;
                    wordCount.Add(w.Text, 1);
                }
                w.Position = i;
                wordList.Add(w);
            }

            return wordList;
        }

        static List<TargetWord> GetTargetWords(string[] words, string[] words2)
        {
            List<TargetWord> wordList = new List<TargetWord>();

            Hashtable wordCount = new Hashtable();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string word2 = words2[i];
                TargetWord w = new TargetWord();

                w.Text = word.Substring(0, word.LastIndexOf("_"));
                w.Text2 = word2.Substring(0, word2.LastIndexOf("_"));
                w.ID = word.Substring(word.LastIndexOf("_") + 1);
                if (wordCount.ContainsKey(w.Text2))
                {
                    int count = (int)wordCount[w.Text2];
                    count++;
                    w.AltID = w.Text2 + "-" + count;
                    wordCount[w.Text2] = count;
                }
                else
                {
                    w.AltID = w.Text2 + "-" + 1;
                    wordCount.Add(w.Text2, 1);
                }
                w.Position = i;
                w.RelativePos = (double)w.Position / (double)words.Length;
                wordList.Add(w);
            }

            return wordList;
        }

        public static string GetVerseID(string word)
        {
            return word.Substring(word.LastIndexOf("_") + 1, 8);
        }

        public static List<Candidate> CreateEmptyCandidate()
        {
            List<Candidate> candidates = new List<Candidate>();
            Candidate c = new Candidate();
            c.Prob = 0.0;
            candidates.Add(c);

            return candidates;
        }

        static string GetChapterID(string targetVerse)
        {
            string firstWord = string.Empty;
            if (targetVerse.Contains(" "))
            {
                firstWord = targetVerse.Substring(0, targetVerse.IndexOf(" "));
            }
            else
            {
                firstWord = targetVerse;
            }
            return firstWord.Substring(firstWord.LastIndexOf("_") + 1, 5);
        }

        static List<string> GetSourceNodes(XmlNode treeNode)
        {
            List<string> sourceNodes = new List<string>();

            List<XmlNode> terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);
            foreach(XmlNode terminalNode in terminalNodes)
            {
                string cat = Utils.GetAttribValue(terminalNode, "Cat");
                string morphId = Utils.GetAttribValue(terminalNode, "morphId");
                string lemma = Utils.GetAttribValue(terminalNode, "Lemma");
                if (morphId.Length == 11) morphId += "1";
                sourceNodes.Add(morphId);
            }

            return sourceNodes;
        }

        public static Candidate GetWinningCandidate(
            List<Candidate> conflictingCandidates)
        {
            double topProb = GetTopProb(conflictingCandidates);
            int count = CountTopProb(conflictingCandidates, topProb);
            if (count == 1) // there is a unique candidate
            {
                Candidate winningCandidate = GetWinningCandidate(topProb, conflictingCandidates);
                return winningCandidate;
            }

            return null;
        }

        static Candidate GetWinningCandidate(double topProb, ArrayList conflictingCandidates)
        {
            Candidate winningCandidate = null;

            foreach (Candidate c in conflictingCandidates)
            {
                if (c.Prob == topProb)
                {
                    winningCandidate = c;
                    break;
                }
            }

            return winningCandidate;
        }

        static double GetTopProb(ArrayList conflictingCandidates)
        {
            Candidate firstCandidate = (Candidate)conflictingCandidates[0];
            double topProb = firstCandidate.Prob;

            foreach(Candidate c in conflictingCandidates)
            {
                if (c.Prob > topProb)
                {
                    topProb = c.Prob;
                }
            }

            return topProb;
        }

        static int CountTopProb(ArrayList conflictingCandidates, double topProb)
        {
            int count = 0;

            foreach (Candidate c in conflictingCandidates)
            {
                if (c.Prob == topProb)
                {
                    count++;
                }
            }

            return count;
        }

        public static TargetWord CreateFakeTargetWord()
        {
            TargetWord tWord = new TargetWord();
            tWord.Text = string.Empty;
            tWord.Position = -1;
            tWord.IsFake = true;
            tWord.ID = "0";

            return tWord;
        }

        public static XmlNode GetTreeNode(string sStartVerseID, string sEndVerseID, Dictionary<string, XmlNode> trees)
        {
            XmlNode treeNode = null;

            ArrayList subTrees = GetSubTrees(sStartVerseID, sEndVerseID, trees);
            if (subTrees.Count == 1)
            {
                treeNode = (XmlNode)subTrees[0];
            }
            else
            {
                treeNode = VerseTrees.CombineTrees(subTrees);
            }

            return treeNode;
        }

        static ArrayList GetSubTrees(string sStartVerseID, string sEndVerseID, Dictionary<string, XmlNode> trees)
        {
            ArrayList subTrees = new ArrayList();

            string book = sStartVerseID.Substring(0, 2);
            string startChapter = sStartVerseID.Substring(2, 3);
            string endChapter = sEndVerseID.Substring(2, 3);

            if (startChapter == endChapter)
            {
                GetSubTreesInSameChapter(sStartVerseID, sEndVerseID, book, startChapter, ref subTrees, trees);
            }
            else
            {
                GetSubTreesInDiffChapter(sStartVerseID, sEndVerseID, book, startChapter, endChapter, ref subTrees, trees);
            }

            return subTrees;
        }

        static void GetSubTreesInSameChapter(string sStartVerseID, string sEndVerseID, string book, string chapter, ref ArrayList subTrees, Dictionary<string, XmlNode> trees)
        {
            int startVerse = Int32.Parse(sStartVerseID.Substring(5, 3));
            int endVerse = Int32.Parse(sEndVerseID.Substring(5, 3));

            for (int i = startVerse; i <= endVerse; i++)
            {
                string verseID = book + chapter + Utils.Pad3(i.ToString());
                XmlNode subTree = (XmlNode)trees[verseID];
                if (subTree != null)
                {
                    subTrees.Add(subTree);
                }
                else
                {
                    break;
                }
            }
        }

        static ArrayList GetMatchingTwords(Hashtable wordIds, List<TargetWord> tWords)
        {
            ArrayList matchingTwords = new ArrayList();

            foreach(TargetWord tWord in tWords)
            {
                if (wordIds.ContainsKey(tWord.ID))
                {
                    matchingTwords.Add(tWord);
                }
            }

            return matchingTwords;
        }

        static void GetSubTreesInDiffChapter(string sStartVerseID, string sEndVerseID, string book, string chapter1, string chapter2, ref ArrayList subTrees, Dictionary<string, XmlNode> trees)
        {
            string hypotheticalLastVerse = book + chapter1 + "100";
            GetSubTreesInSameChapter(sStartVerseID, hypotheticalLastVerse, book, chapter1, ref subTrees, trees);
            string hypotheticalFirstVerse = book + chapter2 + "001";
            GetSubTreesInSameChapter(hypotheticalFirstVerse, sEndVerseID, book, chapter2, ref subTrees, trees);
        }
    }

    public class SourceWord
    {
        public string ID;
        public string AltID;
        public string Text;
        public string Lemma;
        public string Gloss;
        public int Position;
        public string Category;
        public bool IsFunctionWord;
        public double RelativePos;
        public string Morph;
        public string Strong;
        public string Cat;
    }

    public class TargetWord
    {
        public string ID;
        public string AltID;
        public string Text;
        public string Text2;
        public int Position;
        public bool IsFake;
        public double RelativePos;
        public bool InGroup;
    }

    


    public class CandidateSequence
    {
        // Either List<Candidate>, List<TargetWord>, or null.
        //
        private object sequence;

        public CandidateSequence()
        {
        }

        public CandidateSequence(List<Candidate> subcandidates)
        {
            sequence = subcandidates;
        }

        public CandidateSequence(List<TargetWord> targetWords)
        {
            sequence = targetWords;
        }

        public int Count
        {
            get
            {
                switch (sequence)
                {
                    case null:
                        return 0;

                    case List<Candidate> sequenceC:
                        return sequenceC.Count;

                    case List<TargetWord> sequenceTW:
                        return sequenceTW.Count;

                    default:
                        throw new InvalidOperationException("DEFECT");
                }
            }
        }

        public void Match(
            Action<List<TargetWord>> actionTargetWords,
            Action<List<Candidate>> actionCandidates)
        {
            switch (sequence)
            {
                case List<TargetWord> sequenceTW:
                    actionTargetWords(sequenceTW);
                    break;

                case List<Candidate> sequenceC:
                    actionCandidates(sequenceC);
                    break;

                case null:
                    actionCandidates(new List<Candidate>());
                    break;

                default:
                    throw new InvalidOperationException("DEFECT");
            }
        }

        public void Add(TargetWord targetWord)
        {
            switch (sequence)
            {
                case null:
                    List<TargetWord> targetWords = new List<TargetWord>();
                    targetWords.Add(targetWord);
                    sequence = targetWords;
                    break;

                case List<TargetWord> sequenceTW:
                    sequenceTW.Add(targetWord);
                    break;

                default:
                    throw new InvalidOperationException("DEFECT");
            }
        }

        public void Add(Candidate candidate)
        {
            switch (sequence)
            {
                case null:
                    List<Candidate> subCandidates = new List<Candidate>();
                    subCandidates.Add(candidate);
                    sequence = subCandidates;
                    break;

                case List<Candidate> sequenceC:
                    sequenceC.Add(candidate);
                    break;

                default:
                    throw new InvalidOperationException("DEFECT");
            }
        }
    }


    public class Candidate
    {
        public double Prob { get; set; }
        public CandidateSequence Sequence { get; set; }
            = new CandidateSequence();
    }
}
