using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.Miscellaneous;

using System.Xml.Linq;
using System.Diagnostics;

namespace ClearBible.Clear3.Impl.ImportExportService
{
    /// <summary>
    /// (Implementation of IImportExportService.)
    /// </summary>
    ///
    public class ImportExportService : IImportExportService
    {
        public TargetVerseCorpus ImportTargetVerseCorpusFromLegacy(
            string path,
            ISegmenter segmenter,
            List<string> puncs,
            string lang)
        {
            List<TargetVerse> targetsList2 = new();

            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    if ((line.Trim().Length < 9) || (line.Substring(0, 2) == "//")) continue;

                    string canonicalVerseIDString = line.Substring(0, line.IndexOf(" "));

                    VerseID verseID = new VerseID(canonicalVerseIDString);

                    string verseText = line.Substring(line.IndexOf(" ") + 1);

                    string[] segments = segmenter.GetSegments(verseText, puncs, lang);

                    TargetVerse targets = new TargetVerse(
                        segments
                        .Select((segment, j) =>
                            new Target(
                                new TargetText(segment),
                                new TargetID(verseID, j + 1)))
                        .ToList());

                    targetsList2.Add(targets);
                }
            }

            return new TargetVerseCorpus(targetsList2);
        }


        public List<ZoneAlignmentProblem> ImportZoneAlignmentProblemsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath)
        {
            string[] sourceLines = File.ReadAllLines(parallelSourcePath);
            string[] targetLines = File.ReadAllLines(parallelTargetPath);

            if (sourceLines.Length != targetLines.Length)
            {
                throw new InvalidDataException(
                    "Parallel files must have same number of lines.");
            }

            return
                sourceLines.Zip(targetLines, (sourceLine, targetLine) =>
                {
                    IEnumerable<string>
                        sourceStrings = fields(sourceLine),
                        targetStrings = fields(targetLine);

                    return new ZoneAlignmentProblem(
                        TargetZone:
                            new TargetZone(
                                targetStrings
                                .Select(s => new Target(
                                    getTargetMorph(s),
                                    getTargetId(s)))
                                .ToList()),
                        SourceZone:
                            new SourceZone(
                                sourceStrings
                                .Select(s => new Source(
                                    getSourceMorph(s),
                                    getSourceLemma(s),
                                    getSourceId(s)))
                                .ToList()));
                        // FirstSourceVerseID:
                        //     getSourceVerseID(sourceStrings.First()),
                        // LastSourceVerseID:
                        //     getSourceVerseID(sourceStrings.Last()));
                })
                .ToList();


            // Local functions:

            IEnumerable<string> fields(string line) =>
                line.Split(' ').Where(s => !String.IsNullOrWhiteSpace(s));

            TargetText getTargetMorph(string s) =>
                new TargetText(getBeforeLastUnderscore(s));

            TargetID getTargetId(string s) =>
                new TargetID(getAfterLastUnderscore(s));

            SourceText getSourceMorph(string s) =>
               new SourceText(getBeforeLastUnderscore(s));

            Lemma getSourceLemma(string s) =>
                new Lemma(getBeforeLastUnderscore(s));

            SourceID getSourceId(string s) =>
                new SourceID(getAfterLastUnderscore(s));

            // VerseID getSourceVerseID(string s) =>
            //     (new SourceID(getAfterLastUnderscore(s))).VerseID;

            string getBeforeLastUnderscore(string s) =>
                s.Substring(0, s.LastIndexOf("_"));

            string getAfterLastUnderscore(string s) =>
                s.Substring(s.LastIndexOf("_") + 1);
        }


        public TranslationModel ImportTranslationModel(
            string filePath)
        {
            return new TranslationModel(
                File.ReadLines(filePath)
                .Select(line => line.Split('\t').ToList())
                .Where(fields => fields.Count == 3)
                .Select(fields => new
                {
                    lemma = new Lemma(fields[0].Trim()),
                    targetMorph = new TargetText(fields[1].Trim()),
                    score = new Score(Double.Parse(fields[2].Trim()))
                })
                .GroupBy(row => row.lemma)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        row => row.targetMorph,
                        row => row.score)));
        }


        public AlignmentModel ImportAlignmentModel(
            string filePath)
        {
            Regex regex = new Regex(
                @"^\s*(\d+)\s*-\s*(\d+)\s+(\S+)\s*$",
                RegexOptions.Compiled);

            Dictionary<BareLink, Score>
                inner =
                File.ReadLines(filePath)
                .Select(interpretLine)
                .ToDictionary(item => item.Item1, item => item.Item2);

            return new AlignmentModel(inner);

            (BareLink, Score) interpretLine(
                string line, int index)
            {
                Match m = regex.Match(line);
                if (!m.Success)
                    error(index, "invalid input syntax");
                if (m.Groups[1].Length != 12)
                    error(index, "source ID must have 12 digits");
                if (m.Groups[2].Length != 11)
                    error(index, "target ID must have 11 digits");
                if (!double.TryParse(m.Groups[3].Value, out double score))
                    error(index, "third field must be a number");
                return (
                    new BareLink(
                        new SourceID(m.Groups[1].Value),
                        new TargetID(m.Groups[2].Value)),
                    new Score(score));
            }

            void error(int index, string msg)
            {
                throw new ClearException(
                    $"{filePath} line {index + 1}: {msg}",
                    StatusCode.InvalidInput);
            }
        }


        public GroupTranslationsTable ImportGroupTranslationsTable(
            string filePath)
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


        public List<string> GetWordList(string file)
        {
            List<string> wordList = new List<string>();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                wordList.Add(line.Trim());
            }

            return wordList;
        }


        public List<string> GetStopWords(string file)
        {
            List<string> wordList = new List<string>();

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    wordList.Add(line.Trim());
                }
            }

            return wordList;
        }


        public Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(string file)
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
        public Dictionary<string, int> GetXLinks(string file)
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


        public Dictionary<string, Gloss> BuildGlossTableFromFile(string glossFile)
        {
            Dictionary<string, Gloss> glossTable = new Dictionary<string, Gloss>();

            string[] lines = File.ReadAllLines(glossFile);
            foreach (string line in lines)
            {
                string[] groups = line.Split("\t".ToCharArray());

                if (groups.Length == 3)
                {
                    string morphID = groups[0].Trim();

                    Gloss g = new Gloss();
                    g.Gloss1 = groups[1].Trim();
                    g.Gloss2 = groups[2].Trim();

                    glossTable.Add(morphID, g);
                }
            }

            return glossTable;
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
        public Dictionary<string, Dictionary<string, string>> GetOldLinks(string jsonFile, GroupTranslationsTable groups)
        {
            Dictionary<string, Dictionary<string, string>> oldLinks =
                new Dictionary<string, Dictionary<string, string>>();

            string jsonText = File.ReadAllText(jsonFile);
            LpaLine[] lines = JsonConvert.DeserializeObject<LpaLine[]>(jsonText);
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


        public Dictionary<string, Dictionary<string, int>> BuildStrongTable(string strongFile)
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


        public SimpleVersification ImportSimpleVersificationFromLegacy(
            string path,
            string versificationType)
        {
            XElement legacyVersification = XElement.Load(path);

            List<VerseID>
                currentSourceVerses = new(),
                currentTargetVerses = new();

            List<SimpleZoneSpec> specs = new();

            void step()
            {
                if (currentSourceVerses.Any() ||
                    currentTargetVerses.Any())
                {
                    specs.Add(
                        new SimpleZoneSpec(
                            currentSourceVerses,
                            currentTargetVerses));

                    currentSourceVerses = new();
                    currentTargetVerses = new();
                }
            }

            foreach(XElement entry in
                legacyVersification.Elements()
                .First(e =>
                    e.Name.LocalName == "Type" &&
                    e.Attribute("Switch").Value == versificationType)
                .Elements())
            {
                int mb = entry.AttrAsInt("MB");
                int mc = entry.AttrAsInt("MC");
                int mv = entry.AttrAsInt("MV");
                int tb = entry.AttrAsInt("TB");
                int tc = entry.AttrAsInt("TC");
                int tv = entry.AttrAsInt("TV");

                VerseID
                    sourceVerse = new VerseID(mb, mc, mv),
                    targetVerse = new VerseID(tb, tc, tv);

                // 2021.01.05 CL: The original code had the bug below.
                // 2020.10.22 CL: Bug in the following code. It does not check to see if a verse is already in the list before adding it.
                // Pairing seems to be loose in that there is transitivity assumed in the pairing. 
                // So if X1-Y1, X1-Y2, and then:
                //   (a) X2-Y2 then it assumes X2-Y1 
                //   (b) X2-Y1 then it assume X2-Y2

                if (currentSourceVerses.Contains(sourceVerse))
                {
                    // 2021.01.05 CL: The original code had the bug below.
                    // 2020.10.22 CL: Bug. Didn't check to see if it already is in the list.
                    if (!currentTargetVerses.Contains(targetVerse))
                    {
                        currentTargetVerses.Add(targetVerse);
                    } 
                }
                else if (currentTargetVerses.Contains(targetVerse))
                {
                    currentSourceVerses.Add(sourceVerse);
                }
                else
                {
                    step();

                    currentSourceVerses.Add(sourceVerse);
                    currentTargetVerses.Add(targetVerse);
                }
            }

            step();

            return new SimpleVersification(specs);
        }
    }
}
