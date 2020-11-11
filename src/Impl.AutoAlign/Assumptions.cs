using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;

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
        }


        public bool IsPunctuation(TargetWord tw) =>
                _puncs.Contains(tw.Text);


        public bool IsTargetStopWord(TargetWord tw) =>
            _stopWords.Contains(tw.Text);


        public bool IsSourceStopWord(SourceNode sn) =>
            _stopWords.Contains(sn.Lemma);


        public bool IsBadLink(SourceNode sn, TargetWord tw)
        {
            string link = $"{sn.Lemma}#{tw.Text}";
            return
                _badLinks.ContainsKey(link) &&
                _badLinks[link] >= _badLinkMinCount;
        }


        public bool IsGoodLink(SourceNode sn, TargetWord tw)
        {
            string link = $"{sn.Lemma}#{tw.Text}";
            return
                _goodLinks.ContainsKey(link) &&
                _goodLinks[link] >= _goodLinkMinCount;
        }


        public double GetTranslationModelScore(SourceNode sn, TargetWord tw)
        {
            if (_translationModel.Inner.TryGetValue(new Lemma(sn.Lemma),
                out Dictionary<TargetMorph, Score> translations))
            {
                if (translations.TryGetValue(new TargetMorph(tw.Text),
                    out Score score))
                {
                    return score.Double;
                }
            }

            return 0;
        }
    }
}
