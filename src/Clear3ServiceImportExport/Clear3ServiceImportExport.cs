using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;



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


        public ITranslationModel ImportTranslationModel(
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


        public IGroupTranslationsTable ImportGroupTranslationsTable(
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
    }
}
