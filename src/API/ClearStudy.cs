using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClearBible.Clear3.API
{
    // Some ideas.
    // Not currently part of the API.

    public interface ClearStudyManager
    {
        ClearStudy FindOrCreateStudy(string key);

        void SerializeStudy(string key, string path);
        // can throw ClearException

        void DeserializeStudy(string path, out string key);
        // can throw ClearException

        void DeleteStudy(string key);
        // can throw ClearException
    }

    public interface ClearStudy
    {
        Guid Id { get; }

        string ClientMetadata { get; set; }

        void AddTargetZone(Zone targetZone);
        // throws ArgumentException if zone already present

        void RemoveTargetZone(Zone targetZone);
        // throws ArgumentException if zone is not present

        IEnumerable<Zone> TargetZones();

        void SetAlignment(
            Zone targetZone,
            SourceKind kind,
            Guid AlignmentId);
        // throws ArgumentException if zone is not present

        void ClearAlignment(
            Zone targetZone,
            SourceKind kind);
        // throws ArgumentException if zone is not present

        //Alignment GetAlignment(Zone targetZone, SourceKind kind);
        // throws ArgumentException if zone is not present
        // returns null if no alignment for the kind

        IEnumerable<Guid> AllLexiconIds(SourceKind kind);

        IEnumerable<Guid> LexiconIdsForTargetText(
            SourceKind kind,
            string[] targetText);

        IEnumerable<Guid> QueryLexiconByTargetPatterns(
            SourceKind kind,
            Regex[] regularExpressions);

        IEnumerable<Guid> LexiconIdsForAlignment(SourceKind kind, Guid alignmentId);

        LexiconEntry LexiconEntry(SourceKind kind, Guid lexiconId);
    }

    public enum SourceKind
    {
        Original,
        Gateway
    }

    public struct LexiconEntry
    {
        public readonly Guid Id;
        public readonly string[] sourceDictForms;
        public readonly bool IsPhrase;
        public readonly TargetForm[] targetForms;
    }

    public struct TargetForm
    {
        public readonly string targetText;
        public readonly int count;
        public readonly double rate;
        public readonly Guid[] alignmentIds;
    }




    

    public interface TargetLanguageInfo
    {
        string Name { get; set; }

        // Status SetPunctuations(Uri punctuationSetUri);

        void ClearPunctuations();

        void AddPunctuation(string punctuation);

        IEnumerable<string> Punctuations { get; }
    }

    
}
