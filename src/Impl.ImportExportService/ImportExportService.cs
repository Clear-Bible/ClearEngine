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
    // FIXME: Improve documentation.
    //
    public class ImportExportService : IImportExportService
    {
        /// <summary>
        /// Import a TargetVerseCorpus datum from a legacy file format
        /// that was used in Clear2.
        /// </summary>
        /// <param name="path">
        /// Path to the input file.
        /// </param>
        /// <param name="segmenter">
        /// An object that implements the ISegmenter interface and that is
        /// used to segment the target text into translated words.
        /// </param>
        /// <param name="puncs">
        /// A list of words that are to be considered punctuation, which is
        /// passed as a parameter to the segmenter.
        /// </param>
        /// <param name="lang">
        /// The name of the target language, which is passed as a parameter
        /// to the segmenter.
        /// </param>
        /// <returns>
        /// A TargetVerseCorpus, representing the translated text which
        /// is to be analyzed by Clear algorithms.
        /// </returns>
        /// 
        public TargetVerseCorpus ImportTargetVerseCorpusFromLegacy(
            string path,
            ISegmenter segmenter,
            List<string> puncs,
            string lang)
        {
            // Prepare to collect TargetVerse objects.
            List<TargetVerse> targetsList2 = new();

            // Reading from the input file:
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                // Prepare to read lines from the input file.
                string line = string.Empty;

                // For each input line:
                while ((line = sr.ReadLine()) != null)
                {
                    // Only process lines with more than 9 characters.
                    if (line.Trim().Length < 9) continue;

                    // Everything up until the first blank is taken to be
                    // a verse ID as a canonical string.
                    string canonicalVerseIDString = line.Substring(0, line.IndexOf(" "));
                    VerseID verseID = new VerseID(canonicalVerseIDString);

                    // The remainder of the line is the translated text.
                    string verseText = line.Substring(line.IndexOf(" ") + 1);

                    // Break the translated text into segments.
                    string[] segments = segmenter.GetSegments(verseText, puncs, lang);

                    // Produce a TargetVerse datum from the segments and
                    // the VerseID.
                    TargetVerse targets = new TargetVerse(
                        segments
                        .Select((segment, j) =>
                            new Target(
                                new TargetText(segment),
                                new TargetID(verseID, j + 1)))
                        .ToList());

                    // Add the TargetVerse datum to the collection.
                    targetsList2.Add(targets);
                }
            }

            // Produce a TargetVerseCorpus datum from the TargetVerse
            // objects that have been collected from the input file.
            return new TargetVerseCorpus(targetsList2);
        }


        /// <summary>
        /// Import a ZoneAlignmentProblem datum from a legacy file format
        /// that was used in Clear2.
        /// </summary>
        /// <param name="parallelSourcePath">
        /// Path to the parallel source file.
        /// </param>
        /// <param name="parallelTargetPath">
        /// Path to the parallel target file.
        /// </param>
        /// <returns>
        /// The ZoneAlignmentProblem represented by the input files.
        /// </returns>
        /// 
        public List<ZoneAlignmentProblem> ImportZoneAlignmentProblemsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath)
        {
            // Read the input files into string arrays, one string for
            // each input line.
            string[] sourceLines = File.ReadAllLines(parallelSourcePath);
            string[] targetLines = File.ReadAllLines(parallelTargetPath);

            // Require that the input files have the same number of lines.
            if (sourceLines.Length != targetLines.Length)
            {
                throw new InvalidDataException(
                    "Parallel files must have same number of lines.");
            }

            // Compute the result by zipping the source and target
            // lines together, breaking the lines into fields, extracting
            // target morph and target ID from the target fields,
            // getting the first and last source IDs from the source fields,
            // and using these results to build a TargetZone datum;
            // then collect all of the TargetZone objects so created into
            // a ZoneAlignmentProblem datum.
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
                        FirstSourceVerseID:
                            getSourceVerseID(sourceStrings.First()),
                        LastSourceVerseID:
                            getSourceVerseID(sourceStrings.Last()));
                })
                .ToList();


            // Local functions:

            // How to break a line into fields.
            IEnumerable<string> fields(string line) =>
                line.Split(' ').Where(s => !String.IsNullOrWhiteSpace(s));

            // How to get the target morph from a field:
            TargetText getTargetMorph(string s) =>
                new TargetText(getBeforeLastUnderscore(s));

            // How to get the target ID from a field:
            TargetID getTargetId(string s) =>
                new TargetID(getAfterLastUnderscore(s));

            // How to get the source verse ID from a field:
            VerseID getSourceVerseID(string s) =>
                (new SourceID(getAfterLastUnderscore(s))).VerseID;

            // How to get text up until the last underscore from a field:
            string getBeforeLastUnderscore(string s) =>
                s.Substring(0, s.LastIndexOf("_"));

            // How to get text after the last underscore from a field:
            string getAfterLastUnderscore(string s) =>
                s.Substring(s.LastIndexOf("_") + 1);
        }


        /// <summary>
        /// Import a TranslationModel datum from a legacy file format
        /// that was used in Clear2.
        /// </summary>
        /// 
        public TranslationModel ImportTranslationModel(
            string filePath)
        {
            // Process each line of the input file, splitting
            // the line into its blank-separated fields.  Keep
            // only those lines with three fields, and interpret
            // the fields as a lemma, a target morph, and a score.
            // Process the result into a dictionary that maps
            // lemma to a dictionary that maps target morph to score.
            return new TranslationModel(
                File.ReadLines(filePath)
                .Select(line => line.Split(' ').ToList())
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


        /// <summary>
        /// Import an AlignmentModel datum from a legacy file format
        /// that was used in Clear3.
        /// </summary>
        /// 
        public AlignmentModel ImportAlignmentModel(
            string filePath)
        {
            // Prepare to use a regular expression to interpret a
            // line of the input file.
            Regex regex = new Regex(
                @"^\s*(\d+)\s*-\s*(\d+)\s+(\S+)\s*$",
                RegexOptions.Compiled);

            // Read each line of the input file, interpreting the line
            // using the regular expression to yield a bare link and
            // a score, and produce a dictionary that maps bare link
            // to score.
            Dictionary<BareLink, Score>
                inner =
                File.ReadLines(filePath)
                .Select(interpretLine)
                .ToDictionary(item => item.Item1, item => item.Item2);

            // Create an AlignmentModel datum from the dictionary so
            // computed.
            return new AlignmentModel(inner);

            // How to obtain a bare link and score from an input line.
            (BareLink, Score) interpretLine(
                string line, int index)
            {
                // Apply the regular expression to the input line.
                Match m = regex.Match(line);

                // Check for errors.
                if (!m.Success)
                    error(index, "invalid input syntax");
                if (m.Groups[1].Length != 12)
                    error(index, "source ID must have 12 digits");
                if (m.Groups[2].Length != 11)
                    error(index, "target ID must have 11 digits");
                if (!double.TryParse(m.Groups[3].Value, out double score))
                    error(index, "third field must be a number");

                // The bare link maps the SourceID from field 1
                // to the target ID from field 2, with the score obtained
                // from parsing field 3 as a double.
                return (
                    new BareLink(
                        new SourceID(m.Groups[1].Value),
                        new TargetID(m.Groups[2].Value)),
                    new Score(score));
            }

            // How to report an error:
            void error(int index, string msg)
            {
                throw new ClearException(
                    $"{filePath} line {index + 1}: {msg}",
                    StatusCode.InvalidInput);
            }
        }


        /// <summary>
        /// Obtain a GroupTranslationsTable datum from a legacy file
        /// format used in Clear2.
        /// </summary>
        /// 
        public GroupTranslationsTable ImportGroupTranslationsTable(
            string filePath)
        {
            // Read lines from the input file, splitting each line into
            // its '#'-separated fields.  Keep only lines with 3 fields.
            // The first field and second fields are strings that encode
            // the source lemmas and the target words of the group
            // translation, respectively.  The third field gives the
            // position of the primary target word.  Express the data
            // as a dictionary that maps a sources-lemmas string to
            // a set of TargetGroup data that each combine a target-words
            // string and a primary position.
            Dictionary<
                SourceLemmasAsText,
                HashSet<TargetGroup>>
                dictionary =
                    File.ReadLines(filePath)
                    .Select(line =>
                        line.Split('#').Select(s => s.Trim()).ToList())
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

            // Produce a GroupTranslationsTable from the dictionary
            // so constructed.
            return new GroupTranslationsTable(dictionary);
        }


        /// <summary>
        /// Import a list of words from a file, which contains
        /// one word per line.
        /// </summary>
        /// 
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


        /// <summary>
        /// Import a list of stop words from a file, which contains
        /// one stop word per line.
        /// </summary>
        /// 
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


        /// <summary>
        /// Obtain a datum from a Clear2 legacy file format used for a
        /// "manual translation model", which has lines with 4 blank-separated
        /// fields for source lemma, target word, count, and probability.
        /// </summary>
        /// 
        public Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(string file)
        {
            // Prepare to collect entries in the result dictionary.
            Dictionary<string, Dictionary<string, Stats>> transModel =
                new Dictionary<string, Dictionary<string, Stats>>();

            // For each input line:
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                // Split line into fields, and keep only lines with
                // 4 fields.
                string[] groups = line.Split(" ".ToCharArray());
                if (groups.Length == 4)
                {
                    // Interpret fields as source lemma, target word,
                    // count, and probability.
                    string source = groups[0].Trim();
                    string target = groups[1].Trim();
                    string sCount = groups[2].Trim();
                    string sProb = groups[3].Trim();

                    // Build a Stats datum with the count and probability.
                    Stats s = new Stats();
                    s.Count = Int32.Parse(sCount);
                    s.Prob = Double.Parse(sProb);

                    // Add the target word and Stats datum to the dictionary
                    // for the source lemma, creating this dictionary if
                    // necessary.
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


        // Input file has lines of the form:
        //   link count
        // Output datum is of the form
        //   Hashtable(link => count)
        //
        /// <summary>
        /// Obtain a good links or bad links dictionary from a legacy
        /// file format used in Clear2.
        /// </summary>
        /// 
        public Dictionary<string, int> GetXLinks(string file)
        {
            // Prepare to collect dictionary entries.
            Dictionary<string, int> xLinks = new Dictionary<string, int>();

            // For each line of the input file:
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                // Split the line into its blank-separated fields,
                // and keep only lines with 2 fields.
                string[] groups = line.Split(" ".ToCharArray());
                if (groups.Length == 2)
                {
                    // Interpret the first field as a good or bad link, and
                    // the second field as a count.
                    string link = groups[0].Trim();
                    int count = Int32.Parse(groups[1]);

                    // Add the mapping from link to count to the dictionary.
                    xLinks.Add(link, count);
                }
            }

            return xLinks;
        }


        /// <summary>
        /// Obtain a dictionary mapping source ID (as a canonical string)
        /// to its two glosses from a legacy file format used in Clear2.
        /// </summary>
        /// 
        public Dictionary<string, Gloss> BuildGlossTableFromFile(
            string glossFile)
        {
            // Prepare to collect dictionary entries:
            Dictionary<string, Gloss> glossTable =
                new Dictionary<string, Gloss>();

            // For each line of the input file:
            string[] lines = File.ReadAllLines(glossFile);
            foreach (string line in lines)
            {
                // Split the line into '#'-separated fields,
                // and keep only those lines with 3 fields.
                string[] groups = line.Split("#".ToCharArray());
                if (groups.Length == 3)
                {
                    // Interpret the first field as the canonical
                    // string of a Source ID.
                    string morphID = groups[0].Trim();

                    // Interpret the other fields as glosses, and use
                    // them to make a Gloss datum.
                    Gloss g = new Gloss();
                    g.Gloss1 = groups[1].Trim();
                    g.Gloss2 = groups[2].Trim();

                    // Add a mapping from the SourceID to the Gloss datum
                    // to the dictionary.
                    glossTable.Add(morphID, g);
                }
            }

            return glossTable;
        }


        /// <summary>
        /// Obtain a datum representing old links from a file in the
        /// alignment.json format as used in Clear2.
        /// </summary>
        /// <param name="jsonFile">
        /// Path to the input file.
        /// </param>
        /// <param name="groups">
        /// Groups database, which is to be updated whenever any links
        /// are found that imply group translations.
        /// </param>
        /// <returns>
        /// A dictionary mapping verseID (as a canonical string) to
        /// a dictionary mapping alternate ID for a source word to
        /// alternate ID for a target word.
        /// </returns>
        /// <remarks>
        /// When this method finds links that have more than one source
        /// word or more than one target word, it interprets them as
        /// group translations to be added to the groups database, rather
        /// than as mappings to be added to the principal output.
        /// </remarks>
        /// 
        public Dictionary<string, Dictionary<string, string>> GetOldLinks(
            string jsonFile,
            GroupTranslationsTable groups)
        {
            // Prepare to collect dictionary entries.
            Dictionary<string, Dictionary<string, string>> oldLinks =
                new Dictionary<string, Dictionary<string, string>>();

            // Deserialize the input file as JSON to obtain an array
            // of LpaLine objects.
            string jsonText = File.ReadAllText(jsonFile);
            LpaLine[] lines = JsonConvert.DeserializeObject<LpaLine[]>(
                jsonText);

            // If deserialization produced nothing, return an empty
            // dictionary.
            if (lines == null) return oldLinks;

            // For each LpaLine object:
            for (int i = 0; i < lines.Length; i++)
            {
                LpaLine line = lines[i];

                // For each link found in the LpaLine object:
                for (int j = 0; j < line.links.Count; j++)
                {
                    LpaLink link = line.links[j];

                    // Express the link as an array of source indices and
                    // an array of target indices, which are to be
                    // interpreted as indices into the manuscript words
                    // and translated words found in the LpaLine object,
                    // respectively.
                    int[] sourceLinks = link.source;
                    int[] targetLinks = link.target;

                    // If there is more than one source link or target link:
                    if (sourceLinks.Length > 1 || targetLinks.Length > 1)
                    {
                        // Interpret this link as a translated group.
                        UpdateGroups(
                            groups,
                            sourceLinks,
                            targetLinks,
                            line.manuscript,
                            line.translation);
                    }
                    else
                    {
                        // Otherwise, this is a one-to-one link.  Get the
                        // (sole) source index and (sole) target index of the
                        // link, and look up the source and target words
                        // information.     
                        int sourceLink = sourceLinks[0];
                        int targetLink = targetLinks[0];
                        LpaManuscriptWord mWord =
                            line.manuscript.words[sourceLink];
                        LpaTranslationWord tWord =
                            line.translation.words[targetLink];

                        // Obtain the verseID (as a canonical string) from the
                        // source word information.
                        string verseID =
                            mWord.id.ToString()
                            .PadLeft(12, '0')
                            .Substring(0, 8);

                        // Add a mapping from source alternate ID to target
                        // alternate ID to the dictionary for the verse ID,
                        // creating this dictionary if necessary.
                        if (oldLinks.ContainsKey(verseID))
                        {
                            Dictionary<string, string> verseLinks = oldLinks[verseID];
                            verseLinks.Add(mWord.altId, tWord.altId);
                        }
                        else
                        {
                            Dictionary<string, string> verseLinks =
                                new Dictionary<string, string>();
                            verseLinks.Add(mWord.altId, tWord.altId);
                            oldLinks.Add(verseID, verseLinks);
                        }
                    }
                }
            }

            return oldLinks;
        }


        /// <summary>
        /// Update the groups table with a mapping from a set of source
        /// links to a set of target links.
        /// </summary>
        /// <param name="groups">
        /// The groups table which is to be updated.
        /// </param>
        /// <param name="sourceLinks">
        /// The source words in the group, represented as indices into
        /// an array of manuscript words.
        /// </param>
        /// <param name="targetLinks">
        /// The target words in the group, represented as indices into
        /// an array of target words.
        /// </param>
        /// <param name="manuscript">
        /// The array of manuscript words that gives meaning to sourceLinks.
        /// </param>
        /// <param name="translation">
        /// The array of translated words that gives meaning to targetLinks.
        /// </param>
        /// 
        private void UpdateGroups(
            GroupTranslationsTable groups,
            int[] sourceLinks,
            int[] targetLinks,
            LpaManuscript manuscript,
            LpaTranslation translation)
        {
            // Make a string that represents the source words as a blank-separated
            // concatenation of the lemmas of the source words.  This string
            // will be used as a key in the groups table.
            SourceLemmasAsText source = new SourceLemmasAsText(
                String.Join(
                    " ",
                    sourceLinks.Select(link => manuscript.words[link].lemma))
                .Trim());

            // Find the first target index number mentioned in the group.
            int firstTargetLink = targetLinks[0];

            // Sort the target link numbers.
            int[] sortedTargetLinks = targetLinks.OrderBy(x => x).ToArray();

            // Get the new position of the index number that used to be first.
            // This number will be the primary position that is part of the
            // entry to be added to the groups table for this mapping.
            PrimaryPosition primaryPosition = new PrimaryPosition(
                sortedTargetLinks
                .Select((link, newIndex) => Tuple.Create(link, newIndex))
                .First(x => x.Item1 == firstTargetLink)
                .Item2);

            // Create a string representation of the target group as
            // concatenation of the target words, separated by
            // "~" when words are skipped and by blank otherwise.
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

            // Prepare to update the groups table.
            Dictionary<
                SourceLemmasAsText,
                HashSet<TargetGroup>>
                inner = groups.Dictionary;

            // Get the set of targets for this group from the groups table,
            // creating the set if it is not already present.
            if (!inner.TryGetValue(source, out var targets))
            {
                targets = new HashSet<TargetGroup>();
                inner[source] = targets;
            }

            // Add an entry for this mapping to the set of group targets,
            // consisting of the encoded target words and the primary position.
            targets.Add(new TargetGroup(targetGroupAsText, primaryPosition));
        }


        /// <summary>
        /// Imports a Strongs table from a legacy file format used in
        /// Clear2.
        /// </summary>
        /// <returns>
        /// A dictionary mapping Strong's codes to a dictionary mapping
        /// target IDs (as canonical strings) to the integer 1.
        /// </returns>
        /// 
        public Dictionary<string, Dictionary<string, int>> BuildStrongTable(
            string strongFile)
        {
            // Prepare to collect dictionary entries.
            Dictionary<string, Dictionary<string, int>> strongTable =
                new Dictionary<string, Dictionary<string, int>>();

            // For each input line:
            string[] strongLines = File.ReadAllLines(strongFile);
            foreach (string strongLine in strongLines)
            {
                // Split the line into its blank-separated fields,
                // interpreting the first field as a Strong's code,
                // second field as the canonical string of a target ID.
                string[] items = strongLine.Split(" ".ToCharArray());
                string wordId = items[0].Trim();
                string strong = items[1].Trim();

                // Add a mapping from the target ID to the integer 1
                // to the sub-dictionary for the Strong's code, creating
                // this sub-dictionary if necessary.
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


        /// <summary>
        /// Obtain a SimpleVersification datum from a legacy file format
        /// used in Clear2.
        /// </summary>
        /// <param name="path">
        /// Path to a versification database in the legacy XML format.
        /// </param>
        /// <param name="versificationType">
        /// A string that names the particular versification type whose
        /// particulars are to be abstracted.
        /// </param>
        /// <returns>
        /// The SimpleVersification datum represented the particulars of
        /// the requested versification.
        /// </returns>
        /// 
        public SimpleVersification ImportSimpleVersificationFromLegacy(
            string path,
            string versificationType)
        {
            // Read the input file obtaining an XML datum.
            XElement legacyVersification = XElement.Load(path);

            // Prepare to keep track of the source verses and
            // target verses for the current versification entry.
            List<VerseID>
                currentSourceVerses = new(),
                currentTargetVerses = new();

            // Prepare to collect SimpleZoneSpec objects.
            List<SimpleZoneSpec> specs = new();

            // How to take a step of interpreting the input.
            void step()
            {
                // If any source or target verses have been
                // encountered, add a SimpleZoneSpec to record
                // them and then reset the collected source and
                // target collections to empty.
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

            // For each child XML element of a parent element which is
            // named "Type" and which has an attribute named "Switch" whose
            // value equals the requested versification type:
            foreach(XElement entry in
                legacyVersification.Elements()
                .First(e =>
                    e.Name.LocalName == "Type" &&
                    e.Attribute("Switch").Value == versificationType)
                .Elements())
            {
                // Extract the manuscript book chapter and verse and
                // the target book chapter and verse from the XML
                // element.
                int mb = entry.AttrAsInt("MB");
                int mc = entry.AttrAsInt("MC");
                int mv = entry.AttrAsInt("MV");
                int tb = entry.AttrAsInt("TB");
                int tc = entry.AttrAsInt("TC");
                int tv = entry.AttrAsInt("TV");

                // Use these value to produce a VerseID for the
                // source verse and the target verse.
                VerseID
                    sourceVerse = new VerseID(mb, mc, mv),
                    targetVerse = new VerseID(tb, tc, tv);

                // If we have already seen the source verse in the
                // current step:
                if (currentSourceVerses.Contains(sourceVerse))
                {
                    // Add the target verse.
                    currentTargetVerses.Add(targetVerse);
                }
                // Otherwise if we have already seen the target verse
                // in the current step:
                else if (currentTargetVerses.Contains(targetVerse))
                {
                    // Add the source verse:
                    currentSourceVerses.Add(sourceVerse);
                }
                else
                {
                    // Both the source and target verses have not
                    // been seen in this step before.

                    // Record the information for the previous step,
                    // and clear the sets of source and target verses
                    // seen so far.
                    step();

                    // Note the source verse and target verse as
                    // having been seen.
                    currentSourceVerses.Add(sourceVerse);
                    currentTargetVerses.Add(targetVerse);
                }
            }

            // All the input has been ingested; finish processing the
            // final step.
            step();

            // Make a SimpleVersification datum from the SimpleZoneSpec
            // objects that have been collected.
            return new SimpleVersification(specs);
        }
    }
}
