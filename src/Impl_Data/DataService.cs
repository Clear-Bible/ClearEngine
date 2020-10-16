﻿using System;

namespace ClearBible.Clear3.Impl.Data
{
    using ClearBible.Clear3.API;

    public class DataService : IDataService
    {
        public Corpus EmptyCorpus =>
            throw new NotImplementedException();

        public ITranslationPairTable_Old EmptyTranslationPairTable =>
            throw new NotImplementedException();

        public IPhraseTranslationModel EmptyPhraseTranslationModel =>
            throw new NotImplementedException();

        public PlaceAlignmentModel EmptyPlaceAlignmentModel =>
            throw new NotImplementedException();

        public SegmentInstance SegmentInstance(string Text, Place place) =>
            throw new NotImplementedException();

        public ITranslationModel CreateEmptyTranslationModel() =>
            new TranslationModel();

        public IGroupTranslationsTable CreateEmptyGroupTranslationsTable() =>
            new GroupTranslationsTable();

        public ITranslationPairTable CreateEmptyTranslationPairTable() =>
            new TranslationPairTable();
    }
}
