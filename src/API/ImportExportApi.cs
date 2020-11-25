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

        List<TranslationPair> ImportTranslationPairsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath);
    }
}
