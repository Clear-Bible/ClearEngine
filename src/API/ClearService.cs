using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Status
    {
        bool Error { get; }

        int StatusCode { get; }

        string Message { get; }
    }

    public interface KeyedAbstractDatum
    {
        string Uuid { get; }
    }

    public interface ClearService
    {
        ClearStudy FindOrCreateStudy(string key);

        Status SerializeStudy(string key, string path);

        Status DeserializeStudy(string path, out string key);

        TargetZoneId StdTargetZoneId(int book, int chapter, int verse);

        TargetZoneId VariantTargetZoneId(string name);

        Status SetLocalResourceFolder(string path);

        Status DownloadResource(Uri uri);

        Status QueryLocalResources(
            out IEnumerable<LocalResource> resources);

        ManuscriptFactory ManuscriptFactory { get; }

        LemmaService LemmaService { get; }
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
