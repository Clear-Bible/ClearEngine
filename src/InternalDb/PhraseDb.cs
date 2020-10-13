using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.InternalDb
{
    internal class _PhraseUnit : IPhraseUnit
    {
        public string Text { get; }
        public bool IsEllipsis { get; }
        public string Key { get; }

        public _PhraseUnit(string text, bool isEllipsis, string key)
        {
            Text = text;
            IsEllipsis = isEllipsis;
            Key = key;
        }

        public static string MakeKey(string text, bool isEllipsis) =>
            DbUtility.MakeKey(text, isEllipsis);

        public static readonly string EllipsisKey = MakeKey("", true);
    }


    internal class _Phrase : IPhrase
    {
        public IEnumerable<IPhraseUnit> PhraseUnits => _phraseUnits;
        public int PrimaryPhraseUnitIndex { get; }
        public string Key { get; }

        private IPhraseUnit[] _phraseUnits;

        public _Phrase(
            IEnumerable<IPhraseUnit> phraseUnits,
            int primaryPhraseUnitIndex,
            string key)
        {
            _phraseUnits = phraseUnits.ToArray();
            PrimaryPhraseUnitIndex = primaryPhraseUnitIndex;
            Key = key;
        }

        public static string MakeKey(IEnumerable<IPhraseUnit> phraseUnits)
        {
            return DbUtility.MakeKey(phraseUnits.Select(p => p.Key));
        }
    }



    public class PhraseUnitService : IPhraseService
    {
        public IPhraseUnit PhraseUnit(string text)
        {
            return DbUtility.LookupOrCreate(
                _phraseUnitXIndex,
                () => _PhraseUnit.MakeKey(text, false),
                key => new _PhraseUnit(text, false, key));
        }

        public IPhraseUnit PhraseUnitEllipsis()
        {
            return _phraseUnitXIndex[_PhraseUnit.EllipsisKey];
        }

        public IPhraseUnit PhraseUnitByKey(string key)
        {
            return DbUtility.LookupByKey(key, _phraseUnitXIndex);
        }

        public IPhrase Phrase(
            IEnumerable<IPhraseUnit> phraseUnits,
            int primaryPhraseUnitIndex)
        {
            return DbUtility.LookupOrCreate(
                _phraseXIndex,
                () => _Phrase.MakeKey(phraseUnits),
                key => new _Phrase(phraseUnits, primaryPhraseUnitIndex, key));
        }

        public IPhrase PhraseFromText(string text)
        {
            return Phrase(new IPhraseUnit[] { PhraseUnit(text) }, 0);
        }

        public IPhrase PhraseByKey(string key)
        {
            return DbUtility.LookupByKey(key, _phraseXIndex);
        }

        private Dictionary<string, _PhraseUnit> _phraseUnitXIndex;

        private Dictionary<string, _Phrase> _phraseXIndex;

        public PhraseUnitService()
        {
            _phraseUnitXIndex = new Dictionary<string, _PhraseUnit>();
            string ellipsisKey = _PhraseUnit.EllipsisKey;
            _phraseUnitXIndex[ellipsisKey] =
                new _PhraseUnit("", true, ellipsisKey);
            _phraseXIndex = new Dictionary<string, _Phrase>();
        }
    }
}
