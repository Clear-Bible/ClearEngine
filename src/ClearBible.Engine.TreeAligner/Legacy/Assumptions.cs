using ClearBible.Engine.TreeAligner.Translation;

namespace ClearBible.Engine.TreeAligner.Legacy
{
    //-2
    //using ClearBible.Clear3.API;
    //using ClearBible.Clear3.Impl.Miscellaneous;

    /// <summary>
    /// Standard implementation of IAutoAlignAssumptions, based on
    /// the way things were done in Clear2.
    /// 2021.05.27 CL: Added _translationModelTC, _useLemmaCatModel, and _alignProbsPre to be consistent with Clear2
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
            ManuscriptTreeWordAlignerParams manuscriptTreeWordAlignerParams,
            TranslationModel translationModel,
            TranslationModel translationModelTC,
            AlignmentModel alignProbs,
            AlignmentModel alignProbsPre
            )
        {
            _translationModel = translationModel;
            _translationModelTC = translationModelTC;
            _useLemmaCatModel = manuscriptTreeWordAlignerParams.useLemmaCatModel;
            _manTransModel = manuscriptTreeWordAlignerParams.manTransModel;
            _alignProbs = alignProbs;
            _alignProbsPre = alignProbsPre;
            _useAlignModel = manuscriptTreeWordAlignerParams.useAlignModel;
            _puncs = manuscriptTreeWordAlignerParams.puncs;
            _stopWords = manuscriptTreeWordAlignerParams.stopWords;
            _goodLinks = manuscriptTreeWordAlignerParams.goodLinks;
            _goodLinkMinCount = manuscriptTreeWordAlignerParams.goodLinkMinCount;
            _badLinks = manuscriptTreeWordAlignerParams.badLinks;
            _badLinkMinCount = manuscriptTreeWordAlignerParams.badLinkMinCount;
            _oldLinks = manuscriptTreeWordAlignerParams.oldLinks;
            _sourceFuncWords = manuscriptTreeWordAlignerParams.sourceFunctionWords;
            _targetFuncWords = manuscriptTreeWordAlignerParams.targetFunctionWords;
            _contentWordsOnly = manuscriptTreeWordAlignerParams.contentWordsOnly;
            _strongs = manuscriptTreeWordAlignerParams.strongs;
            _maxPaths = manuscriptTreeWordAlignerParams.maxPaths;

            _preAlignment =
                alignProbsPre.Dictionary.Keys
                .GroupBy(bareLink => bareLink.SourceID)
                .Where(group => group.Any())
                .ToDictionary(
                    group => group.Key.AsCanonicalString,
                    group => group.First().TargetID.AsCanonicalString);
        }
        /* //-
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
        */

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


        //+ all the following for reference

        /// <summary>
        /// <para>
        /// Create assumptions for the tree-based auto-aligner
        /// based on certain standard inputs, after the manner of Clear2.
        /// </para>
        /// <para>
        /// Note that you can also create your own assumptions by
        /// supplying a custom object that implements the
        /// IAutoAlignAssumptions interface.
        /// </para>
        /// </summary>
        /// <param name="translationModel">
        /// An estimated TranslationModel such as one obtained from training
        /// a statistical translation model with a ParallelCorpora that is to
        /// be aligned.
        /// </param>
        /// <param name="manTransModel">
        /// A confirmed TranslationModel such as one obtained by analyzing
        /// a database of manual alignments.
        /// </param>
        /// <param name="alignProbs">
        /// An estimated AlignmentModel such as one obtained from training
        /// a statistical translation model with a ParallelCorpora that is to
        /// be aligned.
        /// </param>
        /// <param name="useAlignModel">
        /// True if the estimated AlignmentModel should influence the
        /// probabilities of the possible target words identified for each
        /// source segment.
        /// </param>
        /// <param name="puncs">
        /// A set of target texts that are to be considered as punctuation.
        /// </param>
        /// <param name="stopWords">
        /// A set of source lemmas and lowercased target texts that should
        /// not participate in linking.
        /// </param>
        /// <param name="goodLinks">
        /// A dictionary mapping strings of the form xxx#yyy (where xxx is
        /// a lemma and yyy is a lower-cased target text) to a count,
        /// representing that the association between the lemma and the
        /// target text has been found to be good for the count number of
        /// times.
        /// </param>
        /// <param name="goodLinkMinCount">
        /// The count threshold at which the auto-aligner algorithm will
        /// allow a good link to influence the auto alignment.
        /// </param>
        /// <param name="badLinks">
        /// A dictionary mapping strings of the form xxx#yyy (where xxx is
        /// a lemma and yyy is a lower-cased target text) to a count,
        /// representing that the association between the lemma and the
        /// target text has been found to be good for the count number of
        /// times.
        /// </param>
        /// <param name="badLinkMinCount">
        /// The count threshold at which the auto-aligner algorithm will
        /// allow a bad link to influence the auto alignment.
        /// </param>
        /// <param name="oldLinks">
        /// A database of old links, organized by verse, and using alternate
        /// IDs to identify the sources and targets.  (Alternate IDs have the
        /// form, for example, of "λόγος-2" to mean the second occurence of
        /// the lemma "λόγος" within the verse, or "word-2" to mean the
        /// second occurrence of the lowercased target text "word"
        /// within the verse.)  The auto-aligner gives preference to these
        /// old links when it is identifying possible choices of target word
        /// for a source word.  The use of alternate IDs is intended to help
        /// in case the translation of the verse has changed since the old
        /// links were identified.
        /// </param>
        /// <param name="sourceFuncWords">
        /// Those lemmas that are to be considered function words rather than
        /// content words.
        /// </param>
        /// <param name="targetFuncWords">
        /// Those lowercased target texts that are to be considered function
        /// words rather than content words.
        /// </param>
        /// <param name="contentWordsOnly">
        /// True if the auto-aligner should consider content words only.
        /// </param>
        /// <param name="strongs">
        /// A database of Strong's information, consisting of a dictionary
        /// mapping a Strong number to a dictionary whose keys are the set
        /// of target texts that are possible definitions of the word.
        /// The auto-aligner gives preference to a Strong's definition when
        /// one is available.
        /// </param>
        /// <param name="maxPaths">
        /// The maximum number of alternatives that the auto-aligner should
        /// permit during its generation of alternatives using tree traversal.
        /// </param>
        /// <returns>
        /// An IAutoAlignAssumptions object that the auto-aligner uses in
        /// various ways to influence its behavior.
        /// </returns>
        /// 

    }
}
