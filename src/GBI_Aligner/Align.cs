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
            Dictionary<string, Dictionary<string, double>> model,  
            Dictionary<string, Dictionary<string, Stats>> manModel, 
            Dictionary<string, double> alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            Dictionary<string, string> preAlignment, // Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            Dictionary<string, List<TargetGroup>> groups,
            string treeFolder,
            Dictionary<string, string> bookNames,
            string jsonOutput,
            int maxPaths,
			List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks, // Hashtable(link => count)
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks, // Hashtable(verseID => Hashtable(mWord.altId => tWord.altId))
            List<string> sourceFuncWords, 
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
            )
        {
            List<string> sourceVerses = Data.GetVerses(sourceLemma, false);
            List<string> sourceVerses2 = Data.GetVerses(source, false);
            List<string> targetVerses = Data.GetVerses(target, true);
            List<string> targetVerses2 = Data.GetVerses(target, false);

            string prevChapter = string.Empty;

            Dictionary<string, XmlNode> trees = new Dictionary<string, XmlNode>();

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
            Dictionary<string, Dictionary<string, double>> model, // translation model, Hashtable(source => Hashtable(target => probability))
            Dictionary<string, Dictionary<string, Stats>> manModel, // manually checked alignments
                                // Hashtable(source => Hashtable(target => Stats{ count, probability})
            Dictionary<string, double> alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            Dictionary<string, string> preAlignment, // Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel,
            Dictionary<string, List<TargetGroup>> groups, // comes from Data.LoadGroups("groups.txt")
                              //   of the form Hashtable(...source... => ArrayList(TargetGroup{...text..., primaryPosition}))
            Dictionary<string, XmlNode> trees, // verseID => XmlNode
            ref Alignment2 align,  // Output goes here.
            int i,
            int maxPaths,
			List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,  // Hashtable(verseID => Hashtable(mWord.altId => tWord.altId))
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs
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

            Dictionary<string, WordInfo> wordInfoTable =
                Data.BuildWordInfoTable(treeNode);
           
            List<SourceWord> sWords = GetSourceWords(sourceWords, sourceWords2, wordInfoTable);
           
            List<TargetWord> tWords = GetTargetWords(targetWords, targetWords2);

            Dictionary<string, string> idMap = OldLinks.CreateIdMap(sWords);  // HashTable(SourceWord.ID => SourceWord.AltID)

            string verseNodeID = Utils.GetAttribValue(treeNode, "nodeId");
            verseNodeID = verseNodeID.Substring(0, verseNodeID.Length - 1);
            string verseID = verseNodeID.Substring(0, 8);

            Dictionary<string, string> existingLinks = new Dictionary<string, string>();
            if (oldLinks.ContainsKey(verseID))  // verseID as obtained from tree
            {
                existingLinks = oldLinks[verseID];
            }

            AlternativesForTerminals terminalCandidates =
                new AlternativesForTerminals();
            TerminalCandidates.GetTerminalCandidates(
                terminalCandidates, treeNode, tWords, model, manModel,
                alignProbs, useAlignModel, n, verseID, puncs, stopWords,
                goodLinks, goodLinkMinCount, badLinks, badLinkMinCount,
                existingLinks, idMap, sourceFuncWords, contentWordsOnly,
                strongs);
            
            Dictionary<string, List<Candidate>> alignments =
                new Dictionary<string, List<Candidate>>();
            AlignNodes(
                treeNode, tWords, alignments, n, sourceWords.Length,
                maxPaths, terminalCandidates);

            List<Candidate> verseAlignment = alignments[verseNodeID];
            Candidate topCandidate = verseAlignment[0];

            List<XmlNode> terminals = Terminals.GetTerminalXmlNodes(treeNode);
            List<MappedWords> links = Align2.AlignTheRest(topCandidate, terminals, sourceWords, targetWords, model, preAlignment, useAlignModel, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, sourceFuncWords, targetFuncWords, contentWordsOnly);

            List<MappedGroup> links2 = Groups.WordsToGroups(links);

            Groups.AlignGroups(links2, sWords, tWords, groups, terminals);
            Align2.FixCrossingLinks(ref links2);
            Output.WriteAlignment(links2, sWords, tWords, ref align, i, glossTable, groups);
        }

        static void AlignNodes(
            XmlNode treeNode,
            List<TargetWord> tWords,
            Dictionary<string, List<Candidate>> alignments,
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

                List<Candidate> makeNonEmpty(List<Candidate> list) =>
                    list.Count == 0
                    ? CreateEmptyCandidate()
                    : list;

                List<Candidate> candidatesForNode(XmlNode node) =>
                    makeNonEmpty(alignments[getNodeId(node)]);

                List<List<Candidate>> candidates = 
                    treeNode
                    .ChildNodes
                    .Cast<XmlNode>()
                    .Select(childNode => candidatesForNode(childNode))
                    .ToList();

                List<string> sNodes = GetSourceNodes(treeNode);

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
            List<TargetWord> tWords,
            Dictionary<string, Dictionary<string, double>> model,
            Dictionary<string, Dictionary<string, Stats>> manModel,
            Dictionary<string, double> alignProbs, // Hashtable("bbcccvvvwwwn-bbcccvvvwww" => probability)
            bool useAlignModel,
            int n, // number of target tokens (not actually used)
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks, // (not actually used)
            int goodLinkMinCount, // (not actually used)
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, string> existingLinks, // Hashtable(mWord.altId => tWord.altId)
                                     // it gets used here
            List<string> sourceFuncWords,
            bool contentWordsOnly, // (not actually used)
            Dictionary<string, Dictionary<string, int>> strongs
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

            Dictionary<TargetWord, double> probs =
                new Dictionary<TargetWord, double>();

            bool isContentWord = IsContentWord(sWord.Lemma, sourceFuncWords);
            if (!isContentWord) return topCandidates;

            if (strongs.ContainsKey(sWord.Strong))
            {
                Dictionary<string, int> wordIds = strongs[sWord.Strong];
                List<TargetWord> matchingTwords = GetMatchingTwords(wordIds, tWords);
                foreach(TargetWord target in matchingTwords)
                {
                    Candidate c = new Candidate(target, 0.0);
                    topCandidates.Add(c);
                }
                return topCandidates;
            }

            if (manModel.ContainsKey(sWord.Lemma))
            {
                Dictionary<string, Stats> translations = manModel[sWord.Lemma];

                for (int i = 0; i < tWords.Count; i++)
                {
                    TargetWord tWord = tWords[i];
                    if (translations.ContainsKey(tWord.Text))
                    {
                        Stats s = translations[tWord.Text];
                        if (s.Prob < 0.2) s.Prob = 0.2;
                        probs.Add(tWord, Math.Log(s.Prob));
                    }
                }
            }
            else if (model.ContainsKey(sWord.Lemma))
            {
                Dictionary<string, double> translations = model[sWord.Lemma];

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
            List<string> sourceFuncWords)
            => !sourceFuncWords.Contains(lemma);
 

        static double FindBestProb(Dictionary<TargetWord, double> probs)
        {
            return probs
                .Select(kvp => kvp.Value)
                .Concat(Enumerable.Repeat(-10.0, 1))
                .Max();
        }



        static List<Candidate> GetCandidatesWithSpecifiedProbability(double bestProb, Dictionary<TargetWord, double> probs)
        {
            return probs
                .Where(kvp => kvp.Value == bestProb)
                .Select(kvp => new Candidate(
                    kvp.Key,
                    kvp.Value))
                .ToList();
        }


        static List<Candidate> ComputeTopCandidates(List<List<Candidate>> childCandidateList, int n, int maxPaths, List<string> sNodes, XmlNode treeNode)
        {
            // I think that childCandidateList is a list of alternatives ...

            Dictionary<CandidateChain, double> pathProbs =
                new Dictionary<CandidateChain, double>();

            List<CandidateChain> allPaths = CreatePaths(childCandidateList, maxPaths);

            List<CandidateChain> paths = FilterPaths(allPaths);
            // paths = those where the candidates use different words

            if (paths.Count == 0)
            {
                CandidateChain topPath = allPaths[0];
                paths.Add(topPath);
            }

            List<Candidate> topCandidates = new List<Candidate>();

            foreach (CandidateChain path in paths)
            {
                double jointProb = ComputeJointProb(path); // sum of candidate probabilities in a path
                try
                {
                    pathProbs.Add(path, jointProb);
                }
                catch
                {
                    Console.WriteLine("Hashtable out of memory.");

                    List<CandidateChain> sortedCandidates2 =
                            pathProbs
                                .OrderByDescending(kvp => (double)kvp.Value)
                                .Select(kvp => kvp.Key)
                                .ToList();

                    int topN2 = sortedCandidates2.Count / 10;
                    if (topN2 < n) topN2 = n;

                    topCandidates = GetTopPaths2(sortedCandidates2, pathProbs);
                    return topCandidates;
                }
            }

            Dictionary<CandidateChain, double> pathProbs2 =
                AdjustProbsByDistanceAndOrder(pathProbs);

            List<CandidateChain> sortedCandidates = Data.SortPaths(pathProbs2);

            topCandidates = GetTopPaths2(sortedCandidates, pathProbs);

            return topCandidates;
        }


        static Dictionary<CandidateChain, double>
            AdjustProbsByDistanceAndOrder(
                Dictionary<CandidateChain, double> pathProbs)
        {
            Dictionary<CandidateChain, double> pathProbs2 =
                new Dictionary<CandidateChain, double>();

            List<Candidate> candidates = new List<Candidate>();

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
                int distance = ComputeDistance(c.Chain); 
                if (distance < minimalDistance) minimalDistance = distance;
            }

            if (minimalDistance > 0)
            {
                foreach (Candidate c in candidates)
                {
                    string linkedWords = GetWords(c);                 
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
            List<TargetWord> wordsInPath = GetTargetWordsInPath(c.Chain);

            string words = string.Empty;

            foreach(TargetWord wordInPath in wordsInPath)
            {
                words += wordInPath.Text + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }

        public static string GetWordsInPath(ArrayList path)
        {
            List<TargetWord> wordsInPath = GetTargetWordsInPath(path);

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

            List<TargetWord> wordsInPath = GetTargetWordsInPath(path);

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

            List<TargetWord> wordsInPath = GetTargetWordsInPath(path);

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


        public static List<TargetWord> GetTargetWordsInPath(ArrayList path)
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


            return helper(path).ToList();
        }


        // childCandidateList = ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        // returns a list of paths, which also has the type
        // ArrayList(ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double }))
        //
        static List<CandidateChain> CreatePaths(List<List<Candidate>> childCandidatesList, int maxPaths)
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
        static List<CandidateChain> Create_Depth_N_paths(List<List<Candidate>> childCandidatesList, int depth)
        {
            List<CandidateChain> paths = new List<CandidateChain>();

            // string[] sChildCandidates = childCandidatesList.Cast<ArrayList>().Select(p => GetWordsInPath(p)).ToArray();

            if (childCandidatesList.Count > 1)
            {
                //if (paths.Count > 16000000)  // seems like this can never happen ...
                //{
                //    return paths;
                //}
                List<Candidate> headCandidates = childCandidatesList[0];
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

                List<List<Candidate>> tailCandidatesList = childCandidatesList.ToList();
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
                List<Candidate> candidates = childCandidatesList[0];
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

        static int GetMaxDepth(List<List<Candidate>> childCandidatesList)
        {
            int max = 0;

            foreach(List<Candidate> candidates in childCandidatesList)
            {
                if (candidates.Count > max) max = candidates.Count;
            }

            return max;
        }

        static int GetMaxArcs(List<List<Candidate>> childCandidatesList)
        {
            int max = 1;

            foreach (List<Candidate> candidates in childCandidatesList)
            {
                max *= candidates.Count;
            }

            return max;
        }

        // headCandidates :: ArrayList(Candidate{ Sequence ArrayList(TargetWord), Prob double })
        // result is copy of initial segment of the list, of length depth
        //
        static CandidateChain Get_Nth_Candidate(List<Candidate> headCandidates, int depth)
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


        static List<Candidate> GetTopPaths2(List<CandidateChain> paths, Dictionary<CandidateChain, double> probs)
        {
            List<Candidate> topCandidates = new List<Candidate>();

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

        static List<SourceWord> GetSourceWords(
            string[] words,
            string[] words2,
            Dictionary<string, WordInfo> wordInfoTable)
        {
            List<SourceWord> wordList = new List<SourceWord>();

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

        static List<TargetWord> GetMatchingTwords(Dictionary<string, int> wordIds, List<TargetWord> tWords)
        {
            List<TargetWord> matchingTwords = new List<TargetWord>();

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
    public class AlternativeCandidates : List<Candidate>
    {
        public AlternativeCandidates()
            : base()
        {
        }

        public AlternativeCandidates(IEnumerable<Candidate> candidates)
            : base(candidates)
        { 
        }
    }


    /// <summary>
    /// An AlternativesForTerminals object is a mapping:
    /// SourceWord.ID => AlternativeCandidates.
    /// </summary>
    /// 
    public class AlternativesForTerminals : Dictionary<string, List<Candidate>>
    {
        public AlternativesForTerminals()
            : base()
        {
        }
    }
}
