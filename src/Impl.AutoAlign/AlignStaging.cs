using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;



using Alignment2 = GBI_Aligner.Alignment2;
using Line = GBI_Aligner.Line;
using WordInfo = GBI_Aligner.WordInfo;
using SourceWord = GBI_Aligner.SourceWord;
using TargetWord = GBI_Aligner.TargetWord;
using Candidate = GBI_Aligner.Candidate;
using MappedWords = GBI_Aligner.MappedWords;
using MappedGroup = GBI_Aligner.MappedGroup;
using LinkedWord = GBI_Aligner.LinkedWord;
using AlternativesForTerminals = GBI_Aligner.AlternativesForTerminals;
using Manuscript = GBI_Aligner.Manuscript;
using Translation = GBI_Aligner.Translation;
using TranslationWord = GBI_Aligner.TranslationWord;
using Link = GBI_Aligner.Link;
using SourceNode = GBI_Aligner.SourceNode;
using CandidateChain = GBI_Aligner.CandidateChain;

using CrossingLinks = GBI_Aligner.CrossingLinks;





namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Miscellaneous;

    public class AlignStaging
    {
        public static void FixCrossingLinks(ref List<MappedGroup> links)
        {
            Dictionary<string, List<MappedGroup>> uniqueLemmaLinks =
                GetUniqueLemmaLinks(links);
            List<CrossingLinks> crossingLinks = IdentifyCrossingLinks(uniqueLemmaLinks);
            SwapTargets(crossingLinks, links);
        }


        // lemma => list of MappedGroup
        // where the MappedGroup has just one source and target node
        public static Dictionary<string, List<MappedGroup>> GetUniqueLemmaLinks(List<MappedGroup> links)
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


        public static List<CrossingLinks> IdentifyCrossingLinks(Dictionary<string, List<MappedGroup>> uniqueLemmaLinks)
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


        public static bool Crossing(List<MappedGroup> links)
        {
            MappedGroup link1 = links[0];
            MappedGroup link2 = links[1];
            SourceNode sWord1 = link1.SourceNodes[0];
            LinkedWord tWord1 = link1.TargetNodes[0];
            SourceNode sWord2 = link2.SourceNodes[0];
            LinkedWord tWord2 = link2.TargetNodes[0];
            if (tWord1.Word.Position < 0 || tWord2.Word.Position < 0) return false;
            if ((sWord1.Position < sWord2.Position && tWord1.Word.Position > tWord2.Word.Position)
               || (sWord1.Position > sWord2.Position && tWord1.Word.Position < tWord2.Word.Position)
               )
            {
                return true;
            }

            return false;
        }


        public static void SwapTargets(List<CrossingLinks> crossingLinks, List<MappedGroup> links)
        {
            for (int i = 0; i < crossingLinks.Count; i++)
            {
                CrossingLinks cl = crossingLinks[i];
                SourceNode sNode1 = cl.Link1.SourceNodes[0];
                SourceNode sNode2 = cl.Link2.SourceNodes[0];
                List<LinkedWord> TargetNodes0 = cl.Link1.TargetNodes;
                cl.Link1.TargetNodes = cl.Link2.TargetNodes;
                cl.Link2.TargetNodes = TargetNodes0;
                for (int j = 0; j < links.Count; j++)
                {
                    MappedGroup mp = links[j];
                    SourceNode sNode = (SourceNode)mp.SourceNodes[0];
                    if (sNode.MorphID == sNode1.MorphID) mp.TargetNodes = cl.Link1.TargetNodes;
                    if (sNode.MorphID == sNode2.MorphID) mp.TargetNodes = cl.Link2.TargetNodes;
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
                            Word = AutoAlignUtility.CreateFakeTargetWord()
                        }
                    };
                }
            }
        }


        public static List<MappedWords> FindWinners(List<MappedWords> conflict, int pass)
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


        public static List<string> GetLinkedTargets(List<MappedWords> links)
        {
            return links
                .Where(mw => !mw.TargetNode.Word.IsFake)
                .Select(mw => mw.TargetNode.Word.ID)
                .ToList();
        }


        public static Dictionary<string, MappedWords> CreateLinksTable(List<MappedWords> links)
        {
            return links
                .Where(mw => !mw.TargetNode.Word.IsFake)
                .ToDictionary(mw => mw.SourceNode.MorphID, mw => mw);
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


        public static List<TargetWord> GetTargetCandidates(MappedWords postNeighbor, List<TargetWord> targetWords, List<string> linkedTargets, List<string> puncs, List<string> targetFuncWords, bool contentWordsOnly)
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


        public static List<TargetWord> GetTargetCandidates(MappedWords preNeighbor, MappedWords postNeighbor, List<TargetWord> targetWords, List<string> linkedTargets, List<string> puncBounds, List<string> targetFuncWords, bool contentWordsOnly)
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
    }
}
