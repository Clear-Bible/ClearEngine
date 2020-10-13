using System;
using System.Collections.Generic;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.InternalDb
{
    internal class _PhraseTranslationModel : IPhraseTranslationModel
    {
        public string Key { get; }

        public IEnumerable<IPhrase> FindSourcePhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPhrase> FindTargetPhrasesByTextMembers(
            IEnumerable<string> someOfTheTextMembers)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPhrase> SourcePhrases { get; }

        public IEnumerable<IPhrase> TargetPhrases { get; }

        public IEnumerable<IPhrase> TargetsForSource(string sourceKey)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IPhrase> SourcesForTarget(string targetKey)
        {
            throw new NotImplementedException();
        }

        public double Rate(string sourceKey, string targetKey)
        {
            throw new NotImplementedException();
        }

        public double Count(string sourceKey, string targetKey)
        {
            throw new NotImplementedException();
        }

        public IPhraseTranslationModel Add(
            IPhrase sourcePhrase,
            IPhrase targetPhrase)
        {
            throw new NotImplementedException();
        }

        public IPhraseTranslationModel Remove(
            string sourceKey,
            string targetKey)
        {
            throw new NotImplementedException();
        }
    }


    internal class _PhraseTranslationModelEntry
    {
        IPhrase SourcePhrase;
        IPhrase TargetPhrase;
    }
}
