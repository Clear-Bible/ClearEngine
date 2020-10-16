using System;

using ClearBible.Clear3.API;



namespace ClearBible.Clear3.Service
{
    using ClearBible.Clear3.Impl.Service;

    public class Clear30Service
    {
        public static IClear30ServiceAPI FindOrCreate()
        {
            if (_service == null)
            {
                _service = new Clear30ServiceAPI();
            }

            return _service;
        }

        private static Clear30ServiceAPI _service;
    }
}



namespace ClearBible.Clear3.Impl.Service
{
    using ClearBible.Clear3.Impl.Datatypes;

    internal class Clear30ServiceAPI : IClear30ServiceAPI
    {
        #region Sub-Services

        public ResourceService ResourceService =>
            throw new NotImplementedException();

        public SMTService SMTService =>
            throw new NotImplementedException();

        public IAutoAlignmentService AutoAlignmentService { get; } =
            new AutoAlignmentService();

        public IPhraseService PhraseService =>
            throw new NotImplementedException();

        public ZoneService ZoneService =>
            throw new NotImplementedException();

        #endregion


        #region Construction of Certain Abstract Data

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

        #endregion
    }
}
