using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Miscellaneous;


    public class MappedGroup
    {
        public List<SourcePoint> SourcePoints = new List<SourcePoint>();
        public List<OpenTargetBond> TargetNodes = new List<OpenTargetBond>();
    }


    public class SourceWord
    {
        public string ID { get; set; }
        public string AltID { get; set; }
        public string Text { get; set; }
        public string Lemma { get; set; }
        public string Strong { get; set; }
    }


    public class WordInfo
    {
        public string Lang;
        public string Strong;
        public string Surface;
        public string Lemma;
        public string Cat;
        public string Morph;
    }


    public class Groups
    {
        /// <summary>
        /// Work in progress.
        /// This code was removed from another function and put here
        /// for safekeeping.  It is not currently being called by
        /// anything.
        /// This code is in an intermediate stage of being reworked
        /// from Clear2 into Clear3.
        /// This code has not been tested in its current form.
        /// </summary>
        /// 
        public static List<MappedGroup> GroupAlignmentWip(
            List<OpenMonoLink> links,
            GroupTranslationsTable groups,
            XElement treeNode,
            List<TargetPoint> targetPoints)
        {
            List<MappedGroup> links2 = Groups.WordsToGroups(links);

            GroupTranslationsTable_Old groups_old =
                new GroupTranslationsTable_Old();
            foreach (var kvp in groups.Dictionary)
                foreach (var x in kvp.Value)
                    groups_old.AddEntry(
                        kvp.Key.Text,
                        x.TargetGroupAsText.Text,
                        x.PrimaryPosition.Int);

            Dictionary<string, WordInfo> wordInfoTable =
                BuildWordInfoTable(treeNode);
            // sourceID => WordInfo

            List<XElement> terminals =
                AutoAlignUtility.GetTerminalXmlNodes(treeNode);

            List<SourceWord> sourceWordList =
                MakeSourceWordList(
                    terminals
                    .Select(node => node.SourceID().AsCanonicalString)
                    .OrderBy(sourceID => sourceID),
                    wordInfoTable);

            List<MaybeTargetPoint> tWords =
                targetPoints
                .Select(tp => new MaybeTargetPoint(tp))
                .ToList();

            Groups.AlignGroups(links2, sourceWordList, tWords, groups_old, terminals);

            FixCrossingLinks(ref links2);

            return links2;
        }



        public static void AlignGroups(
            List<MappedGroup> links,
            List<SourceWord> sourceWords,
            List<MaybeTargetPoint> targetWords,
            GroupTranslationsTable_Old groups,
            List<XElement> terminals
            )
        {
            SourceWord[] sWords = sourceWords.ToArray();
            MaybeTargetPoint[] tWords = targetWords.ToArray();

            List<string[][]> mappedGroups = GetGroupLinks(
                sWords, tWords, groups);

            if (mappedGroups.Count > 0)
            {
                links = RemoveOldLinks(mappedGroups, links);

                foreach (string[][] group in mappedGroups)
                {
                    AddGroup(group, links, terminals, targetWords);
                }
            }
        }


        /// <summary>
        /// Produces a list (x, ...) where x[0] is an array of source ID
        /// strings and x[1] is 
        /// </summary>
        ///
        static List<string[][]> GetGroupLinks(
            SourceWord[] sourceWords,
            MaybeTargetPoint[] targetWords,
            GroupTranslationsTable_Old groups)
        {
            List<string[][]> mappedGroups = new List<string[][]>();

            HashSet<MaybeTargetPoint> targetsAlreadyGrouped = new();

            // For each SourceWord sw:
            for (int i = 0; i < sourceWords.Length; i++)
            { 
                SourceWord sw = sourceWords[i];

                bool foundMatch = false;

                // if at least two positions away from the right edge:
                if (i < sourceWords.Length - 2)
                {
                    // consider sw and the two following source words:

                    string trigram = sourceWords[i].Lemma + " " + sourceWords[i + 1].Lemma + " " + sourceWords[i + 2].Lemma;
                    string trigramIDs = sourceWords[i].ID + " " + sourceWords[i + 1].ID + " " + sourceWords[i + 2].ID;
                    if (groups.ContainsSourceGroupKey(trigram))
                    {
                        GroupTranslations_Old targetGroups = groups.TranslationsForSourceGroup(trigram);
                        string match = FindTargetMatch(targetWords, targetGroups, 1, targetsAlreadyGrouped);
                        if (match != string.Empty)
                        {
                            string[] sourceLinks = trigramIDs.Split(" ".ToCharArray());
                            string[] targetLinks = match.Split(" ".ToCharArray());
                            string[][] group = new string[][] { sourceLinks, targetLinks };
                            mappedGroups.Add(group);
                            i += 2;
                            foundMatch = true;
                        }
                    }
                }

                // if at least one position away from the right edge
                // and have not yet found a match:
                if (!foundMatch && i < sourceWords.Length - 1)
                {
                    string bigram = sourceWords[i].Lemma + " " + sourceWords[i + 1].Lemma;
                    string bigramIDs = sourceWords[i].ID + " " + sourceWords[i + 1].ID;
                    if (groups.ContainsSourceGroupKey(bigram))
                    {
                        GroupTranslations_Old targetGroups = groups.TranslationsForSourceGroup(bigram);
                        string match = FindTargetMatch(targetWords, targetGroups, 1, targetsAlreadyGrouped);
                        if (match != string.Empty)
                        {
                            string[] sourceLinks = bigramIDs.Split(" ".ToCharArray());
                            string[] targetLinks = match.Split(" ".ToCharArray());
                            string[][] group = new string[][] { sourceLinks, targetLinks };
                            mappedGroups.Add(group);
                            i += 1;
                            foundMatch = true;
                        }
                    }
                }

                // if not yet found a match:
                if (!foundMatch)
                {
                    string unigram = sourceWords[i].Lemma;
                    string unigramIDs = sourceWords[i].ID;
                    if (groups.ContainsSourceGroupKey(unigram))
                    {
                        GroupTranslations_Old targetGroups = groups.TranslationsForSourceGroup(unigram);
                        string match = FindTargetMatch(targetWords, targetGroups, 2, targetsAlreadyGrouped);
                        if (match != string.Empty)
                        {
                            string[] sourceLinks = unigramIDs.Split(" ".ToCharArray());
                            string[] targetLinks = match.Split(" ".ToCharArray());
                            string[][] group = new string[][] { sourceLinks, targetLinks };
                            mappedGroups.Add(group);
                            foundMatch = true;
                        }
                    }
                }
            }

            return mappedGroups;
        }

        /// <summary>
        /// targetGroups is essentially a list of (TargetGroupAsText, PrimaryPosition)
        /// returns a string with the list of targetIDs, or the empty string if no match found
        /// </summary>
        /// 
        static string FindTargetMatch(
            MaybeTargetPoint[] targetWords,
            GroupTranslations_Old targetGroups,
            int minLength,
            HashSet<MaybeTargetPoint> targetsAlreadyGrouped)
        {
            string match = string.Empty;

            bool isSplit = false; // this means the target group has a ~ in it

            int maxRange; // is the number of target words in the target group
                          // or twice as much if isSplit

            foreach (GroupTranslation_Old targetGroup in targetGroups.AllTranslations)
            {
                if (targetGroup.TargetGroupAsText.Contains("~"))
                {
                    isSplit = true;
                    targetGroup.TargetGroupAsText = targetGroup.TargetGroupAsText.Replace(" ~ ", " ");
                }

                string[] words = targetGroup.TargetGroupAsText.Split(" ".ToCharArray());
                if (isSplit) maxRange = words.Length * 2;
                else maxRange = words.Length;

                int wordIndex = 0;
                int currRange = 0;
                bool inRange = false;
                List<int> targetWordsInGroup = new List<int>();

                for (int i = 0; i < targetWords.Length && wordIndex < words.Length; i++)
                {
                    MaybeTargetPoint targetWord = targetWords[i];
                    if (targetsAlreadyGrouped.Contains(targetWord)) continue;

                    // words are the target group words that I am looking for
                    string word = words[wordIndex].Trim();

                    if (targetWord.Lower == word)
                    {
                        match += targetWord.ID + " ";
                        wordIndex++;
                        inRange = true;
                        targetWordsInGroup.Add(i);
                    }
                    else
                    {
                        match = string.Empty;
                        targetWordsInGroup.Clear();
                        wordIndex = 0;
                        inRange = false;
                        currRange = 0;
                    }
                    if (inRange) currRange++;
                    if (currRange == maxRange) break;
                }
                if (words.Length == targetWordsInGroup.Count)
                {
                    foreach (int n in targetWordsInGroup)
                        targetsAlreadyGrouped.Add(targetWords[n]);
                    break;
                }
                else
                {
                    match = string.Empty;
                }
            }

            return match.Trim();
        }



        static List<MappedGroup> RemoveOldLinks(
            List<string[][]> mappedGroups,
            List<MappedGroup> links)
        {
            List<MappedGroup> cleanLinks = new List<MappedGroup>();

            List<string> sourceWordsInGroups = GetSourceWordsInGroups(mappedGroups);
            List<string> targetWordsInGroups = GetTargetWordsInGroups(mappedGroups);

            foreach (MappedGroup mg in links)
            {
                if (!InGroup(mg, sourceWordsInGroups, targetWordsInGroups))
                {
                    cleanLinks.Add(mg);
                }
            }

            return cleanLinks;
        }


        static List<string> GetSourceWordsInGroups(List<string[][]> mappedGroups)
        {
            return
                mappedGroups
                .Select(mappedGroup => mappedGroup[0])
                .SelectMany(sourceWord => sourceWord)
                .ToList();
        }


        static List<string> GetTargetWordsInGroups(List<string[][]> mappedGroups)
        {
            return
                mappedGroups
                .Select(mappedGroup => mappedGroup[1])
                .SelectMany(sourceWord => sourceWord)
                .ToList();
        }

        static bool InGroup(MappedGroup mg, List<string> sourceWordsInGroups, List<string> targetWordsInGroups)
        {
            bool inGroup = false;

            foreach (SourcePoint sNode in mg.SourcePoints)
            {
                if (sourceWordsInGroups.Contains(
                    sNode.SourceID.AsCanonicalString))
                {
                    inGroup = true;
                }
            }

            if (inGroup == false)
            {
                foreach (OpenTargetBond tNode in mg.TargetNodes)
                {
                    if (targetWordsInGroups.Contains(tNode.MaybeTargetPoint.ID))
                    {
                        inGroup = true;
                    }
                }
            }

            return inGroup;
        }

        static void AddGroup(string[][] group, List<MappedGroup> links, List<XElement> terminals, List<MaybeTargetPoint> targets)
        {
            string[] sourceWords = group[0];
            string[] targetWords = group[1];
            MappedGroup mg = new MappedGroup();
            AddSourceNodes(sourceWords, mg.SourcePoints, terminals);
            AddTargetNodes(targetWords, mg.TargetNodes, targets);
            links.Add(mg);
        }

        static void AddSourceNodes(string[] sourceWords, List<SourcePoint> sourceNodes, List<XElement> terminals)
        {
            for (int i = 0; i < sourceWords.Length; i++)
            {
                SourcePoint node = GetSourceNode(sourceWords[i], terminals);
                sourceNodes.Add(node);
            }
        }

        static SourcePoint GetSourceNode(string id, List<XElement> terminals)
        {           
            XElement treeNode = LocateTreeNode(id, terminals);
            int treePosition = treeNode.AttrAsInt("Start");

            SourcePoint sNode = new SourcePoint(
                Lemma: treeNode.Lemma(),
                Terminal: treeNode,
                SourceID: treeNode.SourceID(),
                AltID: "",
                TreePosition: treePosition,
                SourcePosition: 0,
                RelativeTreePosition: treePosition / (double)terminals.Count);

            // FIXME: altID and sourcePosition are dummy values, does it matter?

            return sNode;
        }

        static XElement LocateTreeNode(string id, List<XElement> terminals)
        {
            XElement treeNode = null;

            foreach (XElement terminal in terminals)
            {
                if (id.StartsWith(terminal.Attribute("morphId").Value))
                {
                    treeNode = terminal;
                    break;
                }
            }

            return treeNode;
        }

        static void AddTargetNodes(string[] targetWords, List<OpenTargetBond> targetNodes, List<MaybeTargetPoint> targets)
        {
            for (int i = 0; i < targetWords.Length; i++)
            {
                OpenTargetBond node = GetTargetNode(targetWords[i], targets);
                targetNodes.Add(node);
            }
        }

        static OpenTargetBond GetTargetNode(string id, List<MaybeTargetPoint> targets)
        {
            MaybeTargetPoint tWord = LocateTargetword(id, targets);

            return new OpenTargetBond(
                MaybeTargetPoint: tWord,
                Score: 1.0);
        }

        static MaybeTargetPoint LocateTargetword(string id, List<MaybeTargetPoint> targets)
        {
            MaybeTargetPoint tw = null;

            foreach (MaybeTargetPoint target in targets)
            {
                if (id == target.ID)
                {
                    tw = target;
                    break;
                }
            }

            return tw;
        }



        public static List<MappedGroup> WordsToGroups(List<OpenMonoLink> wordLinks)
        {
            List<MappedGroup> groupLinks = new List<MappedGroup>();

            foreach (OpenMonoLink wordLink in wordLinks)
            {
                MappedGroup groupLink = new MappedGroup();
                groupLink.SourcePoints.Add(wordLink.SourcePoint);
                groupLink.TargetNodes.Add(wordLink.OpenTargetBond);
                groupLinks.Add(groupLink);
            }

            return groupLinks;
        }


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
                g.SourcePoints[0].SourceID.AsCanonicalString;

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


        public static List<SourceWord> MakeSourceWordList(
             IEnumerable<string> sourceSegmentIds,
             Dictionary<string, WordInfo> wordInfoTable)
        {
            return sourceSegmentIds
                .Select((string id) => Tuple.Create(id, wordInfoTable[id]))
                .WithVersionNumber(
                    (Tuple<string, WordInfo> x) => x.Item2.Surface)
                .Select((Tuple<Tuple<string, WordInfo>, int> y) =>
                {
                    WordInfo wi = y.Item1.Item2;
                    return new SourceWord()
                    {
                        ID = y.Item1.Item1,
                        Text = wi.Surface,
                        Lemma = wi.Lemma,
                        Strong = wi.Lang + wi.Strong,
                        AltID = $"{wi.Surface}-{y.Item2}"
                    };
                })
                .ToList();
        }


        public static Dictionary<string, WordInfo> BuildWordInfoTable(
            XElement tree)
        {
            return
                AutoAlignUtility.GetTerminalXmlNodes(tree)
                .ToDictionary(
                    node => GetSourceIdFromTerminalXmlNode(node),
                    node => GetWordInfoFromTerminalXmlNode(node));
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
    }
}
