using System;
using System.Collections.Generic;

namespace DeadEndWip
{
    public class Stats2
    {
        public int Count;
        public double Prob;
    }


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



    public interface ITranslationModel
    {
        void AddEntry(
            string sourceLemma,
            string targetMorph,
            double score);
    }



    public class TranslationModel_Old : ITranslationModel
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



    public class GroupTranslation_Old
    {
        public string TargetGroupAsText;
        public int PrimaryPosition;

        public GroupTranslation_Old(
            string targetGroupAsText,
            int primaryPosition)
        {
            TargetGroupAsText = targetGroupAsText;
            PrimaryPosition = primaryPosition;
        }

        public GroupTranslation_Old() { }
    }


    public class GroupTranslations_Old
    {
        private List<GroupTranslation_Old> _inner =
            new List<GroupTranslation_Old>();

        public IEnumerable<GroupTranslation_Old> AllTranslations =>
            _inner;

        public void Add(GroupTranslation_Old targetGroupTranslation)
        {
            _inner.Add(targetGroupTranslation);
        }
    }


    public class GroupTranslationsTable_Old
    {
        private Dictionary<string, GroupTranslations_Old> _inner =
            new Dictionary<string, GroupTranslations_Old>();

        public bool ContainsSourceGroupKey(string sourceGroupLemmas) =>
            _inner.ContainsKey(sourceGroupLemmas);

        public GroupTranslations_Old TranslationsForSourceGroup(
            string sourceGroupLemmas) =>
                _inner[sourceGroupLemmas];

        public IEnumerable<KeyValuePair<string, GroupTranslations_Old>>
            AllEntries =>
                _inner;

        public void Add(
            string sourceGroupLemmas,
            GroupTranslations_Old translations)
        {
            _inner.Add(sourceGroupLemmas, translations);
        }

        public void AddEntry(
            string sourceGroupLemmas,
            string targetGroupAsText,
            int primaryPosition)
        {
            if (!_inner.TryGetValue(sourceGroupLemmas,
                out GroupTranslations_Old groupTranslations))
            {
                groupTranslations = new GroupTranslations_Old();
                _inner.Add(sourceGroupLemmas, groupTranslations);
            }
            groupTranslations.Add(new GroupTranslation_Old(
                targetGroupAsText,
                primaryPosition));
        }
    }
}
