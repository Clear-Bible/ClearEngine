using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Miscellaneous;

    public class AutoAlignUtility
    {


        public static List<XElement> GetTerminalXmlNodes(XElement treeNode)
        {
            return treeNode
                .Descendants()
                .Where(e => e.FirstNode is XText)
                .ToList();
        }

        


        


        public static List<OpenMonoLink> GetLinkedSiblings(
            XElement treeNode,
            Dictionary<string, OpenMonoLink> linksTable)
        {
            if (treeNode.Parent != null &&
                treeNode.Parent.Name.LocalName != "Tree")
            {
                List<OpenMonoLink> linkedSiblings =
                    treeNode.Parent.Elements()
                    .Where(child => child != treeNode)
                    .SelectMany(child => GetTerminalXmlNodes(child))
                    .Select(term => term.SourceId())
                    .Select(sourceId => linksTable.GetValueOrDefault(sourceId))
                    .Where(x => !(x is null))
                    .ToList();

                if (linkedSiblings.Count == 0)
                {
                    return GetLinkedSiblings(treeNode.Parent, linksTable);
                }
                else
                {
                    return linkedSiblings;
                }
            }
            else
            {
                return new List<OpenMonoLink>();
            }          
        }


        public static OpenMonoLink GetPreNeighbor(SourcePoint sourceNode, List<OpenMonoLink> linkedSiblings)
        {
            int limit = sourceNode.Terminal.AttrAsInt("Start");

            int end(OpenMonoLink mw) =>
                mw.SourcePoint.Terminal.AttrAsInt("End");

            return
                linkedSiblings
                .Select(mw => new { mw, distance = limit - end(mw) })
                .Where(x => x.distance > 0)
                .OrderBy(x => x.distance)
                .Select(x => x.mw)
                .FirstOrDefault();
        }


        public static OpenMonoLink GetPostNeighbor(SourcePoint sourceNode, List<OpenMonoLink> linkedSiblings)
        {
            int limit = sourceNode.Terminal.AttrAsInt("End");

            int end(OpenMonoLink mw) =>
                mw.SourcePoint.Terminal.AttrAsInt("End");

            return
                linkedSiblings
                .Where(mw => end(mw) > limit)
                .FirstOrDefault();
        }


        public static List<Candidate> CreateEmptyCandidate()
        {
            return new List<Candidate>()
            {
                new Candidate()
            };
        }


        // returns "text1-posn1 text2-posn2 ..."
        //
        public static string GetWords(Candidate c)
        {
            List<MaybeTargetPoint> wordsInPath = GetTargetWordsInPath(c.Chain);

            string words = string.Empty;

            foreach (MaybeTargetPoint wordInPath in wordsInPath)
            {
                words += wordInPath.Lower + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }


        public static List<MaybeTargetPoint> GetTargetWordsInPath(CandidateChain path)
        {
            IEnumerable<MaybeTargetPoint> helper(ArrayList path)
            {
                if (path.Count == 0)
                {
                    return new MaybeTargetPoint[] { CreateFakeTargetWord() };
                }
                else if (path[0] is Candidate)
                {
                    return path
                        .Cast<Candidate>()
                        .SelectMany(c => helper(c.Chain));
                }
                else
                {
                    return path.Cast<MaybeTargetPoint>();
                }
            }

            return helper(path).ToList();
        }


        public static string GetWordsInPath(CandidateChain path)
        {
            List<MaybeTargetPoint> wordsInPath = GetTargetWordsInPath(path);

            string words = string.Empty;

            foreach (MaybeTargetPoint wordInPath in wordsInPath)
            {
                words += wordInPath.Lower + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }



        public static MaybeTargetPoint CreateFakeTargetWord()
        {
            return new MaybeTargetPoint(TargetPoint: null);
        }



        public static List<OpenTargetBond> GetOpenTargetBonds(Candidate candidate)
        {
            List<OpenTargetBond> linkedWords = new List<OpenTargetBond>();
            GetLinkedWordsHelper(candidate.Chain, linkedWords, candidate.Prob);
            return linkedWords;
        }


        public static void GetLinkedWordsHelper(ArrayList path, List<OpenTargetBond> links, double prob)
        {
            if (path.Count == 0)
            {
                links.Add(new OpenTargetBond(
                    MaybeTargetPoint: new MaybeTargetPoint(TargetPoint: null),
                    Score: -1000));
            }
            else
            {
                if (path[0] is Candidate)
                {
                    foreach (Candidate c in path)
                    {
                        GetLinkedWordsHelper(c.Chain, links, c.Prob);
                    }
                }
                else
                {
                    foreach (MaybeTargetPoint tWord in path)
                    {
                        links.Add(new OpenTargetBond(
                            MaybeTargetPoint: tWord,
                            Score: prob));
                    }
                }
            }
        }
    }
}
