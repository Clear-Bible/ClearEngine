﻿using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
	public class Verse : IEquatable<Verse>, IComparable<Verse>
	{
		public Verse(string book, int ChapterNumber, int VerseNumber,IEnumerable<TokenId>? tokenIds)
		{
			Book = book;
			ChapterNum = ChapterNumber;
			VerseNum = VerseNumber;
            TokenIds = tokenIds ?? Enumerable.Empty<TokenId>();
        }

		public Verse(VerseRef verseRef)
		{
			Book = verseRef.Book;
			ChapterNum = verseRef.ChapterNum;
			VerseNum = verseRef.VerseNum;
			TokenIds = Enumerable.Empty<TokenId>();
		}

		public string Book { get; }
		public int ChapterNum { get; }
		public int VerseNum { get; }
        public IEnumerable<TokenId> TokenIds { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Verse verse &&
                   Book == verse.Book &&
                   ChapterNum == verse.ChapterNum &&
                   VerseNum == verse.VerseNum;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Book, ChapterNum, VerseNum);
        }

        public bool Equals(Verse? other)
        {
            return Equals((object?) other);
        }

        public int CompareTo(Verse? other)
        {
            // If other is not a valid object reference, this instance is smaller.
            if (other == null) return -1;

            int result = Book.CompareTo(other.Book);
            if (result == 0)
            {
                result = ChapterNum.CompareTo(other.ChapterNum);
                if (result == 0)
                {
                    result = VerseNum.CompareTo(other.VerseNum);
                }
            }
            return result;
        }
    }
}
