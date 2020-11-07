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
    }
}
