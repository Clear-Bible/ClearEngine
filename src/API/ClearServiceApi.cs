using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// Clear 3.0 Top Level Interface
    /// </summary>
    /// 
    public interface Clear30ServiceAPI
    {
        ResourceManager ResourceManager { get; }

        Segmenter CreateSegmenter(Uri segmenterAlgorithmUri);
        // can throw ClearException

        Corpus EmptyCorpus { get; }

        PhraseService PhraseService { get; }

        SegmentInstance SegmentInstance(string Text, Place place);

        ZoneService ZoneService { get; }

        TranslationPairTable EmptyTranslationPairTable { get; }

        SMTService SMTService { get; }

        PhraseTranslationModel EmptyPhraseTranslationModel { get; }

        PlaceAlignmentModel EmptyPlaceAlignmentModel { get; }

        AutoAlignmentService AutoAlignmentService { get; }

        // ClearStudyManager ClearStudyManager { get; }      
    }   
}
