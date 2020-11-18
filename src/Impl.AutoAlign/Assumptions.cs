using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Miscellaneous;

    public class Assumptions
    {
        private TranslationModel _translationModel;
        private TranslationModel _manTransModel;
        private AlignmentModel _alignProbs;
        private bool _useAlignModel;
        private List<string> _puncs;
        private List<string> _stopWords;
        private Dictionary<string, int> _goodLinks;
        private int _goodLinkMinCount;
        private Dictionary<string, int> _badLinks;
        private int _badLinkMinCount;
        private Dictionary<string, Dictionary<string, string>> _oldLinks;
        private List<string> _sourceFuncWords;
        private List<string> _targetFuncWords;
        private bool _contentWordsOnly;
        private Dictionary<string, Dictionary<string, int>> _strongs;

        private Dictionary<string, string> _preAlignment;

        public Assumptions(
            TranslationModel translationModel,
            TranslationModel manTransModel,
            AlignmentModel alignProbs,
            bool useAlignModel,
            List<string> puncs,
            List<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            List<string> sourceFuncWords,
            List<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs)
        {
            _translationModel = translationModel;
            _manTransModel = manTransModel;
            _alignProbs = alignProbs;
            _useAlignModel = useAlignModel;
            _puncs = puncs;
            _stopWords = stopWords;
            _goodLinks = goodLinks;
            _goodLinkMinCount = goodLinkMinCount;
            _badLinks = badLinks;
            _badLinkMinCount = badLinkMinCount;
            _oldLinks = oldLinks;
            _sourceFuncWords = sourceFuncWords;
            _targetFuncWords = targetFuncWords;
            _contentWordsOnly = contentWordsOnly;
            _strongs = strongs;

            _preAlignment =
                alignProbs.Inner.Keys
                .GroupBy(pair => pair.Item1)
                .Where(group => group.Any())
                .ToDictionary(
                    group => group.Key.AsCanonicalString,
                    group => group.First().Item2.AsCanonicalString);
        }


        public bool ContentWordsOnly => _contentWordsOnly;


        public bool UseAlignModel => _useAlignModel;


        public bool IsPunctuation(string text) =>
            _puncs.Contains(text);


        public bool IsPunctuation(MaybeTargetPoint tw) =>
            _puncs.Contains(tw.Lower);


        public bool IsStopWord(string text) =>
            _stopWords.Contains(text);


        public bool IsTargetStopWord(MaybeTargetPoint tw) =>
            _stopWords.Contains(tw.Lower);


        public bool IsTargetFunctionWord(string text) =>
            _targetFuncWords.Contains(text);


        public bool IsSourceStopWord(SourceNode sn) =>
            _stopWords.Contains(sn.Lemma);

        public bool IsSourceFunctionWord(string lemma) =>
            _sourceFuncWords.Contains(lemma);

        public bool IsSourceFunctionWord(SourceNode sn) =>
            _sourceFuncWords.Contains(sn.Lemma);


        public bool IsBadLink(SourceNode sn, MaybeTargetPoint tw)
        {
            string link = $"{sn.Lemma}#{tw.Lower}";
            return
                _badLinks.ContainsKey(link) &&
                _badLinks[link] >= _badLinkMinCount;
        }


        public bool IsBadLink(string lemma, string targetText)
        {
            string link = $"{lemma}#{targetText}";
            return
                _badLinks.ContainsKey(link) &&
                _badLinks[link] >= _badLinkMinCount;
        }


        public bool IsGoodLink(SourceNode sn, MaybeTargetPoint tw)
        {
            string link = $"{sn.Lemma}#{tw.Lower}";
            return
                _goodLinks.ContainsKey(link) &&
                _goodLinks[link] >= _goodLinkMinCount;
        }


        public Dictionary<string, string> OldLinksForVerse(
            string legacyVerseID)
        {
            if (_oldLinks.TryGetValue(legacyVerseID,
                out Dictionary<string, string> linksForVerse))
            {
                return linksForVerse;
            }
            else
            {
                return new Dictionary<string, string>();
            }
        }


        public double GetTranslationModelScore(SourceNode sn, MaybeTargetPoint tw)
        {
            if (_translationModel.Inner.TryGetValue(new Lemma(sn.Lemma),
                out Dictionary<TargetMorph, Score> translations))
            {
                if (translations.TryGetValue(new TargetMorph(tw.Lower),
                    out Score score))
                {
                    return score.Double;
                }
            }

            return 0;
        }


        public bool TryGetAlignment(
            string sourceID,
            string targetID,
            out double score)
        {
            var key = Tuple.Create(
                new SourceID(sourceID),
                new TargetID(targetID));

            if (_alignProbs.Inner.TryGetValue(key, out Score score2))
            {
                score = score2.Double;
                return true;
            }
            else
            {
                score = 0;
                return false;
            }
        }


        public bool TryGetPreAlignment(
            SourceNode sn,
            out string targetID)
            =>
            _preAlignment.TryGetValue(sn.MorphID, out targetID);


        public Dictionary<string, Dictionary<string, int>> Strongs =>
            _strongs;


        public bool TryGetTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText)
            =>
            TryGetFromTransModel(
                _translationModel,
                lemma,
                out tryGetScoreForTargetText);


        public bool TryGetManTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText)
            =>
            TryGetFromTransModel(
                _manTransModel,
                lemma,
                out tryGetScoreForTargetText);


        private bool TryGetFromTransModel(
            TranslationModel translationModel,
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText)
        {
            if (translationModel.Inner.TryGetValue(
                new Lemma(lemma),
                out Dictionary<TargetMorph, Score> translations))
            {
                tryGetScoreForTargetText =
                    (string targetText, out double score) =>
                    {
                        if (translations.TryGetValue(
                            new TargetMorph(targetText),
                            out Score score2))
                        {
                            score = score2.Double;
                            return true;
                        }
                        else
                        {
                            score = 0;
                            return false;
                        }
                    };
                return true;
            }
            else
            {
                tryGetScoreForTargetText = null;
                return false;
            }
        }
    }
}
