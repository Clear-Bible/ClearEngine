using System;
using System.Collections.Generic;
using System.Linq;


namespace ClearBible.Clear3.API
{ 
    public interface ILocationService
    {
        ISourceID ISourceID(string tag);

        ISourceID ISourceID(
            int book, int chapter, int verse, int word, int subSegment);

        ITargetID ITargetID(string tag);

        ITargetID ITargetID(
            int book, int chapter, int verse, int word);

        IVerseID IVerseID(string tag);

        IVerseID IVerseID(int book, int chapter, int verse);

        IChapterID IChapterID(string tag);

        IChapterID IChapterID(int book, int chapter);

        IBookID IBookID(string tag);

        IBookID IBookID(int book);
    }


    public interface ISourceID
        : IEquatable<ISourceID>, IComparable<ISourceID>
    {
        /// <summary>
        /// String of digits of the form BBCCCVVVWWWS for book BB,
        /// chapter CCC, verse VVV, word WWW, and subsegment S.
        /// </summary>
        /// 
        string Tag { get; }

        IVerseID Verse { get; }
    }


    public interface ITargetID
        : IEquatable<ITargetID>, IComparable<ITargetID>
    {
        /// <summary>
        /// String of digits of the form BBCCCVVVWWW for book BB,
        /// chapter CCC, verse VVV, word WWW.
        /// </summary>
        /// 
        string Tag { get; }

        IVerseID Verse { get; }
    }


    public interface IVerseID
        : IEquatable<IVerseID>, IComparable<IVerseID>
    {
        /// <summary>
        /// String of digits of the form BBCCCVVV for book BB,
        /// chapter CCC, verse VVV.
        /// </summary>
        /// 
        string Tag { get; }

        IChapterID Chapter { get; }
    }


    public interface IChapterID
        : IEquatable<IChapterID>, IComparable<IChapterID>
    {
        /// <summary>
        /// String of digits of the form BBCCC for book BB and
        /// chapter CCC.
        /// </summary>
        /// 
        string Tag { get; }

        IBookID Book { get; }
    }


    public interface IBookID
        : IEquatable<IBookID>, IComparable<IBookID>
    {
        /// <summary>
        /// String of digits of the form BB.
        /// </summary>
        /// 
        string Tag { get; }
    }
}
