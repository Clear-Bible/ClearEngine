using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;



namespace ClearBible.Clear3.ServiceImportExport
{
    using ClearBible.Clear3.APIImportExport;
    using ClearBible.Clear3.Impl.ServiceImportExport;

    public class Clear30ServiceImportExport
    {
        public static IClear30ServiceAPIImportExport Create() =>
            new Clear30ServiceAPIImportExport();
    }
}


namespace ClearBible.Clear3.Impl.ServiceImportExport
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.APIImportExport;


    public class Clear30ServiceAPIImportExport
        : IClear30ServiceAPIImportExport
    {
        public ITranslationPairTable ImportTranslationPairTableFromLegacy1(
            IClear30ServiceAPI clearService,
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath)
        {
            string[] sourceLines = File.ReadAllLines(parallelSourceIdLemmaPath);
            string[] targetLines = File.ReadAllLines(parallelTargetIdPath);

            if (sourceLines.Length != targetLines.Length)
            {
                throw new InvalidDataException(
                    "Parallel files must have same number of lines.");
            }

            ITranslationPairTable table =
                clearService.Data.CreateEmptyTranslationPairTable();

            foreach (var linePair in sourceLines.Zip(targetLines, Tuple.Create))
            {
                table.AddEntry(
                    getFields(linePair.Item1).Select(s =>
                        new LegacySourceSegment(getText(s), getID(s))),
                    getFields(linePair.Item2).Select(s =>
                        new LegacyTargetSegment(getText(s), getID(s))));           
            }

            return table;

            // Local helper functions:

            IEnumerable<string> getFields(string s) =>
                s.Split(' ').Where(s => !String.IsNullOrWhiteSpace(s));

            string getText(string s) => s.Substring(0, s.LastIndexOf("_"));
            string getID(string s) => s.Substring(s.LastIndexOf("_") + 1);           
        }


        public TranslationPairTable ImportTranslationPairTableFromLegacy2(
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath)
        {
            string[] sourceLines = File.ReadAllLines(parallelSourceIdLemmaPath);
            string[] targetLines = File.ReadAllLines(parallelTargetIdPath);

            if (sourceLines.Length != targetLines.Length)
            {
                throw new InvalidDataException(
                    "Parallel files must have same number of lines.");
            }

            return new TranslationPairTable(
                sourceLines
                .Select(line => line
                    .Split(' ')
                    .Where(s => !String.IsNullOrWhiteSpace(s))
                    .Select(s => Tuple.Create(
                        new SourceID(getAfterLastUnderscore(s)),
                        new Lemma(getBeforeLastUnderscore(s))))
                    .ToList())
                .Zip(
                    targetLines
                    .Select(line => line
                        .Split(' ')
                        .Where(s => !String.IsNullOrWhiteSpace(s))
                        .Select(s => Tuple.Create(
                            new TargetID(getAfterLastUnderscore(s)),
                            new TargetText(getBeforeLastUnderscore(s))))
                        .ToList()),
                    Tuple.Create)
                .ToList());

            // Local functions:
            string getBeforeLastUnderscore(string s) =>
                s.Substring(0, s.LastIndexOf("_"));

            string getAfterLastUnderscore(string s) =>
                s.Substring(s.LastIndexOf("_") + 1);
        }


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



        public ITranslationModel ImportTranslationModel_Old(
            IClear30ServiceAPI clearService,
            string filePath)
        {
            //ITranslationModel model =
            //    clearService.Data.CreateEmptyTranslationModel();

            //foreach (string line in File.ReadAllLines(filePath))
            //{
            //    string[] fields =
            //        line.Split(' ').Select(s => s.Trim()).ToArray();
            //    if (fields.Length == 3)
            //    {
            //        model.AddEntry(
            //            sourceLemma: fields[0],
            //            targetMorph: fields[1],
            //            score: Double.Parse(fields[2]));
            //    }
            //}

            //return model;

            IDataService data = clearService.Data;

            var x = File.ReadLines(filePath)
                .Select(line => line.Split(' ').ToArray())
                .Where(fields => fields.Length == 3)
                .Select(fields => new
                {
                    lemma = data.ILemma(fields[0].Trim()),
                    morph = data.IMorph(fields[1].Trim()),
                    score = Double.Parse(fields[2].Trim())
                })
                .GroupBy(row => row.lemma)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        row => row.morph,
                        row => row.score));

            ITranslationModel model =
                clearService.Data.CreateEmptyTranslationModel();

            foreach (var kvp in x)
                foreach (var kvp2 in kvp.Value)
                    model.AddEntry(kvp.Key.Text, kvp2.Key.Text, kvp2.Value);

            return model;
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
               

        public IGroupTranslationsTable ImportGroupTranslationsTable_Old(
            IClear30ServiceAPI clearService,
            string filePath)
        {
            IGroupTranslationsTable table =
                clearService.Data.CreateEmptyGroupTranslationsTable();

            foreach (string line in File.ReadAllLines(filePath))
            {
                string[] fields =
                    line.Split('#').Select(s => s.Trim()).ToArray();
                if (fields.Length == 3)
                {
                    table.AddEntry(
                        sourceGroupLemmas: fields[0],
                        targetGroupAsText: fields[1].ToLower(),
                        primaryPosition: Int32.Parse(fields[2]));
                }
            }

            return table;
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
    }
}
