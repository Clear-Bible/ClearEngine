using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface TreeService
    {
        Uri Id { get; }

        Corpus Corpus { get; }

        string GetLemma(Place place);
    }
}
