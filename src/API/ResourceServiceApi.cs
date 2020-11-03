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


    public class LocalResource
    {
        public Uri Id { get; }

        public DateTime DownloadMoment { get; }

        public bool Ok { get; }

        public bool BuiltIn { get; }

        public string Status { get; }

        public string Description { get; }

        public LocalResource(
            Uri id,
            DateTime downloadMoment,
            bool ok,
            bool builtIn,
            string status,
            string description)
        {
            Id = id;
            DownloadMoment = downloadMoment;
            Ok = ok;
            BuiltIn = builtIn;
            Status = status;
            Description = description;
        }
    }
}
