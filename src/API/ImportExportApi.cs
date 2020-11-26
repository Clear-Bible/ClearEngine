using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface IImportExportService
    {
        TranslationModel ImportTranslationModel(
            string filePath);

        AlignmentModel ImportAlignmentModel(
            string filePath);

        GroupTranslationsTable ImportGroupTranslationsTable(
            string filePath);

        List<ZoneAlignmentFacts> ImportZoneAlignmentFactsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath);

        List<string> GetWordList(string file);

        List<string> GetStopWords(string file);

        Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(
            string file);

        Dictionary<string, int> GetXLinks(string file);

        Dictionary<string, Gloss> BuildGlossTableFromFile(string glossFile);

        Dictionary<string, Dictionary<string, string>> GetOldLinks(
            string jsonFile,
            GroupTranslationsTable groups);

        Dictionary<string, Dictionary<string, int>> BuildStrongTable(
            string strongFile);
    }
}
