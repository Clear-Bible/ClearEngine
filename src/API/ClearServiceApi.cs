using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public class ClearException : Exception
    {
        public ClearException(
            string message,
            StatusCode statusCode,
            Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public StatusCode StatusCode { get; private set; }
    }

    public enum StatusCode
    {
        OK,
        SetLocalResourceFolderFailed,
        QueryLocalResourcesFailed
    }

    public interface Clear30ServiceAPI
    {
        ResourceManager ResourceManager { get; }

        Segmenter CreateSegmenter(Uri segmenterAlgorithmUri);
        // can throw ClearException

        Corpus CreateEmptyCorpus();

        ClearStudyManager ClearStudyManager { get; }

        ZoneService ZoneService { get; }       

        // ManuscriptFactory ManuscriptFactory { get; }

        LemmaService LemmaService { get; }
    }

    public interface ResourceManager
    {
        void SetLocalResourceFolder(string path);
        // can throw ClearException

        void DownloadResource(Uri uri);
        // can throw ClearException

        IEnumerable<LocalResource> QueryLocalResources();
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
