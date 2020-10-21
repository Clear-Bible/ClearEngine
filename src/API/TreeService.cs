using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ITreeService
    {
        Uri IdUri { get; }

        Corpus Corpus { get; }

        string GetLemma(Place place);

        string GetStrong(Place place);

        string GetPartOfSpeech(Place place);

        string GetMorphology(Place place);

        long GetLegacyID(Place place);
    }
}
