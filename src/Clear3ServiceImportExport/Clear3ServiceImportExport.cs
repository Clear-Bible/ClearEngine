using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using ClearBible.Clear3.API;
using ClearBible.Clear3.APIImportExport;

namespace ClearBible.Clear3.ServiceImportExport
{
    public class Clear30ServiceImportExport
    {
        public static IClear30ServiceAPIImportExport Create() =>
            new Clear30ServiceAPIImportExport();
    }


    internal class Clear30ServiceAPIImportExport
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
                clearService.CreateEmptyTranslationPairTable();

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
    }
}
