using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.Miscellaneous;

    public class AutoAlignUtility
    {
        public static Dictionary<string, WordInfo> BuildWordInfoTable(
            XElement tree)
        {           
            return
                GetTerminalXmlNodes(tree)
                .ToDictionary(
                    node => GetSourceIdFromTerminalXmlNode(node),
                    node => GetWordInfoFromTerminalXmlNode(node));
        }

        public static List<XElement> GetTerminalXmlNodes(XElement treeNode)
        {
            return treeNode
                .Descendants()
                .Where(e => e.FirstNode is XText)
                .ToList();
        }

        public static string GetSourceIdFromTerminalXmlNode(XElement node)
        {
            string sourceId = node.Attribute("morphId").Value;
            if (sourceId.Length == 11) sourceId += "1";
            return sourceId;
        }

        public static WordInfo GetWordInfoFromTerminalXmlNode(XElement node)
        {
            string language = node.Attribute("Language").Value;

            string type =
                node.AttrAsString(language == "G" ? "Type" : "NounType");

            string category = node.Attribute("Cat").Value;
            if (category == "noun" && type == "Proper")
                category = "Name";

            return new WordInfo()
            {
                Lang = language,
                Strong = node.Attribute("StrongNumberX").Value,
                Surface = node.Attribute("Unicode").Value,
                Lemma = node.Attribute("UnicodeLemma").Value,
                Cat = category,
                Morph = node.Attribute("Analysis").Value
            };               
        }


        


        public static List<MonoLink> GetLinkedSiblings(
            XElement treeNode,
            Dictionary<string, MonoLink> linksTable)
        {
            if (treeNode.Parent != null &&
                treeNode.Parent.Name.LocalName != "Tree")
            {
                List<MonoLink> linkedSiblings =
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
                return new List<MonoLink>();
            }          
        }


        public static MonoLink GetPreNeighbor(MonoLink unLinked, List<MonoLink> linkedSiblings)
        {
            int limit = unLinked.SourceNode.TreeNode.AttrAsInt("Start");

            int end(MonoLink mw) =>
                mw.SourceNode.TreeNode.AttrAsInt("End");

            return
                linkedSiblings
                .Select(mw => new { mw, distance = limit - end(mw) })
                .Where(x => x.distance > 0)
                .OrderBy(x => x.distance)
                .Select(x => x.mw)
                .FirstOrDefault();
        }


        public static MonoLink GetPostNeighbor(MonoLink unLinked, List<MonoLink> linkedSiblings)
        {
            int limit = unLinked.SourceNode.TreeNode.AttrAsInt("End");

            int end(MonoLink mw) =>
                mw.SourceNode.TreeNode.AttrAsInt("End");

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
                words += wordInPath.Text + "-" + wordInPath.Position + " ";
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
                words += wordInPath.Text + "-" + wordInPath.Position + " ";
            }

            return words.Trim();
        }



        public static MaybeTargetPoint CreateFakeTargetWord()
        {
            return new MaybeTargetPoint();
        }



        public static List<LinkedWord> GetLinkedWords(Candidate candidate)
        {
            List<LinkedWord> linkedWords = new List<LinkedWord>();
            GetLinkedWordsHelper(candidate.Chain, linkedWords, candidate.Prob);
            return linkedWords;
        }


        public static void GetLinkedWordsHelper(ArrayList path, List<LinkedWord> links, double prob)
        {
            if (path.Count == 0)
            {
                links.Add(new LinkedWord()
                {
                    Word = new MaybeTargetPoint(),
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
                        GetLinkedWordsHelper(c.Chain, links, c.Prob);
                    }
                }
                else
                {
                    foreach (MaybeTargetPoint tWord in path)
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
    }
}
