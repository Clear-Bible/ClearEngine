using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Miscellaneous;

    public class AutoAlignAssumptions : IAutoAlignAssumptions
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
        private int _maxPaths;

        private Dictionary<string, string> _preAlignment;

        public AutoAlignAssumptions(
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
            Dictionary<string, Dictionary<string, int>> strongs,
            int maxPaths)
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
            _maxPaths = maxPaths;

            _preAlignment =
                alignProbs.Dictionary.Keys
                .GroupBy(bareLink => bareLink.SourceID)
                .Where(group => group.Any())
                .ToDictionary(
                    group => group.Key.AsCanonicalString,
                    group => group.First().TargetID.AsCanonicalString);
        }


        public bool ContentWordsOnly => _contentWordsOnly;


        public bool UseAlignModel => _useAlignModel;


        public int MaxPaths => _maxPaths;


        public bool IsPunctuation(string text) =>
            _puncs.Contains(text);


        public bool IsStopWord(string text) =>
            _stopWords.Contains(text);


        public bool IsTargetFunctionWord(string text) =>
            _targetFuncWords.Contains(text);


        public bool IsSourceFunctionWord(string lemma) =>
            _sourceFuncWords.Contains(lemma);


        public bool IsBadLink(string lemma, string targetTextLower)
        {
            string link = $"{lemma}#{targetTextLower}";
            return
                _badLinks.ContainsKey(link) &&
                _badLinks[link] >= _badLinkMinCount;
        }


        public bool IsGoodLink(string lemma, string targetTextLower)
        {
            string link = $"{lemma}#{targetTextLower}";
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


        public double GetTranslationModelScore(
            string lemma,
            string targetTextLower)
        {
            if (_translationModel.Dictionary.TryGetValue(new Lemma(lemma),
                out Dictionary<TargetText, Score> translations))
            {
                if (translations.TryGetValue(new TargetText(targetTextLower),
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
            var key = new BareLink(
                new SourceID(sourceID),
                new TargetID(targetID));

            if (_alignProbs.Dictionary.TryGetValue(key, out Score score2))
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
            string sourceID,
            out string targetID)
            =>
            _preAlignment.TryGetValue(sourceID, out targetID);


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
            if (translationModel.Dictionary.TryGetValue(
                new Lemma(lemma),
                out Dictionary<TargetText, Score> translations))
            {
                tryGetScoreForTargetText =
                    (string targetText, out double score) =>
                    {
                        if (translations.TryGetValue(
                            new TargetText(targetText),
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
