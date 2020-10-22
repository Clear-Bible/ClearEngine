using System;

namespace ClearBible.Clear3.API
{
    public interface IDataService
    {
        Corpus EmptyCorpus { get; }

        ITranslationPairTable_Old EmptyTranslationPairTable { get; }

        IPhraseTranslationModel EmptyPhraseTranslationModel { get; }

        PlaceAlignmentModel EmptyPlaceAlignmentModel { get; }

        SegmentInstance SegmentInstance(string Text, IPlace place);

        ITranslationModel CreateEmptyTranslationModel();

        IGroupTranslationsTable CreateEmptyGroupTranslationsTable();

        ITranslationPairTable CreateEmptyTranslationPairTable();
    }
}
