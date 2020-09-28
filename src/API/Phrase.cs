using System;
using System.Collections.Generic;


namespace ClearBible.Clear3.API
{
    public interface Phrase
    {
        string Key { get; }

        IEnumerable<PhraseUnit> PhraseUnits { get; }
    }

    /// <summary>
    /// A PhraseUnit is either a Text or an Ellipsis.
    /// </summary>
    /// 
    public interface PhraseUnit
    {
        string Key { get; }

        bool IsEllipsis { get; }

        string Text { get; }
    }


    public interface PhraseService
    {
        PhraseUnit PhraseUnit(string text);

        PhraseUnit PhraseUnitEllipsis();

        PhraseUnit PhraseUnitByKey(string key);

        Phrase Phrase(IEnumerable<PhraseUnit> phraseUnits);

        Phrase PhraseText(string text);

        Phrase PhraseByKey(string key);
    }
}
