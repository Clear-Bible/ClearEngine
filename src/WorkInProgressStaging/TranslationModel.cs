using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WorkInProgressStaging
{

    public class Translations
    {
        private Dictionary<string, double> _inner =
            new Dictionary<string, double>();

        public bool ContainsKey(string s) =>
            _inner.ContainsKey(s);

        public double this[string s] =>
            _inner[s];

        public void Add(string s, double d)
        {
            _inner.Add(s, d);
        }
    }


    public class TranslationModel
    {
        private Dictionary<string, Translations> _inner =
            new Dictionary<string, Translations>();

        public bool ContainsKey(string s) =>
            _inner.ContainsKey(s);

        public Translations this[string s] =>
            _inner[s];

        public void Add(string s, Translations translations)
        {
            _inner.Add(s, translations);
        }
    }
}
