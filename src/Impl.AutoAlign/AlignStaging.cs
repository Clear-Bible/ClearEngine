using System;
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
                g.SourcePoints[0].MorphID;

            bool linkIsOneToOne(MappedGroup link) =>
                link.SourcePoints.Count == 1 && link.TargetNodes.Count == 1;

            string lemmaOfSoleSourceWord(MappedGroup link) =>
                link.SourcePoints[0].Lemma;
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
                g.SourcePoints[0].TreePosition;

            int positionOfSoleWordInTargetGroup(MappedGroup g) =>
                g.TargetNodes[0].MaybeTargetPoint.Position;
        }
           

        public static List<List<OpenMonoLink>> FindConflictingLinks(
            List<OpenMonoLink> links)
        {
            return links
                .Where(targetWordNotEmpty)
                .GroupBy(targetTextAndId)
                .Where(group => group.Count() > 1)
                .Select(group => group.ToList())
                .ToList();

            bool targetWordNotEmpty(OpenMonoLink link) =>
                link.OpenTargetBond.MaybeTargetPoint.Lower != string.Empty;

            Tuple<string, string> targetTextAndId(OpenMonoLink link) =>
                Tuple.Create(
                    link.OpenTargetBond.MaybeTargetPoint.Lower,
                    link.OpenTargetBond.MaybeTargetPoint.ID);
        }


        public static void ResolveConflicts(
            List<List<OpenMonoLink>> conflicts,
            List<OpenMonoLink> links,
            int pass)
        {
            List<OpenMonoLink> linksToRemove =
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
                links[i] = makeFakeLink(links[i].SourcePoint);

            OpenMonoLink makeFakeLink(SourcePoint sourceNode) =>
                new OpenMonoLink(
                    sourcePoint: sourceNode,
                    openTargetBond: new OpenTargetBond(
                        maybeTargetPoint: AutoAlignUtility.CreateFakeTargetWord(),
                        score: -1000));
        }


        public static List<OpenMonoLink> FindWinners(
            List<OpenMonoLink> conflict,
            int pass)
        {
            // The winners are the links of maximal probability.
            // (we know that conflict is not the empty list)
            //
            double bestProb = conflict.Max(mw => prob(mw));
            List<OpenMonoLink> winners = conflict
                .Where(mw => mw.OpenTargetBond.Score == bestProb)
                .ToList();

            // On the second pass, if there are multiple winners,
            // then select the winner where the source and target
            // relative positions are closest in a relative sense.
            //
            if (pass == 2 && winners.Count > 1)
            {
                double minDelta = conflict.Min(mw => relativeDelta(mw));

                OpenMonoLink winner2 = winners
                    .Where(mw => relativeDelta(mw) == minDelta)
                    .FirstOrDefault();

                if (winner2 != null)
                {
                    winners = new List<OpenMonoLink>() { winner2 };
                }
            }

            return winners;

            double prob(OpenMonoLink mw) => mw.OpenTargetBond.Score;

            double relativeDelta(OpenMonoLink mw) =>
                Math.Abs(mw.SourcePoint.RelativeTreePosition -
                         mw.OpenTargetBond.MaybeTargetPoint.RelativePos);         
        }



        public static string GetTargetWordTextFromID(string targetID, List<MaybeTargetPoint> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Lower)
                .DefaultIfEmpty("")
                .First();
        }


        public static int GetTargetPositionFromID(string targetID, List<MaybeTargetPoint> targetWords)
        {
            return targetWords
                .Where(tw => targetID == tw.ID)
                .Select(tw => tw.Position)
                .DefaultIfEmpty(0)
                .First();
        }


        public static List<MaybeTargetPoint> GetTargetCandidates(
            OpenMonoLink anchorLink,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            Assumptions assumptions)
        {
            int anchor = anchorLink.OpenTargetBond.MaybeTargetPoint.Position;

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

            IEnumerable<MaybeTargetPoint> getWords(IEnumerable<int> positions) =>
                PositionsToTargetCandidates(
                    positions,
                    targetPoints,
                    linkedTargets,
                    assumptions);
        }


        public static List<MaybeTargetPoint> GetTargetCandidates(
            OpenMonoLink leftAnchor,
            OpenMonoLink rightAnchor,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            Assumptions assumptions)
        {
            IEnumerable<int> span()
            {
                for (int i = leftAnchor.OpenTargetBond.MaybeTargetPoint.Position;
                    i < rightAnchor.OpenTargetBond.MaybeTargetPoint.Position;
                    i++)
                {
                    yield return i;
                }
            }

            return PositionsToTargetCandidates(
                span(),
                targetPoints,
                linkedTargets,
                assumptions).ToList();
        }


        public static IEnumerable<MaybeTargetPoint> PositionsToTargetCandidates(
            IEnumerable<int> positions,
            List<TargetPoint> targetPoints,
            List<string> linkedTargets,
            Assumptions assumptions)
        {
            var ansr =
                positions
                .Where(n => n >= 0 && n < targetPoints.Count)
                .Select(n => targetPoints[n])
                .Select(targetPoint => new
                {
                    targetPoint.Lower,
                    targetPoint
                })
                .Where(x =>
                    !assumptions.ContentWordsOnly || isContentWord(x.Lower))
                .Where(x => isNotLinkedAlready(x.Lower))
                .TakeWhile(x => isNotPunctuation(x.Lower))
                .Select(x => new MaybeTargetPoint(x.targetPoint))
                .ToList();

            return ansr;

            bool isContentWord(string text) =>
                !assumptions.IsTargetFunctionWord(text);

            bool isNotLinkedAlready(string text) =>
                !linkedTargets.Contains(text);

            bool isNotPunctuation(string text) =>
                !assumptions.IsPunctuation(text);
        }


        public static List<CandidateChain> CreatePaths(List<List<Candidate>> childCandidatesList, int maxPaths)
        {
            // FIXME: what about overflow?
            // Maybe the condition (maxArcs <= 0) below is meant for overflow?
            //
            int maxArcs =
                childCandidatesList
                .Select(candidates => candidates.Count)
                .Aggregate(1, (product, n) => product * n);

            int maxDepth = // GetMaxDepth(childCandidatesList); // maximum sub-list length
                childCandidatesList
                .Select(candidates => candidates.Count)
                .DefaultIfEmpty(0)
                .Max();

            if (maxArcs > maxPaths || maxArcs <= 0)
            {
                double root = Math.Pow((double)maxPaths, 1.0 / childCandidatesList.Count);
                maxDepth = (int)root;
            }

            List<CandidateChain> depth_N_paths = new List<CandidateChain>();

            try
            {
                depth_N_paths = CreatePathsWithDepthLimit(childCandidatesList, maxDepth);
            }
            catch
            {
                depth_N_paths = CreatePaths(childCandidatesList, maxPaths / 2);
            }

            return depth_N_paths;
        }


        public static List<CandidateChain> CreatePathsWithDepthLimit(
            List<List<Candidate>> childCandidatesList,
            int depth)
        {
            if (childCandidatesList.Count > 1)
            {
                IEnumerable<Candidate> headCandidates =
                    childCandidatesList[0].Take(depth + 1);

                // (recursive call)
                List<CandidateChain> tailPaths =
                    CreatePathsWithDepthLimit(
                        getTail(childCandidatesList),
                        depth);

                return
                    headCandidates
                    .SelectMany((Candidate nHeadCandidate) =>
                        tailPaths
                        .Select((CandidateChain tailPath) =>
                            ConsChain(nHeadCandidate, tailPath)))
                    .Take(16000000)
                    .ToList();
            }
            else
            {
                return
                    childCandidatesList[0]
                    .Take(depth + 1)
                    .Select(makeSingletonChain)
                    .ToList();
            }

            List<List<Candidate>> getTail(List<List<Candidate>> x) =>
                x.Skip(1).ToList();

            CandidateChain makeSingletonChain(Candidate candidate) =>
                new CandidateChain(Enumerable.Repeat(candidate, 1));
        }


        // prepends head to a copy of tail to obtain result
        public static CandidateChain ConsChain(Candidate head, CandidateChain tail)
        {
            return new CandidateChain(
                tail.Cast<Candidate>().Prepend(head));
        }


        public static bool HasNoDuplicateWords(CandidateChain path)
        {
            bool pathHasDuplicateWords =
                AutoAlignUtility.GetTargetWordsInPath(path)
                .Where(word => !word.IsNothing)
                .GroupBy(word => new { word.Lower, word.Position })
                .Any(hasAtLeastTwoMembers);

            return !pathHasDuplicateWords;

            bool hasAtLeastTwoMembers(IEnumerable<MaybeTargetPoint> words) =>
                words.Skip(1).Any();
        }


        public static List<Candidate> GetLeadingCandidates(
            List<CandidateChain> paths,
            Dictionary<CandidateChain, double> probs)
        {
            double leadingProb =
                paths.Select(path => probs[path]).FirstOrDefault();

            return
                paths
                .Select(path => new Candidate(path, probs[path]))
                .TakeWhile(cand => cand.Prob == leadingProb)
                .ToList();
        }


        public static Dictionary<CandidateChain, double>
            AdjustProbsByDistanceAndOrder(
                Dictionary<CandidateChain, double> pathProbs)
        {
            int minimalDistance =
                pathProbs.Keys
                .Select(ComputeDistance)
                .DefaultIfEmpty(10000)
                .Min();

            if (minimalDistance > 0)
            {
                double getDistanceProb(double distance) =>
                    Math.Log(minimalDistance / distance);

                return
                    pathProbs
                    .Select(kvp => new { Chain = kvp.Key, Prob = kvp.Value })
                    .ToDictionary(
                        c => c.Chain,
                        c => c.Prob + c.Prob +
                            getDistanceProb(ComputeDistance(c.Chain)) +
                            ComputeOrderProb(c.Chain) / 2.0);
            }
            else
            {
                return pathProbs;
            }
        }


        public static int ComputeDistance(CandidateChain path)
        {
            IEnumerable<Tuple<int, int>> motions = ComputeMotions(path);

            return motions.Sum(m => Math.Abs(m.Item1 - m.Item2));
        }


        public static double ComputeOrderProb(CandidateChain path)
        {
            IEnumerable<Tuple<int, int>> motions = ComputeMotions(path);

            double countedWords = 1 + motions.Count();
            double violations = motions.Count(m => m.Item2 < m.Item1);

            return Math.Log(1.0 - violations / countedWords);
        }


        public static IEnumerable<Tuple<int, int>> ComputeMotions(
            CandidateChain path)
        {
            IEnumerable<int> positions =
                AutoAlignUtility.GetTargetWordsInPath(path)
                .Where(tw => !tw.IsNothing)
                .Select(tw => tw.Position);

            return
                positions
                .Zip(positions.Skip(1), Tuple.Create)
                .Where(m => m.Item1 != m.Item2);
        }
    }
}
