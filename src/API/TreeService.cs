using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ITreeService
    {
        Uri IdUri { get; }

        Corpus Corpus { get; }

        string GetLemma(IPlace place);

        string GetStrong(IPlace place);

        string GetPartOfSpeech(IPlace place);

        string GetMorphology(IPlace place);

        long GetLegacyID(IPlace place);
    }
}
