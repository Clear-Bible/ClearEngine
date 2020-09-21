using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Corpus
    {
        Guid Id { get; }

        IEnumerable<Zone> AllZones();

        void AddOrReplaceZone(Zone zone);

        void RemoveZone(Zone zone);

        IEnumerable<Token> Tokens(Zone zone);

        void AppendToken(Zone zone, Token token);

        void AppendText(Zone zone, string text);
    }


    public interface Token
    {
        Guid HomeCorpos { get; }

        Zone HomeZone { get; }

        int HomeZoneIndex { get; }

        string Text { get; }
    }
}
