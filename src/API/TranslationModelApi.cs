using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ITranslationModel
    {
        void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score);
    }

    public class TranslationModel
    {
        public Dictionary<Lemma, Dictionary<TargetText, Score>>
            Inner { get; }

        public TranslationModel(
            Dictionary<Lemma, Dictionary<TargetText, Score>> inner)
        {
            Inner = inner;
        }
    }
}
