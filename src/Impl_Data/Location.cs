using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Data
{
    public class LocationService : ILocationService
    {
        public ISourceID ISourceID(string tag) =>
            new SourceID_Old(tag);

        public ISourceID ISourceID(
            int book, int chapter, int verse, int word, int subSegment) =>
                throw new NotImplementedException();

        public ITargetID ITargetID(string tag) =>
            new TargetID_Old(tag);

        public ITargetID ITargetID(
            int book, int chapter, int verse, int word) =>
                throw new NotImplementedException();

        public IVerseID IVerseID(string tag) =>
            new VerseID_Old(tag);

        public IVerseID IVerseID(int book, int chapter, int verse) =>
            throw new NotImplementedException();

        public IChapterID IChapterID(string tag) =>
            new ChapterID_Old(tag);

        public IChapterID IChapterID(int book, int chapter) =>
            throw new NotImplementedException();

        public IBookID IBookID(string tag) =>
            new BookID_Old(tag);

        public IBookID IBookID(int book) =>
            throw new NotImplementedException();

        internal static string CheckedTag(string s, int n)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("Blank tag is invalid");
            if (s.Length != n)
                throw new ArgumentException($"Tag must have {n} characters.");
            if (s.Any(c => !char.IsDigit(c)))
                throw new ArgumentException("Tag must be consist of digits");
            return s;
        }
    }


    public readonly struct SourceID_Old : ISourceID,
        IEquatable<ISourceID>, IComparable<ISourceID>
    {
        public string Tag { get; }  // BBCCCVVVWWWS

        public SourceID_Old(string tag)
        {
            Tag = LocationService.CheckedTag(tag, 12);
        }

        public IVerseID Verse => new VerseID_Old(Tag.Substring(0, 8));

        public bool Equals(ISourceID x) => Tag.Equals(x.Tag);

        public int CompareTo(ISourceID x) => Tag.CompareTo(x.Tag);
    }


    public struct TargetID_Old : ITargetID,
        IEquatable<ITargetID>, IComparable<ITargetID>
    {
        public string Tag { get; }  // BBCCCVVVWWW

        public TargetID_Old(string tag)
        {
            Tag = LocationService.CheckedTag(tag, 11);
        }

        public IVerseID Verse => new VerseID_Old(Tag.Substring(0, 8));

        public bool Equals(ITargetID x) => Tag.Equals(x.Tag);

        public int CompareTo(ITargetID x) => Tag.CompareTo(x.Tag);
    }


    public struct VerseID_Old : IVerseID,
        IEquatable<IVerseID>, IComparable<IVerseID>
    {
        public string Tag { get; }  // BBCCCVVV

        public VerseID_Old(string tag)
        {
            Tag = LocationService.CheckedTag(tag, 8);
        }

        public IChapterID Chapter => new ChapterID_Old(Tag.Substring(0, 5));

        public bool Equals(IVerseID x) => Tag.Equals(x.Tag);

        public int CompareTo(IVerseID x) => Tag.CompareTo(x.Tag);
    }


    public struct ChapterID_Old : IChapterID,
        IEquatable<IChapterID>, IComparable<IChapterID>
    {
        public string Tag { get; } // BBCCC

        public ChapterID_Old(string tag)
        {
            Tag = LocationService.CheckedTag(tag, 5);
        }

        public IBookID Book => new BookID_Old(Tag.Substring(0, 2));

        public bool Equals(IChapterID x) => Tag.Equals(x.Tag);

        public int CompareTo(IChapterID x) => Tag.CompareTo(x.Tag);
    }


    public struct BookID_Old : IBookID,
        IEquatable<IBookID>, IComparable<IBookID>
    {
        public string Tag { get; }  // BB

        public BookID_Old(string tag)
        {
            Tag = LocationService.CheckedTag(tag, 2);
        }

        public bool Equals(IBookID x) => Tag.Equals(x.Tag);

        public int CompareTo(IBookID x) => Tag.CompareTo(x.Tag);
    }
}


