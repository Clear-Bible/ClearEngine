using System.Text;

using ClearBible.Engine.Persistence;
using ClearBible.Engine.SyntaxTree.Aligner.Translation;
using ClearBible.Engine.SyntaxTree.Aligner.Legacy;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClearBible.Engine.SyntaxTree.Aligner.Persistence
{
    public class FileGetSyntaxTreeWordAlignerHyperparams : IPersistGettable<FileGetSyntaxTreeWordAlignerHyperparams, SyntaxTreeWordAlignerHyperparameters>
    {
        private string? PathPrefix { get; set; }

        private string AddPathPrefix(string s) => Path.Combine(PathPrefix ?? "", s);

        public FileGetSyntaxTreeWordAlignerHyperparams()
        {
        }
        public override IPersistGettable<FileGetSyntaxTreeWordAlignerHyperparams, SyntaxTreeWordAlignerHyperparameters> SetLocation(string location)
        {
            PathPrefix = location;
            return this;
        }
        public override async Task<SyntaxTreeWordAlignerHyperparameters> GetAsync()
        {
            var puncsPath = AddPathPrefix("puncs.txt");
            var stopWordsPath = AddPathPrefix("stopWords.txt");
            var sourceFuncWordsPath = AddPathPrefix("sourceFuncWords.txt");
            var targetFuncWordsPath = AddPathPrefix("targetFuncWords.txt");
            var manTransModelPath = AddPathPrefix("manTransModel.tsv");
            var goodLinksPath = AddPathPrefix("goodLinks.tsv");
            var badLinksPath = AddPathPrefix("badLinks.tsv");
            //var glossTablePath = AddPathPrefix("Gloss.tsv");
            var groupsPath = AddPathPrefix("groups.tsv");
            var oldAlignmentPath = AddPathPrefix("oldAlignment.json");
            var strongsPath = AddPathPrefix("strongs.txt");

            var puncs = new List<string>();
            var stopWords = new List<string>();
            var sourceFunctionWords = new List<string>();
            var targetFunctionWords = new List<string>();
            var manTransModel = new TranslationModel(new Dictionary<SourceLemma, Dictionary<TargetLemma, Score>>());
            var groups = new GroupTranslationsTable(new Dictionary<SourceLemmasAsText, HashSet<TargetGroup>>());
            var goodLinks = new Dictionary<string, int>();
            var badLinks = new Dictionary<string, int>();
            //var glossTable = new Dictionary<string, Gloss>();
            var oldLinks = new Dictionary<string, Dictionary<string, string>>();
            var strongs = new Dictionary<string, Dictionary<string, int>>();

            if (File.Exists(puncsPath)) puncs = GetWordList(puncsPath);
            if (File.Exists(stopWordsPath)) stopWords = GetStopWords(stopWordsPath);
            if (File.Exists(sourceFuncWordsPath)) sourceFunctionWords = GetWordList(sourceFuncWordsPath);
            if (File.Exists(targetFuncWordsPath)) targetFunctionWords = GetWordList(targetFuncWordsPath);
            if (File.Exists(manTransModelPath))
            {
                var manTransModelOrig = GetTranslationModel2(manTransModelPath);

                manTransModel =
                new TranslationModel(
                    manTransModelOrig.ToDictionary(
                        kvp => new SourceLemma(kvp.Key),
                        kvp => kvp.Value.ToDictionary(
                            kvp2 => new TargetLemma(kvp2.Key),
                            kvp2 => new Score(kvp2.Value.Prob))));
            }

            if (File.Exists(goodLinksPath)) goodLinks = GetXLinks(goodLinksPath);
            if (File.Exists(badLinksPath)) badLinks = GetXLinks(badLinksPath);
            //if (File.Exists(glossTablePath)) glossTable = importExportService.BuildGlossTableFromFile(glossTablePath);
            if (File.Exists(groupsPath)) groups = ImportGroupTranslationsTable(groupsPath);
            if (File.Exists(oldAlignmentPath)) oldLinks = GetOldLinks(oldAlignmentPath, groups);
            if (File.Exists(strongsPath)) strongs = BuildStrongTable(strongsPath);



            return await Task.Run(() => new SyntaxTreeWordAlignerHyperparameters(
                strongs,
                //glossTable,
                oldLinks,
                goodLinks,
                badLinks,
                sourceFunctionWords,
                targetFunctionWords,
                stopWords,
                puncs,
                manTransModel//,
                //groups
            ));
        }

        private List<string> GetWordList(string file)
        {
            List<string> wordList = new List<string>();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                wordList.Add(line.Trim());
            }

            return wordList;
        }

        private List<string> GetStopWords(string file)
        {
            List<string> wordList = new List<string>();

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string? line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    wordList.Add(line.Trim());
                }
            }

            return wordList;
        }

        /// <summary>
        /// Obtain a good links or bad links dictionary from a legacy
        /// file format used in Clear2.
        /// </summary>
        /// <remarks>
        /// The intent is to collect information about the judgments 
        /// made when checking alignments manually or otherwise performing
        /// manual linking.
        /// </remarks>
        /// <returns>
        /// A Dictionary that maps a string of the form xxx#yyy (where xxx
        /// is a lemma and yyy is a lowercased target text) to a count.
        /// The meaning is that an association between the lemma
        /// and the lowercased target text was found to be good (or bad)
        /// for the count number of times.
        /// </returns>
        /// 
        private Dictionary<string, int> GetXLinks(string file)
        {
            Dictionary<string, int> xLinks = new Dictionary<string, int>();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split("\t".ToCharArray());
                if (groups.Length == 2)
                {
                    string badLink = groups[0].Trim();
                    int count = Int32.Parse(groups[1]);
                    xLinks.Add(badLink, count);
                }
            }

            return xLinks;
        }

        private Dictionary<string, Dictionary<string, int>> BuildStrongTable(string strongFile)
        {
            Dictionary<string, Dictionary<string, int>> strongTable =
                new Dictionary<string, Dictionary<string, int>>();

            string[] strongLines = File.ReadAllLines(strongFile);

            foreach (string strongLine in strongLines)
            {
                string[] items = strongLine.Split(" ".ToCharArray());

                string wordId = items[0].Trim();
                string strong = items[1].Trim();

                if (strongTable.ContainsKey(strong))
                {
                    Dictionary<String, int> wordIds = strongTable[strong];
                    wordIds.Add(wordId, 1);
                }
                else
                {
                    Dictionary<string, int> wordIds = new Dictionary<string, int>();
                    wordIds.Add(wordId, 1);
                    strongTable.Add(strong, wordIds);
                }
            }

            return strongTable;
        }

        private GroupTranslationsTable ImportGroupTranslationsTable(string filePath)
        {
            Dictionary<
                SourceLemmasAsText,
                HashSet<TargetGroup>>
                dictionary =
                    File.ReadLines(filePath)
                    .Select(line =>
                        line.Split('\t').Select(s => s.Trim()).ToList())
                    .Where(fields => fields.Count == 3)
                    .Select(fields => new
                    {
                        src = new SourceLemmasAsText(fields[0]),
                        targ = new TargetGroupAsText(fields[1].ToLower()),
                        pos = new PrimaryPosition(Int32.Parse(fields[2]))
                    })
                    .GroupBy(record => record.src)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(record =>
                                new TargetGroup(record.targ, record.pos))
                            .ToHashSet());

            return new GroupTranslationsTable(dictionary);
        }


        #region lpa entities
        private class LpaManuscriptWord
        {
            /// <summary>
            /// Source ID as a canonical string (which contains only decimal
            /// digits) and then converted to a long integer.
            /// </summary>
            /// 
            public long id;

            /// <summary>
            /// Alternate ID of the form, for example, "λόγος-2" to mean the
            /// second occurence of the surface text "λόγος" within this zone
            /// </summary>
            /// 
            public string altId;

            /// <summary>
            /// Surface text.
            /// </summary>
            /// 
            public string text;

            /// <summary>
            /// Strong number, with prefix such as "G" or "H" to indicate
            /// language, as obtained from the treebank.
            /// </summary>
            /// 
            public string strong;

            public string gloss;
            public string gloss2;

            public string lemma;

            /// <summary>
            /// Part of speech, as obtained from the treebank.
            /// </summary>
            /// 
            public string pos;

            /// <summary>
            /// Morphology, a string that encodes the linguistic morphological
            /// analysis of this word, as obtained from the treebank.
            /// </summary>
            /// 
            public string morph;
        }

        private class LpaManuscript
        {
            public LpaManuscriptWord[] words;
        }

        private class LpaLink
        {
            public int[] source;
            public int[] target;
            public double cscore;

            public LpaLink(int[] source, int[] target, double cscore)
            {
                this.source = source;
                this.target = target;
                this.cscore = cscore;
            }
        }
        private class LpaLine
        {
            public LpaManuscript manuscript;

            public LpaTranslation translation;

            //public int[][][] links;
            [JsonConverter(typeof(LpaLinkJsonConverter))]
            public List<LpaLink> links;
        }
        private class LpaTranslationWord
        {
            /// <summary>
            /// TargetID as a canonical string (which contains only digits) and
            /// then converted to a long integer.
            /// </summary>
            /// 
            public long id;

            /// <summary>
            /// Alternate ID of the form, for example, "word-2" to mean the
            /// second occurence of the surface text "word" within this zone.
            /// </summary>
            /// 

            public string altId;


            /// <summary>
            /// Text, not lowercased.
            /// </summary>
            /// 
            public string text;
        }
        private class LpaTranslation
        {
            public LpaTranslationWord[] words;
        }

        #endregion
        private class LpaLinkJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(LpaLink).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                var links = new List<LpaLink>();

                if (reader.TokenType == JsonToken.Null)
                {
                    return links;
                }
                else
                {
                    JArray array = JArray.Load(reader);
                    var linksobj = array.ToObject<IList<dynamic>>() ?? new List<dynamic>();
                    foreach (var linkobj in linksobj)
                    {
                        int[] source = JsonConvert.DeserializeObject<int[]>(linkobj[0].ToString());
                        int[] target = JsonConvert.DeserializeObject<int[]>(linkobj[1].ToString());
                        double? cscore = null;
                        if (linkobj.Count >= 3)
                        {
                            dynamic attr = JsonConvert.DeserializeObject<dynamic>(linkobj[2].ToString());
                            cscore = attr.cscore;
                        }
                        if (cscore == null)
                        {
                            cscore = 0.0;
                        }
                        links.Add(new LpaLink(source, target, (double)cscore));
                    }
                }

                return links;
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                var links = (value as List<LpaLink>) ?? new List<LpaLink>();
                var linksobj = new List<dynamic>();

                foreach (var link in links)
                {
                    var linkobj = new List<dynamic>();
                    linkobj.Add(link.source);
                    linkobj.Add(link.target);
                    linkobj.Add(new Dictionary<string, object>(){
                        {"cscore", link.cscore}
                    });

                    linksobj.Add(linkobj);
                }

                JToken t = JToken.FromObject(linksobj);
                t.WriteTo(writer);
            }
        }

        // Deserializes the JSON in the input file into a Line[] with
        // types as above.
        // Returns a datum of the form:
        //   Hashtable(verseId => Hashtable(manuscriptWord.AltId => translationWord.AltId))
        //   where the verseId is the first 8 characters of the manuscriptWord.Id
        //   and the entries come from the one-to-one links
        //
        // In addition, this routine calls UpdateGroups() whenever it encounters
        // a link that is not one-to-one.
        //
        private Dictionary<string, Dictionary<string, string>> GetOldLinks(string jsonFile, GroupTranslationsTable groups)
        {
            Dictionary<string, Dictionary<string, string>> oldLinks =
                new Dictionary<string, Dictionary<string, string>>();

            string jsonText = File.ReadAllText(jsonFile);
            LpaLine[]? lines = JsonConvert.DeserializeObject<LpaLine[]>(jsonText);
            if (lines == null) return oldLinks;

            for (int i = 0; i < lines.Length; i++)
            {
                LpaLine line = lines[i];

                for (int j = 0; j < line.links.Count; j++)
                {
                    LpaLink link = line.links[j];
                    int[] sourceLinks = link.source;
                    int[] targetLinks = link.target;

                    if (sourceLinks.Length > 1 || targetLinks.Length > 1)
                    {
                        UpdateGroups(groups, sourceLinks, targetLinks, line.manuscript, line.translation);
                    }
                    else
                    {
                        int sourceLink = sourceLinks[0];
                        int targetLink = targetLinks[0];
                        LpaManuscriptWord mWord = line.manuscript.words[sourceLink];
                        LpaTranslationWord tWord = line.translation.words[targetLink];

                        string verseID = mWord.id.ToString().PadLeft(12, '0').Substring(0, 8);

                        if (oldLinks.ContainsKey(verseID))
                        {
                            Dictionary<string, string> verseLinks = oldLinks[verseID];
                            verseLinks.Add(mWord.altId, tWord.altId);
                        }
                        else
                        {
                            Dictionary<string, string> verseLinks = new Dictionary<string, string>();
                            verseLinks.Add(mWord.altId, tWord.altId);
                            oldLinks.Add(verseID, verseLinks);
                        }
                    }
                }
            }

            return oldLinks;
        }

        private void UpdateGroups(
            GroupTranslationsTable groups, 
            int[] sourceLinks,
            int[] targetLinks,
            LpaManuscript manuscript,
            LpaTranslation translation)
        {
            SourceLemmasAsText source = new SourceLemmasAsText(
                String.Join(
                    " ",
                    sourceLinks.Select(link => manuscript.words[link].lemma))
                .Trim());

            int firstTargetLink = targetLinks[0];

            int[] sortedTargetLinks = targetLinks.OrderBy(x => x).ToArray();

            PrimaryPosition primaryPosition = new PrimaryPosition(
                sortedTargetLinks
                .Select((link, newIndex) => Tuple.Create(link, newIndex))
                .First(x => x.Item1 == firstTargetLink)
                .Item2);

            TargetGroupAsText targetGroupAsText = new TargetGroupAsText(
                sortedTargetLinks
                .Aggregate(
                    Tuple.Create(-1, string.Empty),
                    (state, targetLink) =>
                    {
                        int prevIndex = state.Item1;
                        string text = state.Item2;
                        string sep =
                          (prevIndex >= 0 && (targetLink - prevIndex) > 1)
                              ? " ~ "
                              : " ";
                        return Tuple.Create(
                            targetLink,
                            text + sep + translation.words[targetLink].text);
                    })
                .Item2
                .Trim()
                .ToLower());

            Dictionary<
                SourceLemmasAsText,
                HashSet<TargetGroup>>
                inner = groups.Dictionary;

            if (!inner.TryGetValue(source, out var targets))
            {
                targets = new HashSet<TargetGroup>();
                inner[source] = targets;
            }

            targets.Add(new TargetGroup(targetGroupAsText, primaryPosition));
        }

        private class Stats
        {
            public int Count;
            public double Prob;
        }
        private Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(string file)
        {
            Dictionary<string, Dictionary<string, Stats>> transModel =
                new Dictionary<string, Dictionary<string, Stats>>();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split("\t".ToCharArray());
                if (groups.Length == 4)
                {
                    string source = groups[0].Trim();
                    string target = groups[1].Trim();
                    string sCount = groups[2].Trim();
                    string sProb = groups[3].Trim();
                    Stats s = new Stats();
                    s.Count = Int32.Parse(sCount);
                    s.Prob = Double.Parse(sProb);

                    if (transModel.ContainsKey(source))
                    {
                        Dictionary<string, Stats> translations = transModel[source];
                        translations.Add(target, s);
                    }
                    else
                    {
                        Dictionary<string, Stats> translations = new Dictionary<string, Stats>();
                        translations.Add(target, s);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }
    }
}
