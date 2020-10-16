using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Datatypes
{

    public class Translations
    {
        private Dictionary<string, double> _inner =
            new Dictionary<string, double>();

        public bool ContainsTargetText(string targetText) =>
            _inner.ContainsKey(targetText);

        public double ScoreForTargetText(string targetText) =>
            _inner[targetText];

        public void AddTranslation(string targetText, double score)
        {
            _inner.Add(targetText, score);
        }

        public void SetTranslation(string targetText, double score)
        {
            _inner[targetText] = score;
        }
    }


    public class TranslationModel : ITranslationModel
    {
        private Dictionary<string, Translations> _inner =
            new Dictionary<string, Translations>();

        public bool ContainsSourceLemma(string sourceLemma) =>
            _inner.ContainsKey(sourceLemma);

        public Translations TranslationsForSourceLemma(string sourceLemma) =>
            _inner[sourceLemma];

        public void AddTranslations(string sourceLemma, Translations translations)
        {
            _inner.Add(sourceLemma, translations);
        }

        public void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score)
        {
            if (!_inner.TryGetValue(sourceLemma,
                out Translations translations))
            {
                translations = new Translations();
                _inner.Add(sourceLemma, translations);
            }
            translations.SetTranslation(targetMorph, score);
        }
    }
}
