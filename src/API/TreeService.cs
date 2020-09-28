using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface TreeService
    {
        Uri Id { get; }

        Corpus Corpus { get; }

        Corpus CreateCorpus(IEnumerable<Zone> zones);

        string LemmaForToken(Token token);

        IEnumerable<Token> GetTokens(Zone zone, bool lemmas);
    }
}
