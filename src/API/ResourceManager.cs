using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface ResourceManager
    {
        void SetLocalResourceFolder(string path);
        // can throw ClearException

        void DownloadResource(Uri uri);
        // can throw ClearException

        IEnumerable<LocalResource> QueryLocalResources();
        // can throw ClearException

        TreeService GetTreeService(Uri treeResourceUri);
        // can throw ClearException

        HashSet<string> GetStringSet(Uri stringSetUri);
        // can throw ClearException

        Versification GetVersification(Uri versificationUri);
        // can throw ClearException
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
