using System;
using System.Collections.Generic;


namespace ClearBible.Clear3.API
{
    public interface IPhrase
    {
        string Key { get; }

        IEnumerable<IPhraseUnit> PhraseUnits { get; }

        int PrimaryPhraseUnitIndex { get; }
    }

    /// <summary>
    /// A PhraseUnit is either a Text or an Ellipsis.
    /// </summary>
    /// 
    public interface IPhraseUnit
    {
        string Key { get; }

        bool IsEllipsis { get; }

        string Text { get; }
    }


    public interface IPhraseService
    {
        IPhraseUnit PhraseUnit(string text);

        IPhraseUnit PhraseUnitEllipsis();

        IPhraseUnit PhraseUnitByKey(string key);

        IPhrase Phrase(
            IEnumerable<IPhraseUnit> phraseUnits,
            int primaryPhraseUnitIndex);

        IPhrase PhraseFromText(string text);

        IPhrase PhraseByKey(string key);
    }
}
