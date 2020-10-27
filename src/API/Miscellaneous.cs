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


    public readonly struct SourceID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));
        public int Subsegment => int.Parse(_tag.Substring(11, 1));

        private readonly string _tag;

        public SourceID(string tag) { _tag = tag; }
    }


    public readonly struct TargetID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));

        private readonly string _tag;

        public TargetID(string tag) { _tag = tag; }
    }


    public readonly struct TargetMorph
    {
        public readonly string Text;

        public TargetMorph(string text) { Text = text; }
    }


    public readonly struct SourceMorph
    {
        public readonly string Text;

        public SourceMorph(string text) { Text = text; }
    }



    public readonly struct Lemma
    {
        public readonly string Text;

        public Lemma(string text) { Text = text; }
    }
}
