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


    internal class _PhraseUnitComparer : IComparer<IPhraseUnit>
    {
        private IComparer<string> _stringComparer;

        public _PhraseUnitComparer(IComparer<string> stringComparer)
        {
            _stringComparer = stringComparer;
        }

        public _PhraseUnitComparer()
        {
            _stringComparer = StringComparer.InvariantCulture;
        }

        public int Compare(IPhraseUnit p1, IPhraseUnit p2)
        {
            if (p1.IsEllipsis)
            {
                return p2.IsEllipsis ? 0 : -1;
            }
            else if (p2.IsEllipsis)
            {
                return 1;
            }
            else
            {
                return _stringComparer.Compare(p1.Text, p2.Text);
            }
        }
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


    internal class _PhraseComparer : IComparer<IPhrase>
    {
        private IComparer<IPhraseUnit> _phraseUnitComparer;
        private readonly int
            one_less_than_two = -1,
            one_equals_two = 0,
            one_greater_than_two = 1;

        public _PhraseComparer(IComparer<IPhraseUnit> phraseUnitComparer)
        {
            _phraseUnitComparer = phraseUnitComparer;
        }


        public int Compare(IPhrase p1, IPhrase p2)
        {
            IEnumerator<IPhraseUnit> units1 = p1.PhraseUnits.GetEnumerator();
            IEnumerator<IPhraseUnit> units2 = p2.PhraseUnits.GetEnumerator();

            while (true)
            {
                bool another1 = units1.MoveNext();
                bool another2 = units2.MoveNext();

                if (another1)
                {
                    if (another2)
                    {
                        int comparison = _phraseUnitComparer.Compare(
                            units1.Current,
                            units2.Current);
                        if (comparison != one_equals_two)
                        {
                            return comparison;
                        }
                        else
                        {
                            // move on to next phrase unit
                        }
                    }
                    else
                    {
                        return one_greater_than_two;
                    }
                }
                else 
                {
                    if (another2)
                    {
                        return one_less_than_two;
                    }
                    else
                    {
                        return one_equals_two;
                    }
                }
            }
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
