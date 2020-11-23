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

        public ClearException(
            string message,
            StatusCode statusCode)
            : base(message)
        {

        }

        public StatusCode StatusCode { get; private set; }
    }


    public enum StatusCode
    {
        OK,
        InvalidInput,
        ResourceDirectoryDoesNotExist,
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
