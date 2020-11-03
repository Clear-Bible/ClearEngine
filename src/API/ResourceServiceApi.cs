using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface IResourceService
    {
        void SetLocalResourceFolder(string path);

        void DownloadResource(Uri uri);

        IEnumerable<LocalResource> QueryLocalResources();

        Segmenter CreateSegmenter(Uri segmenterAlgorithmUri);

        ITreeService GetTreeService(Uri treeResourceUri);

        HashSet<string> GetStringSet(Uri stringSetUri);

        Dictionary<string, string> GetStringsDictionary(
            Uri stringsDictionaryUri);

        Versification GetVersification(Uri versificationUri);
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
