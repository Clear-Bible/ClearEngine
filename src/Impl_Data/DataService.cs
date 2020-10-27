using System;

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

        public SegmentInstance SegmentInstance(string Text, IPlace place) =>
            throw new NotImplementedException();

        public ITranslationModel CreateEmptyTranslationModel() =>
            new TranslationModel();

        public IGroupTranslationsTable CreateEmptyGroupTranslationsTable() =>
            new GroupTranslationsTable();

        public ITranslationPairTable CreateEmptyTranslationPairTable() =>
            new TranslationPairTable_Old();

        public ILemma ILemma(string text) => new Lemma_Bak(text);

        public IMorph IMorph(string text) => new Morph_Bak(text);
    }
}
