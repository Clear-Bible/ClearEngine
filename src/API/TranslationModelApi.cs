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
        public Dictionary<Lemma, Dictionary<TargetMorph, Score>>
            Inner { get; }

        public TranslationModel(
            Dictionary<Lemma, Dictionary<TargetMorph, Score>> inner)
        {
            Inner = inner;
        }
    }
}
