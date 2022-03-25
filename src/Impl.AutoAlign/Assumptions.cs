using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Miscellaneous;

    /// <summary>
    /// Standard implementation of IAutoAlignAssumptions, based on
    /// the way things were done in Clear2.
    /// 2021.05.27 CL: Added _translationModelTC, _useLemmaCatModel, and _alignProbsPre to be consistent with Clear2
    /// 2022.03.24 CL: Changed puncs, stopWords, sourceFuncWords, targetFuncWords from List<string> to HashSet<string>
    /// </summary>
    /// 
    public class AutoAlignAssumptions : IAutoAlignAssumptions
    {
        private TranslationModel _translationModel;
        private TranslationModel _translationModelTC;
        private bool _useLemmaCatModel;
        private TranslationModel _manTransModel;
        private AlignmentModel _alignProbs;
        private AlignmentModel _alignProbsPre;
        private bool _useAlignModel;
        private HashSet<string> _puncs;
        private HashSet<string> _stopWords;
        private Dictionary<string, int> _goodLinks;
        private int _goodLinkMinCount;
        private Dictionary<string, int> _badLinks;
        private int _badLinkMinCount;
        private Dictionary<string, Dictionary<string, string>> _oldLinks;
        private HashSet<string> _sourceFuncWords;
        private HashSet<string> _targetFuncWords;
        private bool _contentWordsOnly;
        private Dictionary<string, Dictionary<string, int>> _strongs;
        private int _maxPaths;

        private Dictionary<string, string> _preAlignment;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// 
        public AutoAlignAssumptions(
            TranslationModel translationModel,
            TranslationModel translationModelTC,
            bool useLemmaCatModel,
            TranslationModel manTransModel,
            AlignmentModel alignProbs,
            AlignmentModel alignProbsPre,
            bool useAlignModel,
            HashSet<string> puncs,
            HashSet<string> stopWords,
            Dictionary<string, int> goodLinks,
            int goodLinkMinCount,
            Dictionary<string, int> badLinks,
            int badLinkMinCount,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            HashSet<string> sourceFuncWords,
            HashSet<string> targetFuncWords,
            bool contentWordsOnly,
            Dictionary<string, Dictionary<string, int>> strongs,
            int maxPaths)
        {
            _translationModel = translationModel;
            _translationModelTC = translationModelTC;
            _useLemmaCatModel = useLemmaCatModel;
            _manTransModel = manTransModel;
            _alignProbs = alignProbs;
            _alignProbsPre = alignProbsPre;
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
                alignProbsPre.Dictionary.Keys
                .GroupBy(bareLink => bareLink.SourceID)
                .Where(group => group.Any())
                .ToDictionary(
                    group => group.Key.AsCanonicalString,
                    group => group.First().TargetID.AsCanonicalString);
        }


        public bool ContentWordsOnly => _contentWordsOnly;


        public bool UseAlignModel => _useAlignModel;


        public bool UseLemmaCatModel => _useLemmaCatModel;


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
            string sourceLemma,
            string targetLemma)
        {
            if (_translationModel.Dictionary.TryGetValue(new SourceLemma(sourceLemma),
                out Dictionary<TargetLemma, Score> translations))
            {
                if (translations.TryGetValue(new TargetLemma(targetLemma),
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


        // 2021.05.27 CL: Changed to use the translationModelTC to let us use different models to get translations
        // when getting terminal candidates (TC) and when getting alignments for the rest.
        public bool TryGetTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText)
            =>
            TryGetFromTransModel(
                _translationModelTC,
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
                new SourceLemma(lemma),
                out Dictionary<TargetLemma, Score> translations))
            {
                // Debugging
                foreach (var entry in translations)
                {
                    if (double.IsNaN(entry.Value.Double))
                    {
                        ;
                    }
                }


                tryGetScoreForTargetText =
                    (string targetLemma, out double score) =>
                    {
                        if (translations.TryGetValue(
                            new TargetLemma(targetLemma),
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
