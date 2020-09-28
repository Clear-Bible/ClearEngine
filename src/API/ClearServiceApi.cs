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

        Corpus CreateEmptyCorpus();

        CorpusService CorpusService { get; }

        ZoneService ZoneService { get; }

        TranslationPairTable MakeEmptyTranslationPairTable();

        SMTService SMTService { get; }

        TextTranslationModelBuilder CreateNewTextTranslationModelBuilder();

        TokenAlignmentModelBuilder CreateNewTokenAlignmentModelBuilder();

        AutoAlignmentService AutoAlignmentService { get; }

        // ClearStudyManager ClearStudyManager { get; }      

        // LemmaService LemmaService { get; }
    }   
}
