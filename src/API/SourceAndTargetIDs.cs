using System;


namespace ClearBible.Clear3.API
{
    // Source and Target IDs
    // 
    // Ways of identifying locations in source and target corpora
    // by book, chapter, verse, word, and (in case of a source) subsegment.


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

        public string AsCanonicalString => _tag;
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

        public string AsCanonicalString => _tag;
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

        public string AsCanonicalString => _tag;
    }
}
