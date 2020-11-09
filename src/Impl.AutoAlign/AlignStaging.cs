﻿using System;
using System.Collections.Generic;
using System.Linq;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.Runtime.CompilerServices;
    using ClearBible.Clear3.Miscellaneous;

    public class AlignStaging
    {
        public static void FixCrossingLinks(ref List<MappedGroup> links)
        {
            var crossingLinks =
                links
                .Where(linkIsOneToOne)
                .GroupBy(lemmaOfSoleSourceWord)
                .Where(links => links.Count() == 2)
                .Select(links => new
                {
                    Link1 = links.ElementAt(0),
                    Link2 = links.ElementAt(1)
                })
                .Where(x => Crossing(x.Link1, x.Link2))
                .Select(x => new
                {
                    Src1Id = idOfSoleSourceWord(x.Link1),
                    Src2Id = idOfSoleSourceWord(x.Link2),
                    Target1 = x.Link1.TargetNodes,
                    Target2 = x.Link2.TargetNodes
                });

            foreach (var x in crossingLinks)
            {
                foreach (MappedGroup mp in links)
                {
                    string sourceId = idOfSoleSourceWord(mp);
                    if (sourceId == x.Src1Id) mp.TargetNodes = x.Target2;
                    if (sourceId == x.Src2Id) mp.TargetNodes = x.Target1;
                }
            }

            string idOfSoleSourceWord(MappedGroup g) =>
                g.SourceNodes[0].MorphID;

            bool linkIsOneToOne(MappedGroup link) =>
                link.SourceNodes.Count == 1 && link.TargetNodes.Count == 1;

            string lemmaOfSoleSourceWord(MappedGroup link) =>
                link.SourceNodes[0].Lemma;
        }


        public static bool Crossing(MappedGroup link1, MappedGroup link2)
        {
            int tpos1 = positionOfSoleWordInTargetGroup(link1);
            int tpos2 = positionOfSoleWordInTargetGroup(link2);

            if (tpos1 < 0 || tpos2 < 0) return false;

            int spos1 = positionOfSoleWordInSourceGroup(link1);
            int spos2 = positionOfSoleWordInSourceGroup(link2);

            return (spos1 < spos2 && tpos1 > tpos2) ||
                (spos1 > spos2 && tpos1 < tpos2);

            int positionOfSoleWordInSourceGroup(MappedGroup g) =>
                g.SourceNodes[0].Position;

            int positionOfSoleWordInTargetGroup(MappedGroup g) =>
                g.TargetNodes[0].Word.Position;
        }
           

        public static List<List<MappedWords>> FindConflictingLinks(List<MappedWords> links)
        {
            return links
                .Where(targetWordNotEmpty)
                .GroupBy(targetTextAndId)
                .Where(group => group.Count() > 1)
                .Select(group => group.ToList())
                .ToList();

            bool targetWordNotEmpty(MappedWords link) =>
                link.TargetNode.Word.Text != string.Empty;

            Tuple<string, string> targetTextAndId(MappedWords link) =>
                Tuple.Create(
                    link.TargetNode.Word.Text,
                    link.TargetNode.Word.ID);
        }


        public static void ResolveConflicts(
            List<List<MappedWords>> conflicts,
            List<MappedWords> links,
            int pass)
        {
            List<MappedWords> linksToRemove =
                conflicts.
                SelectMany(conflict =>
                    conflict.Except(
                        FindWinners(conflict, pass).Take(1)))
                .ToList();

            List<int> toStrikeOut =
                links
                .Select((link, index) => new { link, index })
                .Where(x => linksToRemove.Contains(x.link))
                .Select(x => x.index)
                .ToList();

            foreach (int i in toStrikeOut)
            {               
                strikeOut(i);
            }

            void strikeOut(int i) =>
                links[i] = makeFakeLink(links[i].SourceNode);

            MappedWords makeFakeLink(SourceNode sourceNode) =>
                new MappedWords
                {
                    SourceNode = sourceNode,
                    TargetNode = new LinkedWord()
                    {
                        Prob = -1000,
                        Word = AutoAlignUtility.CreateFakeTargetWord()
                    }
                };
        }


        public static List<MappedWords> FindWinners(
            List<MappedWords> conflict,
            int pass)
        {
            // The winners are the links of maximal probability.
            // (we know that conflict is not the empty list)
            //
            double bestProb = conflict.Max(mw => prob(mw));
            List<MappedWords> winners = conflict
                .Where(mw => mw.TargetNode.Prob == bestProb)
                .ToList();

            // On the second pass, if there are multiple winners,
            // then select the winner where the source and target
            // relative positions are closest in a relative sense.
            //
            if (pass == 2 && winners.Count > 1)
            {
                double minDelta = conflict.Min(mw => relativeDelta(mw));

                MappedWords winner2 = winners
                    .Where(mw => relativeDelta(mw) == minDelta)
                    .FirstOrDefault();

                if (winner2 != null)
                {
                    winners = new List<MappedWords>() { winner2 };
                }
            }

            return winners;

            double prob(MappedWords mw) => mw.TargetNode.Prob;

            double relativeDelta(MappedWords mw) =>
                Math.Abs(mw.SourceNode.RelativePos -
                         mw.TargetNode.Word.RelativePos);         
        }



        public static string GetTargetWordTextFromID(string targetID, List<TargetWord> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Text)
                .DefaultIfEmpty("")
                .First();
        }


        public static int GetTargetPositionFromID(string targetID, List<TargetWord> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Position)
                .DefaultIfEmpty(0)
                .First();
        }


        public static List<TargetWord> GetTargetCandidates(
            MappedWords anchorLink,
            List<TargetWord> targetWords,
            List<string> linkedTargets,
            List<string> puncs,
            List<string> targetFuncWords,
            bool contentWordsOnly)
        {
            int anchor = anchorLink.TargetNode.Word.Position;

            IEnumerable<int> down()
            {
                for (int i = anchor - 1; i >= anchor - 3; i--)
                    yield return i;
            }

            IEnumerable<int> up()
            {
                for (int i = anchor + 1; i <= anchor + 3; i++)
                    yield return i;
            }

            return
                getWords(down())
                .Concat(getWords(up()))
                .ToList();

            IEnumerable<TargetWord> getWords(IEnumerable<int> positions) =>
                PositionsToTargetCandidates(
                    positions,
                    targetWords,
                    linkedTargets,
                    puncs,
                    targetFuncWords,
                    contentWordsOnly);
        }


        public static List<TargetWord> GetTargetCandidates(
            MappedWords leftAnchor,
            MappedWords rightAnchor,
            List<TargetWord> targetWords,
            List<string> linkedTargets,
            List<string> puncs,
            List<string> targetFuncWords,
            bool contentWordsOnly)
        {
            IEnumerable<int> span()
            {
                for (int i = leftAnchor.TargetNode.Word.Position;
                    i < rightAnchor.TargetNode.Word.Position;
                    i++)
                {
                    yield return i;
                }
            }

            return PositionsToTargetCandidates(
                span(),
                targetWords,
                linkedTargets,
                puncs,
                targetFuncWords,
                contentWordsOnly).ToList();
        }


        public static IEnumerable<TargetWord> PositionsToTargetCandidates(
            IEnumerable<int> positions,
            List<TargetWord> targetWords,
            List<string> linkedTargets,
            List<string> puncs,
            List<string> targetFuncWords,
            bool contentWordsOnly)
        {
            return
                positions
                .Where(n => n >= 0 && n < targetWords.Count)
                .Select(n => new { n, tw = targetWords[n] })
                .Select(x => new { x.n, x.tw.Text, x.tw.ID })
                .Where(x => !contentWordsOnly || isContentWord(x.Text))
                .Where(x => isNotLinkedAlready(x.Text))
                .TakeWhile(x => isNotPunctuation(x.Text))
                .Select(x => new TargetWord()
                {
                    ID = x.ID,
                    Position = x.n,
                    Text = x.Text
                })
                .ToList();

            bool isContentWord(string text) =>
                !targetFuncWords.Contains(text);

            bool isNotLinkedAlready(string text) =>
                !linkedTargets.Contains(text);

            bool isNotPunctuation(string text) =>
                !puncs.Contains(text);
        }


        public static List<CandidateChain> CreatePaths(List<List<Candidate>> childCandidatesList, int maxPaths)
        {
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


        public static int GetMaxArcs(List<List<Candidate>> childCandidatesList)
        {
            int max = 1;

            foreach (List<Candidate> candidates in childCandidatesList)
            {
                max *= candidates.Count;
            }

            return max;
        }


        public static int GetMaxDepth(List<List<Candidate>> childCandidatesList)
        {
            int max = 0;

            foreach (List<Candidate> candidates in childCandidatesList)
            {
                if (candidates.Count > max) max = candidates.Count;
            }

            return max;
        }


        public static List<CandidateChain> Create_Depth_N_paths(List<List<Candidate>> childCandidatesList, int depth)
        {
            List<CandidateChain> paths = new List<CandidateChain>();

            if (childCandidatesList.Count > 1)
            {
                List<Candidate> headCandidates = childCandidatesList[0];

                int headDepth = headCandidates.Count - 1;
                if (headDepth > depth)
                {
                    headDepth = depth;
                }
                // headDepth is one less than number of head candidates,
                // but truncated to depth

                CandidateChain nHeadCandidates = Get_Nth_Candidate(headCandidates, headDepth);
                // nHeadCandidates = first headDepth members of headCandidates

                List<List<Candidate>> tailCandidatesList = childCandidatesList.ToList();
                tailCandidatesList.Remove(headCandidates);
                // tailCandidatesList = the remaining members of childCandidatesList

                List<CandidateChain> tailPaths = Create_Depth_N_paths(tailCandidatesList, depth);
                // (recursive call)

                for (int i = 0; i < nHeadCandidates.Count; i++) // for each member of nHeadCandidates
                {
                    Candidate nHeadCandidate = (Candidate)nHeadCandidates[i];

                    for (int j = 0; j < tailPaths.Count; j++)
                    {
                        CandidateChain tailPath = tailPaths[j];
                        CandidateChain path = ConsChain(nHeadCandidate, tailPath);

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
                List<Candidate> candidates = childCandidatesList[0];
                for (int i = 0; i < candidates.Count && i <= depth; i++)
                {
                    Candidate candidate = candidates[i];
                    CandidateChain path = new CandidateChain(Enumerable.Repeat(candidate, 1));
                    paths.Add(path);
                }
            }

            return paths;
        }


        public static CandidateChain Get_Nth_Candidate(List<Candidate> headCandidates, int depth)
        {
            return new CandidateChain(
                headCandidates.Cast<Candidate>().Take(depth + 1));
        }


        // prepends head to a copy of tail to obtain result
        public static CandidateChain ConsChain(Candidate head, CandidateChain tail)
        {
            return new CandidateChain(
                tail.Cast<Candidate>().Prepend(head));
        }


        public static List<CandidateChain> FilterPaths(List<CandidateChain> paths)
        {
            List<CandidateChain> filteredPaths = new List<CandidateChain>();

            foreach (CandidateChain path in paths)
            {
                if (IsValidPath(path))
                {
                    filteredPaths.Add(path);
                }
            }

            return filteredPaths;
        }


        public static bool IsValidPath(CandidateChain path)
        {
            string wordsInPath = AutoAlignUtility.GetWordsInPath(path);
            string[] words = wordsInPath.Split(" ".ToCharArray());
            List<string> usedWords = new List<string>();
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


        public static double ComputeJointProb(CandidateChain path)
        {
            double jointProb = 0.0;

            // Look at this again.
            // It assumes that the chain is all Candidates.
            foreach (Candidate c in path)
            {
                jointProb += c.Prob;
            }

            return jointProb;
        }


        public static List<Candidate> GetTopPaths2(List<CandidateChain> paths, Dictionary<CandidateChain, double> probs)
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


        public static Dictionary<CandidateChain, double>
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
                    string linkedWords = AutoAlignUtility.GetWords(c);
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


        public static int ComputeDistance(CandidateChain path)
        {
            List<TargetWord> wordsInPath = AutoAlignUtility.GetTargetWordsInPath(path);

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


        public static int GetInitialPosition(List<TargetWord> wordsInPath)
        {
            int initialPosition = 0;

            foreach (TargetWord tWord in wordsInPath)
            {
                if (tWord.Position >= 0)
                {
                    initialPosition = tWord.Position;
                    break;
                }
            }

            return initialPosition;
        }


        static double ComputeOrderProb(CandidateChain path)
        {
            List<TargetWord> wordsInPath =
                AutoAlignUtility.GetTargetWordsInPath(path);

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
    }
}
