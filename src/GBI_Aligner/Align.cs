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
            Hashtable oldLinks, // Hashtable(verseID => Hashtable(mWord.altId => tWord.altId))
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
            Hashtable oldLinks,  // Hashtable(verseID => Hashtable(mWord.altId => tWord.altId))
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
           
            ArrayList sWords = GetSourceWords(sourceWords, sourceWords2, wordInfoTable);
            // ArrayList(SourceWord)
            // sourceWords2 not actually used

            // TIM Study
            // TimUtil.PrintArrayList("sWords", sWords);
           
            ArrayList tWords = GetTargetWords(targetWords, targetWords2);
            // ArrayList(TargetWord)
            // targetWords not actually used

            // TIM Study
            // TimUtil.PrintArrayList("tWords", tWords);

            Hashtable idMap = OldLinks.CreateIdMap(sWords);  // HashTable(SourceWord.ID => SourceWord.AltID)

            string verseNodeID = Utils.GetAttribValue(treeNode, "nodeId");
            verseNodeID = verseNodeID.Substring(0, verseNodeID.Length - 1);
            string verseID = verseNodeID.Substring(0, 8);
            if (verseID == "41002004")
            {
                ;
            }

            Hashtable existingLinks = new Hashtable();
            if (oldLinks.ContainsKey(verseID))  // verseID as obtained from tree
            {
                existingLinks = (Hashtable)oldLinks[verseID];
                // Hashtable(mWord.altId => tWord.altId)
            }

            AlternativesForTerminals terminalCandidates =
                new AlternativesForTerminals();
            TerminalCandidates.GetTerminalCandidates(
                terminalCandidates, treeNode, tWords, model, manModel,
                alignProbs, useAlignModel, n, verseID, puncs, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                existingLinks, idMap, sourceFuncWords, contentWordsOnly,
                strongs);
            
            Hashtable alignments = new Hashtable();
            AlignNodes(
                treeNode, tWords, alignments, n, sourceWords.Length,
                maxPaths, terminalCandidates);

            // alignments :: Hashtable(nodeId =>
            //   ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })
            //   or Candidate)

            ArrayList verseAlignment = (ArrayList) alignments[verseNodeID];
            Candidate topCandidate = (Candidate)verseAlignment[0];

            // TIM Study
            // TimUtil.PrintAsJson("verseAlignment", verseAlignment);


            string linkedWords = GetWords(topCandidate);
            //Console.WriteLine($"\nGetWords(topCandidate) = {linkedWords}\n");


            ArrayList terminals = Terminals.GetTerminalXmlNodes(treeNode);
            ArrayList links = Align2.AlignTheRest(topCandidate, terminals, sourceWords, targetWords, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);
            // links :: ArrayList(MappedWords)

            links = Groups.WordsToGroups(links);
            // links :: ArrayList(MappedGroup)

            Groups.AlignGroups(ref links, sWords, tWords, groups, terminals);
            Align2.FixCrossingLinks(ref links);
            //            Output.WriteAlignment(links, sourceWords, targetWords2, ref align, i, wordInfoTable, groups);
            Output.WriteAlignment(links, sWords, tWords, ref align, i, glossTable, groups);
        }

        static void AlignNodes(
            XmlNode treeNode,
            ArrayList tWords, // ArrayList(TargetWord)

            Hashtable alignments, // Hashtable(nodeId =>
                                      //   ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })
                                      //   or Candidate)

            int n, // number of target tokens
            int sLength, // number of source words
            int maxPaths,
            AlternativesForTerminals terminalCandidates
            )
        {
            if (treeNode.NodeType.ToString() == "Text") // child of a terminal node
            {
                return;
            }

            // string nodeString = TimUtil.DebugTreeToString(treeNode);

            // Recursive calls.
            //
            foreach(XmlNode subTree in treeNode)
            {
                AlignNodes(
                    subTree, tWords, alignments, n, sLength,
                    maxPaths, terminalCandidates);
            }

            string nodeID = Utils.GetAttribValue(treeNode, "nodeId");
            nodeID = nodeID.Substring(0, nodeID.Length - 1);

            if (treeNode.FirstChild.NodeType.ToString() == "Text") // terminal node
            {
                string morphId = Utils.GetAttribValue(treeNode, "morphId");
                if (morphId.Length == 11)
                {
                    morphId += "1";
                }

                alignments.Add(nodeID, terminalCandidates[morphId]);
            }   
            else if (treeNode.ChildNodes.Count > 1)  // non-terminal with multiple children
            {
                // (John 1:1 first node: nodeId="430010010010171")
                //
                string getNodeId(XmlNode node)
                {
                    string childNodeID = Utils.GetAttribValue(node, "nodeId");
                    if (childNodeID.Length == 15)
                    {
                        return childNodeID.Substring(0, childNodeID.Length - 1);
                    }
                    else
                    {
                        return childNodeID.Substring(0, childNodeID.Length - 2);
                    }
                }

                ArrayList makeNonEmpty(ArrayList list) =>
                    list.Count == 0
                    ? CreateEmptyCandidate()
                    : list;

                ArrayList candidatesForNode(XmlNode node) =>
                    makeNonEmpty((ArrayList)alignments[getNodeId(node)]);

                ArrayList candidates = new ArrayList(
                    treeNode
                    .ChildNodes
                    .Cast<XmlNode>()
                    .Select(childNode => candidatesForNode(childNode))
                    .ToList());

                ArrayList sNodes = GetSourceNodes(treeNode);

                alignments[nodeID] = ComputeTopCandidates(
                    candidates, n, maxPaths, sNodes, treeNode);
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
        public static AlternativeCandidates GetTopCandidates(
            SourceWord sWord,
            ArrayList tWords, // ArrayList(TargetWord)
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
            Hashtable existingLinks, // Hashtable(mWord.altId => tWord.altId)
                                     // it gets used here
            ArrayList sourceFuncWords,
            bool contentWordsOnly, // (not actually used)
            Hashtable strongs
            )
        {
            AlternativeCandidates topCandidates = new AlternativeCandidates();

            if (existingLinks.Count > 0 && sWord.AltID != null && existingLinks.ContainsKey(sWord.AltID))
            {
                string targetAltID = (string)existingLinks[sWord.AltID];
                TargetWord target = OldLinks.GetTarget(targetAltID, tWords);
                if (target != null)
                {
                    Candidate c = new Candidate(target, 0.0);
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
                    Candidate c = new Candidate(target, 0.0);
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
            topCandidates = new AlternativeCandidates(
                GetCandidatesWithSpecifiedProbability(bestProb, probs));

            foreach (Candidate c in topCandidates)
            {
                string linkedWords = GetWords(c);
            }

            return topCandidates;
        }



        public static bool IsContentWord(
            string lemma,
            ArrayList sourceFuncWords)
            => !sourceFuncWords.Contains(lemma);
 

        // the values of the Hashtable are probabilities that are doubles
        static double FindBestProb(Hashtable probs)
        {
            return probs
                .Cast<DictionaryEntry>()
                .Select(kvp => (double)kvp.Value)
                .Concat(Enumerable.Repeat(-10.0, 1))
                .Max();
        }



        static List<Candidate> GetCandidatesWithSpecifiedProbability(double bestProb, Hashtable probs)
        {
            return probs
                .Cast<DictionaryEntry>()
                .Where(kvp => (double)kvp.Value == bestProb)
                .Select(kvp => new Candidate(
                    (TargetWord)kvp.Key,
                    (double)kvp.Value))
                .ToList();
        }


        // childCandidateList = ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        // returns ArrayList(Candidate)
        //
        static ArrayList ComputeTopCandidates(ArrayList childCandidateList, int n, int maxPaths, ArrayList sNodes, XmlNode treeNode)
        {
            // I think that childCandidateList is a list of alternatives ...

            Dictionary<CandidateChain, double> pathProbs =
                new Dictionary<CandidateChain, double>();

            List<CandidateChain> allPaths = CreatePaths(childCandidateList, maxPaths);
            // allPaths :: ArrayList(ArrayList(Candidate))

            List<CandidateChain> paths = FilterPaths(allPaths);
            // paths :: ArrayList(ArrayList(Candidate))
            // paths = those where the candidates use different words

            if (paths.Count == 0)
            {
                CandidateChain topPath = allPaths[0];
                paths.Add(topPath);
            }

            ArrayList topCandidates = new ArrayList();

            foreach (CandidateChain path in paths)
            {
                // path :: ArrayList(Candidate)
                double jointProb = ComputeJointProb(path); // sum of candidate probabilities in a path
                try
                {
                    pathProbs.Add(path, jointProb);
                }
                catch
                {
                    Console.WriteLine("Hashtable out of memory.");

                    // ArrayList sortedCandidates2 = Sort.SortTableDoubleDesc(pathProbs);

                    List<CandidateChain> sortedCandidates2 =
                            pathProbs
                                .OrderByDescending(kvp => (double)kvp.Value)
                                .Select(kvp => kvp.Key)
                                .ToList();

                    int topN2 = sortedCandidates2.Count / 10;
                    if (topN2 < n) topN2 = n;
                    //                    topCandidates = GetTopPaths(sortedCandidates2, pathProbs, topN2);
                    topCandidates = GetTopPaths2(sortedCandidates2, pathProbs);
                    return topCandidates;
                }
            }

            // pathProbs :: Hashtable(ArrayList(Candidate), jointProb)

            Dictionary<CandidateChain, double> pathProbs2 = AdjustProbsByDistanceAndOrder(pathProbs);

            // pathProbs :: Hashtable(ArrayList(Candidate), revisedProb)

            List<CandidateChain> sortedCandidates = Data.SortPaths(pathProbs2);
            // sortedCandidates :: ArrayList(Candidate)

            // (topN not actually used)
            int topN = sortedCandidates.Count / 10;
            if (topN < n) topN = n;

            topCandidates = GetTopPaths2(sortedCandidates, pathProbs);
            // topCandidates :: ArrayList(Candidate)
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
        static Dictionary<CandidateChain, double>
            AdjustProbsByDistanceAndOrder(
                Dictionary<CandidateChain, double> pathProbs)
        {
            Dictionary<CandidateChain, double> pathProbs2 =
                new Dictionary<CandidateChain, double>();

            ArrayList candidates = new ArrayList(); // ::= ArrayList(Candidate)

            foreach (var pathEnum in pathProbs)
            {
                Candidate candidate = new Candidate(
                    pathEnum.Key,
                    (double)pathEnum.Value);
                candidates.Add(candidate);
            }

            int minimalDistance = 10000;
            foreach (Candidate c in candidates)
            {
                int distance = ComputeDistance(c.Chain); // something about distance between words
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
                    int distance = ComputeDistance(c.Chain);
                    double distanceProb = Math.Log((double)minimalDistance / (double)distance);
                    double orderProb = ComputeOrderProb(c.Chain);  // something about word order
                    double adjustedProb = c.Prob + c.Prob + distanceProb + orderProb / 2.0;
                    c.Prob = adjustedProb;
                    pathProbs2.Add(c.Chain, adjustedProb);
                }
            }
            else if (candidates.Count > 0)
            {
                pathProbs2 = pathProbs;
            }

            return pathProbs2;
        }

        // returns "text1-posn1 text2-posn2 ..."
        //
        public static string GetWords(Candidate c)
        {
            ArrayList wordsInPath = GetTargetWordsInPath(c.Chain);

            string words = string.Empty;

            foreach(TargetWord wordInPath in wordsInPath)
            {
                words += wordInPath.Text + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }

        public static string GetWordsInPath(ArrayList path)
        {
            ArrayList wordsInPath = GetTargetWordsInPath(path);

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
        static List<CandidateChain> FilterPaths(List<CandidateChain> paths)
        {
            List<CandidateChain> filteredPaths = new List<CandidateChain>();

            foreach(CandidateChain path in paths)
            {
                if (IsValidPath(path))
                {
                    filteredPaths.Add(path);
                }
            }

            return filteredPaths;
        }


        static bool IsValidPath(ArrayList path)
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

        static double ComputeJointProb(ArrayList path)
        {
            double jointProb = 0.0;

            foreach(Candidate c in path)
            {
                jointProb += c.Prob;
            }

            return jointProb;
        }

        static int ComputeDistance(ArrayList path)
        {

            ArrayList wordsInPath = GetTargetWordsInPath(path);

            int distance = 0;

            int position = GetInitialPosition(wordsInPath);

            for (int i = 0; i < wordsInPath.Count; i++)
            {
                TargetWord tw = (TargetWord)wordsInPath[i];
                if (tw.Position == -1) continue;
                if (tw.Position == position) continue;
                distance += Math.Abs(position - tw.Position);
                position = tw.Position;
            }

            return distance;
        }

        static double ComputeOrderProb(ArrayList path)
        {
            //ArrayList wordsInPath = new ArrayList();
            //GetWordsInPath(path, ref wordsInPath);

            ArrayList wordsInPath = GetTargetWordsInPath(path);

            int violations = 0;
            int countedWords = 1;

            int position = GetInitialPosition(wordsInPath);

            for (int i = 0; i < wordsInPath.Count; i++)
            {
                TargetWord tw = (TargetWord)wordsInPath[i];
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

        static int GetInitialPosition(ArrayList wordsInPath)
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

        //public static void GetWordsInPath(ArrayList path, ref ArrayList wordsInPath)
        //{
        //    ArrayList words = new ArrayList();

        //    if (path.Count == 0)
        //    {
        //        TargetWord tWord = CreateFakeTargetWord();
        //    }
        //    else if (path[0] is Candidate)
        //    {
        //        foreach (Candidate c in path)
        //        {
        //            GetWordsInPath(c.Sequence, ref wordsInPath);
        //        }
        //    }
        //    else
        //    {
        //        foreach (TargetWord tWord in path)
        //        {
        //            wordsInPath.Add(tWord);
        //        }
        //    }
        //}


        // returns an ArrayList of TargetWord objects.
        public static ArrayList GetTargetWordsInPath(ArrayList path)
        {
            IEnumerable<TargetWord> helper(ArrayList path)
            {
                if (path.Count == 0)
                {
                    return new TargetWord[] { CreateFakeTargetWord() };
                }
                else if (path[0] is Candidate)
                {
                    return path
                        .Cast<Candidate>()
                        .SelectMany(c => helper(c.Chain));
                }
                else
                {
                    return path.Cast<TargetWord>();
                }
            }


            return new ArrayList(helper(path).ToList());
        }


        // childCandidateList = ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        // returns a list of paths, which also has the type
        // ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        static List<CandidateChain> CreatePaths(ArrayList childCandidatesList, int maxPaths)
        {
 //           int arcsLimit = 2000000;
            int maxArcs = GetMaxArcs(childCandidatesList); // product of all sub-list lengths
            int maxDepth = GetMaxDepth(childCandidatesList); // maximum sub-list length
            if (maxArcs > maxPaths || maxArcs <= 0)
            {
                double root = Math.Pow((double)maxPaths, 1.0 / childCandidatesList.Count);
                maxDepth = (int)root;
            }

            List<CandidateChain> depth_N_paths = new List<CandidateChain>();
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
        static List<CandidateChain> Create_Depth_N_paths(ArrayList childCandidatesList, int depth)
        {
            List<CandidateChain> paths = new List<CandidateChain>();

            // string[] sChildCandidates = childCandidatesList.Cast<ArrayList>().Select(p => GetWordsInPath(p)).ToArray();

            if (childCandidatesList.Count > 1)
            {
                //if (paths.Count > 16000000)  // seems like this can never happen ...
                //{
                //    return paths;
                //}
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

                List<CandidateChain> tailPaths = Create_Depth_N_paths(tailCandidatesList, depth);
                // (recursive call)

                for (int i = 0; i < nHeadCandidates.Count; i++) // for each member of nHeadCandidates
                {
                    Candidate nHeadCandidate = (Candidate)nHeadCandidates[i];
                    // nHeadCandidate :: Candidate{ Sequence ArrayList(TargetWord), Prob double }

                    for (int j = 0; j < tailPaths.Count; j++)  // for each tail path
                    { 
                        ArrayList tailPath = (ArrayList)tailPaths[j];
                        CandidateChain path = ConsChain(nHeadCandidate, tailPath);
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
                //if (paths.Count > 16000000)  // seems like this can never happen
                //{
                //    return paths;
                //}
                ArrayList candidates = (ArrayList)childCandidatesList[0];
                for (int i = 0; i < candidates.Count && i <= depth; i++)
                {
                    Candidate candidate = (Candidate)candidates[i];
                    CandidateChain path = new CandidateChain(Enumerable.Repeat(candidate, 1));
                    paths.Add(path);
                }

                // Puts each candidate into its own path.
            }

            //  string[] sPaths = paths.Cast<ArrayList>().Select(p => GetWordsInPath(p)).ToArray();

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
        static CandidateChain Get_Nth_Candidate(ArrayList headCandidates, int depth)
        {
            return new CandidateChain(
                headCandidates.Cast<Candidate>().Take(depth + 1));
        }

        // prepends head to a copy of tail to obtain result
        static CandidateChain ConsChain(Candidate head, ArrayList tail)
        {
            return new CandidateChain(
                tail.Cast<Candidate>().Prepend(head));
        }


        static ArrayList GetTopPaths2(List<CandidateChain> paths, Dictionary<CandidateChain, double> probs)
        {
            ArrayList topCandidates = new ArrayList();

            double topProb = 10;

            for (int i = 0; i < paths.Count; i++)
            {
                CandidateChain path = paths[i];
                Candidate c = new Candidate(path, (double)probs[path]);
                if (topProb == 10) topProb = c.Prob;
                if (c.Prob < topProb) break;
                topCandidates.Add(c);
            }

            return topCandidates;
        }

        static ArrayList GetSourceWords(
            string[] words,
            string[] words2,
            Dictionary<string, WordInfo> wordInfoTable)
        {
            ArrayList wordList = new ArrayList();

            Hashtable wordCount = new Hashtable();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string word2 = words2[i];
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

        static ArrayList GetTargetWords(string[] words, string[] words2)
        {
            ArrayList wordList = new ArrayList();

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

        public static ArrayList CreateEmptyCandidate()
        {
            ArrayList candidates = new ArrayList();
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

        static ArrayList GetSourceNodes(XmlNode treeNode)
        {
            ArrayList sourceNodes = new ArrayList();

            ArrayList terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);
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

        public static Candidate GetWinningCandidate(ArrayList conflictingCandidates)
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
            //TargetWord tWord = new TargetWord();
            //tWord.Text = string.Empty;
            //tWord.Position = -1;
            //tWord.IsFake = true;
            //tWord.ID = "0";

            //return tWord;

            return new TargetWord()
            {
                Text = string.Empty,
                Position = -1,
                IsFake = true,
                ID = "0"
            };
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
                if (trees.TryGetValue(verseID, out XmlNode subTree))
                {
                    subTrees.Add(subTree);
                }
                else
                {
                    break;
                }
            }
        }

        static ArrayList GetMatchingTwords(Hashtable wordIds, ArrayList tWords)
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

    public class Candidate
    {
        public CandidateChain Chain;
        public double Prob;

        public Candidate()
        {
            Chain = new CandidateChain();
        }

        public Candidate(TargetWord tw, double probability)
        {
            Chain = new CandidateChain(Enumerable.Repeat(tw, 1));
            Prob = probability;
        }

        public Candidate(CandidateChain chain, double probability)
        {
            Chain = chain;
            Prob = probability;
        }
    }

    /// <summary>
    /// A CandidateChain is a sequence of TargetWord objects
    /// or a sequence of Candidate objects.
    /// </summary>
    /// 
    public class CandidateChain : ArrayList
    {
        public CandidateChain()
            : base()
        {
        }

        public CandidateChain(IEnumerable<Candidate> candidates)
            : base(candidates.ToList())
        {
        }

        public CandidateChain(IEnumerable<TargetWord> targetWords)
            : base(targetWords.ToList())
        {
        }
    }


    /// <summary>
    /// An AlternativeCandidates object is a list of Candidate
    /// objects that are alternatives to one another.
    /// </summary>
    /// 
    public class AlternativeCandidates : ArrayList
    {
        public AlternativeCandidates()
            : base()
        {
        }

        public AlternativeCandidates(IEnumerable<Candidate> candidates)
            : base(candidates.ToList())
        { 
        }
    }


    /// <summary>
    /// An AlternativesForTerminals object is a mapping:
    /// SourceWord.ID => AlternativeCandidates.
    /// </summary>
    /// 
    public class AlternativesForTerminals : Hashtable
    {
        public AlternativesForTerminals()
            : base()
        {
        }
    }
}
