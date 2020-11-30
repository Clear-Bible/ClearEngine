using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// The caller of AlignZone must supply an object that implements
    /// this interface, in order to express the assumptions that the
    /// auto-alignment algorithm should use.
    /// You can define and use your own object that implements this
    /// interface if desired.
    /// </summary>
    /// 
    public interface IAutoAlignAssumptions
    {
        bool ContentWordsOnly { get; }

        bool UseAlignModel { get; }

        int MaxPaths { get; }

        Dictionary<string, Dictionary<string, int>> Strongs { get; }

        double GetTranslationModelScore(
            string lemma,
            string targetTextLower);

        bool IsBadLink(string lemma, string targetTextLower);

        bool IsGoodLink(string lemma, string targetTextLower);

        bool IsPunctuation(string text);

        bool IsSourceFunctionWord(string lemma);

        bool IsStopWord(string text);

        bool IsTargetFunctionWord(string targetTextLower);

        Dictionary<string, string> OldLinksForVerse(
            string legacyVerseID);

        bool TryGetTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText);

        bool TryGetManTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText);

        bool TryGetAlignment(
            string sourceID,
            string targetID,
            out double score);

        bool TryGetPreAlignment(
            string sourceID,
            out string targetID);
    }
}
