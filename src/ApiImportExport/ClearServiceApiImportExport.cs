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

        ITranslationModel ImportTranslationModel_Old(
            IClear30ServiceAPI clearService,
            string filePath);

        IGroupTranslationsTable ImportGroupTranslationsTable_Old(
            IClear30ServiceAPI clearService,
            string filePath);

        ITranslationPairTable ImportTranslationPairTableFromLegacy1(
            IClear30ServiceAPI clearService,
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath);

        TranslationPairTable ImportTranslationPairTableFromLegacy2(
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath);

        List<TranslationPair> ImportTranslationPairsFromLegacy(
            string parallelSourcePath,
            string parallelTargetPath);
    }
}
