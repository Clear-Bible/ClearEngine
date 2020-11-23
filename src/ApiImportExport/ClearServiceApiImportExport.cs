using System;
using System.Collections.Generic;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.APIImportExport
{
    public interface IClear30ServiceAPIImportExport
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
