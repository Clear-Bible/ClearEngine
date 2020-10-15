using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// Clear 3.0 Top Level Interface
    /// </summary>
    /// 
    public interface IClear30ServiceAPI
    {
        #region Sub-Services

        ResourceService ResourceService { get; }

        SMTService SMTService { get; }

        IAutoAlignmentService AutoAlignmentService { get; }

        IPhraseService PhraseService { get; }

        ZoneService ZoneService { get; }

        #endregion


        #region Construction of Certain Abstract Data

        Corpus EmptyCorpus { get; }       

        TranslationPairTable EmptyTranslationPairTable { get; }       

        IPhraseTranslationModel EmptyPhraseTranslationModel { get; }

        PlaceAlignmentModel EmptyPlaceAlignmentModel { get; }
        
        SegmentInstance SegmentInstance(string Text, Place place);

        ITranslationModel CreateEmptyTranslationModel();

        #endregion
    }
}
