using System;

namespace ClearBible.Clear3.Impl.ResourceService
{
    using System.Collections.Generic;
    using ClearBible.Clear3.API;

    public class ResourceService : IResourceService
    {
        public Segmenter CreateSegmenter(Uri segmenterAlgorithmUri)
        {
            throw new NotImplementedException();
        }

        public void DownloadResource(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetStringsDictionary(Uri stringsDictionaryUri)
        {
            throw new NotImplementedException();
        }

        public HashSet<string> GetStringSet(Uri stringSetUri)
        {
            throw new NotImplementedException();
        }

        public ITreeService GetTreeService(Uri treeResourceUri)
        {
            throw new NotImplementedException();
        }

        public Versification GetVersification(Uri versificationUri)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<LocalResource> QueryLocalResources()
        {
            throw new NotImplementedException();
        }

        public void SetLocalResourceFolder(string path)
        {
            throw new NotImplementedException();
        }
    }
}
