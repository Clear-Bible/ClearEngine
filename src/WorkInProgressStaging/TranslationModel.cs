using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WorkInProgressStaging
{

    public class TranslationModel
    {
        private Dictionary<string, Dictionary<string, double>> _inner =
            new Dictionary<string, Dictionary<string, double>>();

        public bool ContainsKey(string s) =>
            _inner.ContainsKey(s);

        public Dictionary<string, double> this[string s] =>
            _inner[s];

        public void Add(string s, Dictionary<string, double> translations)
        {
            _inner.Add(s, translations);
        }
    }
}
