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


    public readonly struct SourceID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));
        public int Subsegment => int.Parse(_tag.Substring(11, 1));

        public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));
        public VerseID VerseID => new VerseID(_tag.Substring(0, 8));

        private readonly string _tag;

        public SourceID(string tag) { _tag = tag; }

        public SourceID(
            int book, int chapter, int verse, int word, int subsegment)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{word:D3}{subsegment:D1}";
        }

        // FIXME
        public string Legacy => _tag;
    }


    public readonly struct TargetID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));

        public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));
        public VerseID VerseID => new VerseID(_tag.Substring(0, 8));

        private readonly string _tag;

        public TargetID(string tag) { _tag = tag; }

        public TargetID(
            int book, int chapter, int verse, int word)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{word:D3}";
        }

        // FIXME
        public string Legacy => _tag;
    }


    public readonly struct ChapterID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));

        private readonly string _tag;

        public ChapterID(string tag) { _tag = tag; }

        public ChapterID(
            int book, int chapter)
        {
            _tag = $"{book:D2}{chapter:D3}";
        }

        public static ChapterID None => new ChapterID("00000");
    }


    public readonly struct VerseID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));

        public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));

        private readonly string _tag;

        public VerseID(string tag) { _tag = tag; }

        public VerseID(
            int book, int chapter, int verse)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}";
        }
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


    public readonly struct Score
    {
        public readonly double Double;

        public Score(double d) { Double = d; }
    }

    // FIXME
    public class Gloss
    {
        public string Gloss1;
        public string Gloss2;
    }
}
