using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.ImportExportService
{
    public class ImportExportService : IImportExportService
    {
        public List<TranslationPair> ImportTranslationPairsFromLegacy(
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

                    return new TranslationPair(
                        Targets:
                            targetStrings
                            .Select(s => new Target(
                                getTargetMorph(s),
                                getTargetId(s)))
                            .ToList(),
                        FirstSourceVerseID:
                            getSourceVerseID(sourceStrings.First()),
                        LastSourceVerseID:
                            getSourceVerseID(sourceStrings.Last()));
                })
                .ToList();


            // Local functions:

            IEnumerable<string> fields(string line) =>
                line.Split(' ').Where(s => !String.IsNullOrWhiteSpace(s));

            TargetText getTargetMorph(string s) =>
                new TargetText(getBeforeLastUnderscore(s));

            TargetID getTargetId(string s) =>
                new TargetID(getAfterLastUnderscore(s));

            VerseID getSourceVerseID(string s) =>
                (new SourceID(getAfterLastUnderscore(s))).VerseID;

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
                HashSet<Tuple<TargetGroupAsText, PrimaryPosition>>>
                inner =
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
                                Tuple.Create(record.targ, record.pos))
                            .ToHashSet());

            return new GroupTranslationsTable(inner);
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


        public static List<string> GetStopWords(string file)
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


        public static Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(string file)
        {
            Dictionary<string, Dictionary<string, Stats>> transModel =
                new Dictionary<string, Dictionary<string, Stats>>();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split(" ".ToCharArray());
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
