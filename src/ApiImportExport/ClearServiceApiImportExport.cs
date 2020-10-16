﻿using System;
using System.Collections.Generic;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.APIImportExport
{
    public interface IClear30ServiceAPIImportExport
    {
        ITranslationModel ImportTranslationModel(
            IClear30ServiceAPI clearService,
            string filePath);

        IGroupTranslationsTable ImportGroupTranslationsTable(
            IClear30ServiceAPI clearService,
            string filePath);

        ITranslationPairTable ImportTranslationPairTableFromLegacy1(
            IClear30ServiceAPI clearService,
            string parallelSourceIdLemmaPath,
            string parallelTargetIdPath);
    }
}