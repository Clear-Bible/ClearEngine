using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// An exception that Clear3 services can throw.
    /// </summary>
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


    /// <summary>
    /// Status codes for use in a ClearException.
    /// </summary>
    /// 
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


    /// <summary>
    /// Progress report to be sent periodically from a Clear job that
    /// is executing concurrently.
    /// </summary>
    /// 
    public interface ProgressReport
    {
        string Message { get; }

        float PercentComplete { get; }
    }


    /// <summary>
    /// The type of a function that attempts to look up a value
    /// for a given key.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the value.
    /// </typeparam>
    /// <param name="key">
    /// The key that is sought.
    /// </param>
    /// <param name="value">
    /// Will be set to the value found for the key, or to the default
    /// value for its type if the key cannot be found.
    /// </param>
    /// <returns>
    /// True if a value for the key was found, and false otherwise.
    /// </returns>
    /// 
    public delegate bool TryGet<TKey, TValue>(
        TKey key,
        out TValue value);
}
