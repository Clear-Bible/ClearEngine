using System;


namespace ClearBible.Clear3.API
{
    // Data Model: IDs for Source Segments, Target Words, Chapters, Verses
    //--------------------------------------------------------------------


    /// <summary>
    /// <para>
    /// Identifies a particular segment instance in the source manuscript,
    /// as described by its book, chapter, verse, word, and subsegment numbers
    /// as known to the syntax tree.
    /// </para>
    /// <para>
    /// The SourceID object consists of a struct containing a single string
    /// that encodes the identification in a canonical form.  This string is
    /// available using the AsCanonicalString property.  A new SourceID can
    /// be created from a canonical string if desired.
    /// </para>
    /// <para>
    /// Once created, a SourceID is immutable.  Two SourceID objects are
    /// Equal() if they contain the same canonical string, and will work as
    /// dictionary keys on this basis.
    /// </para>
    /// </summary>
    /// FIXME: Finish using SourceIDs instead of bare strings in the
    /// system at large.
    /// 
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


    /// <summary>
    /// <para>
    /// Identifies a particular translated word instance in the translation,
    /// as described by its book, chapter, verse, and word, numbers
    /// as known to the translation.
    /// </para>
    /// <para>
    /// The TargetID object consists of a struct containing a single string
    /// that encodes the identification in a canonical form.  This string is
    /// available using the AsCanonicalString property.  A new TargetID can
    /// be created from a canonical string if desired.
    /// </para>
    /// <para>
    /// Once created, a TargetID is immutable.  Two TargetID objects are
    /// Equal() if they contain the same canonical string, and will work as
    /// dictionary keys on this basis.
    /// </para>
    /// </summary>
    /// FIXME: Finish using TargetIDs instead of bare strings in the
    /// system at large.
    /// 
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

        public TargetID(
            VerseID verseID,
            int word)
        {
            _tag = $"{verseID.AsCanonicalString}{word:D3}";
        }

        public string AsCanonicalString => _tag;
    }

    /// <summary>
    /// <para>
    /// Identifies a particular segment instance in the translation
    /// as described by its book, chapter, verse, word, and subsegment numbers
    /// after lemmatization of the translation.
    /// </para>
    /// <para>
    /// The TargetLemmaID object consists of a struct containing a single string
    /// that encodes the identification in a canonical form.  This string is
    /// available using the AsCanonicalString property.  A new TargetLemmaID can
    /// be created from a canonical string if desired.
    /// </para>
    /// <para>
    /// Once created, a TargetLemmaID is immutable.  Two TargetLemmaID objects are
    /// Equal() if they contain the same canonical string, and will work as
    /// dictionary keys on this basis.
    /// </para>
    /// <para>
    /// To accomdate highly agglutinative languages, the subsegment has
    /// two digits to allow one word to have more than 9 subsegments.
    /// </para>
    /// </summary>
    /// FIXME: Finish using TargetLemmaIDs instead of bare strings in the
    /// system at large.
    /// 
    public readonly struct TargetLemmaID
    {
        public int Book => int.Parse(_tag.Substring(0, 2));
        public int Chapter => int.Parse(_tag.Substring(2, 3));
        public int Verse => int.Parse(_tag.Substring(5, 3));
        public int Word => int.Parse(_tag.Substring(8, 3));
        public int Subsegment => int.Parse(_tag.Substring(11, 2));

        public ChapterID ChapterID => new ChapterID(_tag.Substring(0, 5));
        public VerseID VerseID => new VerseID(_tag.Substring(0, 8));

        private readonly string _tag;

        public TargetLemmaID(string tag) { _tag = tag; }

        public TargetLemmaID(
            int book, int chapter, int verse, int word, int subsegment)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{word:D3}{subsegment:D1}";
        }

        public string AsCanonicalString => _tag;
    }

    /// <summary>
    /// <para>
    /// Identifies a chapter as described by its book and chapter numbers.
    /// </para>
    /// <para>
    /// The ChapterID object consists of a struct containing a single string
    /// that encodes the identification in a canonical form.  This string is
    /// available using the AsCanonicalString property.  A new ChapterID can
    /// be created from a canonical string if desired.
    /// </para>
    /// <para>
    /// Once created, a ChapterID is immutable.  Two ChapterID objects are
    /// Equal() if they contain the same canonical string, and will work as
    /// dictionary keys on this basis.
    /// </para>
    /// </summary>
    /// 
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

        public string AsCanonicalString => _tag;

        /// <summary>
        /// A special value of ChapterID that has book 0 and chapter 0,
        /// and that means "no chapter".
        /// </summary>
        /// 
        public static ChapterID None => new ChapterID("00000");
    }


    /// <summary>
    /// <para>
    /// Identifies a particular verse as described by its book, chapter,
    /// and verse numbers.
    /// </para>
    /// <para>
    /// The VerseID object consists of a struct containing a single string
    /// that encodes the identification in a canonical form.  This string is
    /// available using the AsCanonicalString property.  A new VerseID can
    /// be created from a canonical string if desired.
    /// </para>
    /// <para>
    /// Once created, a VerseID is immutable.  Two VerseID objects are
    /// Equal() if they contain the same canonical string, and will work as
    /// dictionary keys on this basis.
    /// </para>
    /// </summary>
    /// 
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
