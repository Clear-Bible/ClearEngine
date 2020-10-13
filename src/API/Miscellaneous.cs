using System;
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
        QueryLocalResourcesFailed,
        NullOrBlankKey,
        KeyIsNotPresent
    }


    public interface ProgressReport
    {
        string Message { get; }

        float PercentComplete { get; }
    }
}
