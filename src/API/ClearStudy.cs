using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ClearStudy
    {
        string Key { get; }

        string ClientMetadata { get; set; }

        bool SetTrees(Uri trees, out string status);

        bool SetManuscript(Uri manuscript, out string status);

        bool GetManuscript(out Manuscript manuscript, out string status);

        TargetLanguageInfo TargetLanguageInfo { get; }

        TargetZones TargetZones { get; }
    }

    public interface TargetLanguageInfo
    {
        string Name { get; set; }

        void SetPunctuations(Uri punctuationSetUri);

        void ClearPunctuations();

        void AddPunctuation(string punctuation);
    }

    public interface TargetZones
    {
        TargetZone FindOrCreateStd(int book, int chapter, int verse);

        TargetZone FindOrCreateNonStd(string name);

        void Delete(TargetZone zone);

        IEnumerable<TargetZone> All { get; }
    }
}
