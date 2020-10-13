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
    public interface IPhraseTranslationModel
    {
        string Key { get; }

        IEnumerable<IPhrase> FindSourcePhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers);

        IEnumerable<IPhrase> FindTargetPhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers);

        IEnumerable<IPhrase> SourcePhrases { get; }

        IEnumerable<IPhrase> TargetPhrases { get; }

        IEnumerable<IPhrase> TargetsForSource(string sourceKey);

        IEnumerable<IPhrase> SourcesForTarget(string targetKey);

        double Rate(string sourceKey, string targetKey);

        double Count(string sourceKey, string targetKey);

        IPhraseTranslationModel Add(
            IPhrase sourcePhrase,
            IPhrase targetPhrase);

        IPhraseTranslationModel Remove(
            string sourceKey,
            string targetKey);
    }
}
