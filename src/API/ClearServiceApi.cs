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
        #region Sub-Services

        ResourceService ResourceService { get; }

        SMTService SMTService { get; }

        AutoAlignmentService AutoAlignmentService { get; }

        PhraseService PhraseService { get; }

        ZoneService ZoneService { get; }

        #endregion


        #region Construction of Certain Abstract Data

        Corpus EmptyCorpus { get; }       

        TranslationPairTable EmptyTranslationPairTable { get; }       

        PhraseTranslationModel EmptyPhraseTranslationModel { get; }

        PlaceAlignmentModel EmptyPlaceAlignmentModel { get; }
        
        SegmentInstance SegmentInstance(string Text, Place place);

        #endregion
    }
}
