using System.Reflection;


namespace ClearBible.Engine.Persistence
{
    /*
    public static class _Mappings
    {
        public static Dictionary<string, ParatextBook> ManuscriptFileBookToSILBookPrefixes = new()
        {
            { "gn", new ParatextBook("GEN", "Gen", "Genesis", "Genesis", "01") },
            { "ex", new ParatextBook("EXO", "Exod", "Exodus", "Exodus", "02") },
            { "lv", new ParatextBook("LEV", "Lev", "Leviticus", "Leviticus", "03") },
            { "nu", new ParatextBook("NUM", "Num", "Numbers", "Numbers", "04") },
            { "dt", new ParatextBook("DEU", "Deut", "Deuteronomy", "Deuteronomy", "05") },
            { "js", new ParatextBook("JOS", "Josh", "Joshua", "Joshua", "06") },
            { "ju", new ParatextBook("JDG", "Judg", "Judges", "Judges", "07") },
            { "ru", new ParatextBook("RUT", "Ruth", "Ruth", "Ruth", "08") },
            { "1s", new ParatextBook("1SA", "1 Sam", "1 Samuel", "1 Samuel", "09") },
            { "2s", new ParatextBook("2SA", "2 Sam", "2 Samuel", "2 Samuel", "10") },
            { "1k", new ParatextBook("1KI", "1 Kgs", "1 Kings", "1 Kings", "11") },
            { "2k", new ParatextBook("2KI", "2 Kgs", "2 Kings", "2 Kings", "12") },
            { "1c", new ParatextBook("1CH", "1 Chr", "1 Chronicles", "1 Chronicles", "13") },
            { "2c", new ParatextBook("2CH", "2 Chr", "2 Chronicles", "2 Chronicles", "14") },
            { "er", new ParatextBook("EZR", "Ezra", "Ezra", "Ezra", "15") },
            { "ne", new ParatextBook("NEH", "Neh", "Nehemiah", "Nehemiah", "16") },
            { "es", new ParatextBook("EST", "Esth", "Esther", "Esther", "17") },
            { "jb", new ParatextBook("JOB", "Job", "Job", "Job", "18") },
            { "ps", new ParatextBook("PSA", "Ps(s)}", "Psalms", "Psalms", "19") },
            { "pr", new ParatextBook("PRO", "Prov", "Proverbs", "Proverbs", "20") },
            { "ec", new ParatextBook("ECC", "Eccl", "Ecclesiastes", "Ecclesiastes", "21") },
            { "ca", new ParatextBook("SNG", "Song", "Song of Songs", "The Song of Songs", "22") }, //FIXME - is this right?
            { "is", new ParatextBook("ISA", "Isa", "Isaiah", "Isaiah", "23") },
            { "je", new ParatextBook("JER", "Jer", "Jeremiah", "Jeremiah", "24") },
            { "lm", new ParatextBook("LAM", "Lam", "Lamentations", "Lamentations", "25") },
            { "ek", new ParatextBook("EZK", "Ezek", "Ezekiel", "Ezekiel", "26") },
            { "da", new ParatextBook("DAN", "Dan", "Daniel", "Daniel", "27") },
            { "ho", new ParatextBook("HOS", "Hos", "Hosea", "Hosea", "28") },
            { "jl", new ParatextBook("JOL", "Joel", "Joel", "Joel", "29") },
            { "am", new ParatextBook("AMO", "Amos", "Amos", "Amos", "30") },
            { "ob", new ParatextBook("OBA", "Obad", "Obadiah", "Obadiah", "31") },
            { "jn", new ParatextBook("JON", "Jonah", "Jonah", "Jonah", "32") },
            { "mi", new ParatextBook("MIC", "Micah", "Mic", "Micah", "33") },
            { "na", new ParatextBook("NAM", "Nah", "Nahum", "Nahum", "34") },
            { "hb", new ParatextBook("HAB", "Hab", "Habakkuk", "Habakkuk", "35") },
            { "zp", new ParatextBook("ZEP", "Zeph", "Zephaniah", "Zephaniah", "36") },
            { "hg", new ParatextBook("HAG", "Hag", "Haggai", "Haggai", "37") },
            { "zc", new ParatextBook("ZEC", "Zech", "Zechariah", "Zechariah", "38") },
            { "ma", new ParatextBook("MAL", "Mal", "Malachi", "Malachi", "39") },
            { "Mat", new ParatextBook("MAT", "Matt", "Matthew", "Matthew", "41") },
            { "Mrk", new ParatextBook("MRK", "Mark", "Mark", "Mark", "42") },
            { "Luk", new ParatextBook("LUK", "Luke", "Luke", "Luke", "43") },
            { "Jhn", new ParatextBook("JHN", "John", "John", "John", "44") },
            { "Act", new ParatextBook("ACT", "Acts", "Acts", "Acts", "45") },
            { "Rom", new ParatextBook("ROM", "Rom", "Romans", "Romans", "46") },
            { "1Co", new ParatextBook("1CO", "1 Cor", "1 Corinthians", "1 Corinthians", "47") },
            { "2Co", new ParatextBook("2CO", "2 Cor", "2 Corinthians", "2 Corinthians", "48") },
            { "Gal", new ParatextBook("GAL", "Galatians", "Galatians", "Galatians", "49") },
            { "Eph", new ParatextBook("EPH", "Eph", "Ephesians", "Ephesians", "50") },
            { "Php", new ParatextBook("PHP", "Phil", "Philippians", "Philippians", "51") },
            { "Col", new ParatextBook("COL", "Col", "Colossians", "Colossians", "52") },
            { "1Th", new ParatextBook("1TH", "1 Thess", "1 Thessalonians", "1 Thessalonians", "53") },
            { "2Th", new ParatextBook("2TH", "2 Thess", "2 Thessalonians", "2 Thessalonians", "54") },
            { "1Tm", new ParatextBook("1TI", "1 Tim", "1 Timothy", "1 Timothy", "55") },
            { "2Tm", new ParatextBook("2TI", "2 Tim", "2 Timothy", "2 Timothy", "56") },
            { "Tit", new ParatextBook("TIT", "Titus", "Titus", "Titus", "57") },
            { "Phm", new ParatextBook("PHM", "Phlm", "Philemon", "Philemon", "58") },
            { "Heb", new ParatextBook("HEB", "Heb", "Hebrews", "Hebrews", "59") },
            { "Jms", new ParatextBook("JAS", "Jas", "James", "James", "60") },
            { "1Pe", new ParatextBook("1PE", "1 Pet", "1 Peter", "1 Peter", "61") },
            { "2Pe", new ParatextBook("2PE", "2 Pet", "2 Peter", "2 Peter", "62") },
            { "1Jn", new ParatextBook("1JN", "1 John", "1 John", "1 John", "63") },
            { "2Jn", new ParatextBook("2JN", "2 John", "2 John", "2 John", "64") },
            { "3Jn", new ParatextBook("3JN", "3 John", "3 John", "3 John", "65") },
            { "Jud", new ParatextBook("JUD", "Jude", "Jude", "Jude", "66") },
            { "Rev", new ParatextBook("REV", "Rev", "Revelation", "Revelation", "67") },
        };

        public record ParatextBook(string code, string abbr, string shortName, string longName, string id);
    }
    */

    public class FileGetBookIdsNonStaticAwesomeness
    {
        public record BookId(string silCannonBookAbbrev, string silCannonBookNum, string clearTreeBookAbbrev,
            string clearTreeBookNum);

        private string _fileName = "books.csv";
        private List<BookId> _bookIds = new();

        public List<BookId> BookIds
        {
            get
            {
                _bookIds.Clear();
                using (var reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                                     Path.DirectorySeparatorChar + _fileName))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int commentLocation = line?.IndexOf('#') ?? -1;
                        if (commentLocation != -1)
                        {
                            line = line?.Substring(0, commentLocation) ?? "";
                        }

                        var pieces = line?.Split(',') ?? new string[0];
                        if (pieces.Length >= 4)
                        {
                            _bookIds.Add(new BookId(pieces[0], pieces[1], pieces[2], pieces[3]));
                        }
                    }
                }

                return _bookIds;
            }
        }
    }
}