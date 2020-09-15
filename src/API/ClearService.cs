using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ClearService
    {
        ClearStudy FindOrCreateStudy(string key);

        bool SerializeStudy(string key, string path, out string status);

        bool DeserializeStudy(string path, out string key, out string status);

        TargetZoneId StdTargetZoneId(int book, int chapter, int verse);

        TargetZoneId VariantTargetZoneId(string name);

        bool SetLocalResourceFolder(string path, out string status);

        bool DownloadResource(Uri uri, out string status);

        bool QueryLocalResources(
            out IEnumerable<LocalResource> resources,
            out string status);
    }

    public interface LocalResource
    {
        Uri Id { get; }

        DateTime DownloadMoment { get; }

        bool Ok { get; }

        string Status { get; }

        string Description { get; }
    }
}
