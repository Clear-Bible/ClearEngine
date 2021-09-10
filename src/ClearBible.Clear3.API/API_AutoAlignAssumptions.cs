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
        /// <summary>
        /// True if the auto aligner should use content words only.
        /// </summary>
        /// 
        bool ContentWordsOnly { get; }

        /// <summary>
        /// True if the estimated AlignmentModel should influence the
        /// probabilities of the possible target words identified for each
        /// source segment.
        /// </summary>
        /// 
        bool UseAlignModel { get; }

        /// <summary>
        /// True if lemma_cat was used to create the SMT models.
        /// </summary>
        /// 
        bool UseLemmaCatModel { get; }

        /// <summary>
        /// The maximum number of alternatives that the auto-aligner should
        /// permit during its generation of alternatives using tree traversal.
        /// </summary>
        /// 
        int MaxPaths { get; }


        /// <summary>
        /// A database of Strong's information, consisting of a dictionary
        /// mapping a Strong number to a dictionary whose keys are the set
        /// of target texts that are possible definitions of the word.
        /// The auto-aligner gives preference to a Strong's definition when
        /// one is available.
        /// </summary>
        ///
        // FIXME: Use Dictionary<string, HashSet<string>> instead?
        //
        Dictionary<string, Dictionary<string, int>> Strongs { get; }

        /// <summary>
        /// Get the score from the estimated translation model for a
        /// specified lemma and target text, or 0 if the target text
        /// is not a translation for the lemma in the estimated translation
        /// model.
        /// </summary>
        /// 
        double GetTranslationModelScore(
            string lemma,
            string targetTextLower);

        /// <summary>
        /// Returns true if the specified lemma and lowercased target
        /// text has been identified as a bad link.
        /// </summary>
        /// 
        bool IsBadLink(string lemma, string targetTextLower);

        /// <summary>
        /// Returns true if the specified lemma and lowercased target
        /// text has been identified as a good link.
        /// </summary>
        /// 
        bool IsGoodLink(string lemma, string targetTextLower);

        /// <summary>
        /// Returns true if the specified target text is punctuation.
        /// </summary>
        /// 
        bool IsPunctuation(string text);

        /// <summary>
        /// Returns true if the specified source lemma is a function word.
        /// </summary>
        /// 
        bool IsSourceFunctionWord(string lemma);

        /// <summary>
        /// Returns true if the specified source lemma or lowercased target
        /// text is a stop word.
        /// </summary>
        /// 
        bool IsStopWord(string text);

        /// <summary>
        /// Returns true if the specified lowercased target text is a
        /// function word.
        /// </summary>
        /// 
        bool IsTargetFunctionWord(string targetTextLower);

        /// <summary>
        /// Get a dictionary of old links for a specified verse.  When there
        /// is an old link for a source lemma, the auto aligner should give
        /// priority to it.
        /// </summary>
        /// <returns>
        /// A dictionary that maps source word to target
        /// word, as identified by their alternate IDs.  (Alternate IDs have
        /// the form, for example, of "λόγος-2" to mean the second occurence
        /// of the surface form "λόγος" within the verse, or "word-2" to mean
        /// the second occurrence of the surface target text "word"
        /// within the verse.)
        /// </returns>
        /// 
        Dictionary<string, string> OldLinksForVerse(
            string legacyVerseID);

        /// <summary>
        /// Get the translation of a source lemma from the estimated
        /// translation model.
        /// </summary>
        /// <param name="tryGetScoreForTargetText">
        /// Will be set to a function that looks up scores for
        /// target texts, or null if there are no translations for
        /// the lemma.
        /// </param>
        /// <returns>
        /// True if any translations exist and so tryGetScoreForTargetText
        /// is not null.
        /// </returns>
        /// 
        bool TryGetTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText);

        /// <summary>
        /// Get the translation of a source lemma from the confirmed
        /// translation model.
        /// </summary>
        /// <param name="tryGetScoreForTargetText">
        /// Will be set to a function that looks up scores for
        /// target texts, or null if there are no translations for
        /// the lemma.
        /// </param>
        /// <returns>
        /// True if any translations exist and so tryGetScoreForTargetText
        /// is not null.
        /// </returns>
        /// 
        bool TryGetManTranslations(
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText);

        /// <summary>
        /// Look up the score in the estimated alignment model for a
        /// link between specified source and target instances.
        /// </summary>
        /// <param name="score">
        /// Set to the score associated with the link, or to zero if the link
        /// does not occur in the estimated alignment model.
        /// </param>
        /// <returns>
        /// True if the link occurs in the estimated alignment model,
        /// and false otherwise.
        /// </returns>
        /// 
        bool TryGetAlignment(
            string sourceID,
            string targetID,
            out double score);

        /// <summary>
        /// Given a source instance, attempt to find some target instance
        /// to which it is linked in the estimated alignment model.
        /// </summary>
        /// <param name="targetID">
        /// Set to the ID of some target instance that is linked to the given
        /// source instance in the estimated alignment model, or "" if no
        /// such link can be found.
        /// </param>
        /// <returns>
        /// True if some target link was found, and false otherwise.
        /// </returns>
        /// 
        bool TryGetPreAlignment(
            string sourceID,
            out string targetID);
    }
}
