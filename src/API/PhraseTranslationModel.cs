using System;
using System.Collections.Generic;


namespace ClearBible.Clear3.API
{
    /// <summary>
    /// A PhraseTranslationModel represents a counted many-to-many
    /// relationship between source phrases and target phrases.
    /// Counted means that the model keeps track of how many times
    /// a particular relationship has been added and removed.
    /// </summary>
    /// 
    public interface PhraseTranslationModel
    {
        string Key { get; }

        IEnumerable<Phrase> FindSourcePhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers);

        IEnumerable<Phrase> FindTargetPhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers);

        IEnumerable<Phrase> SourcePhrases { get; }

        IEnumerable<Phrase> TargetPhrases { get; }

        IEnumerable<Phrase> TargetsForSource(string sourceKey);

        IEnumerable<Phrase> SourcesForTarget(string targetKey);

        double Rate(string sourceKey, string targetKey);

        double Count(string sourceKey, string targetKey);

        PhraseTranslationModel Add(
            Phrase sourcePhrase,
            Phrase targetPhrase);

        PhraseTranslationModel Remove(
            string sourceKey,
            string targetKey);
    }
}
