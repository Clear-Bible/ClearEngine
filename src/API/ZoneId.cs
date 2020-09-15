using System;
using System.Text.RegularExpressions;

namespace ClearBible.Clear3.API
{
    public struct ZoneId
    {
        public readonly string Id;

        public readonly bool IsStandard;

        public readonly int Book;

        public readonly int Chapter;

        public readonly int Verse;

        public ZoneId(string id)
        {
            Id = id;
            Match match = matchStdId.Match(id);
            if (match.Success)
            {
                Id = id;
                IsStandard = true;              
                Book = int.Parse(match.Groups[1].ToString());
                Chapter = int.Parse(match.Groups[2].ToString());
                Verse = int.Parse(match.Groups[3].ToString());
            }
            else
            {
                IsStandard = false;
                Book = 0;
                Chapter = 0;
                Verse = 0;
            }
        }

        public ZoneId(int book, int chapter, int verse)
        {
            if (book < 0) throw new ArgumentException($"invalid book {book}");
            if (chapter < 0) throw new ArgumentException($"invalid chapter {chapter}");
            if (verse < 0) throw new ArgumentException($"invalid verse {verse}");
            IsStandard = true;
            Id = $"{book}-{chapter}-{verse}";
            Book = book;
            Chapter = chapter;
            Verse = verse;
        }

        private static Regex matchStdId =
            new Regex(@"(\d+)-(\d+)-(\d+)", RegexOptions.Compiled);
    }


    public struct MssZoneId
    {
        public interface IMss { bool ValidZone(ZoneId zoneId); }

        private ZoneId zoneId;
        public string Id => zoneId.Id;
        public int Book => zoneId.Book;
        public int Chapter => zoneId.Chapter;
        public int Verse => zoneId.Verse;

        public readonly IMss Mss;

        public MssZoneId(IMss mss, ZoneId zoneId)
        {
            if (!zoneId.IsStandard) throw new ArgumentException("nonstandard zone");
            if (!mss.ValidZone(zoneId)) throw new ArgumentException("invalid zone");
            this.zoneId = zoneId;
            Mss = mss;
        }
    }
}
