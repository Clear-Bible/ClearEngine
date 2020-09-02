using System;
using System.Collections;

namespace Utilities
{
	public class BookTables
	{
		public BookTables()
		{
			bookNames = new Hashtable();

			bookNames.Add("1","Genesis");
			bookNames.Add("2","Exodus");
			bookNames.Add("3","Leviticus");
            bookNames.Add("4","Numbers");
			bookNames.Add("5","Deuteronomy");
			bookNames.Add("6","Joshua");
			bookNames.Add("7","Judges");
			bookNames.Add("8","Ruth");
			bookNames.Add("9","1 Samuel");
			bookNames.Add("10","2 Samuel");
			bookNames.Add("11","1 Kings");
			bookNames.Add("12","2 Kings");
			bookNames.Add("13","1 Chronicles");
			bookNames.Add("14","2 Chronicies");
			bookNames.Add("15","Ezra");
			bookNames.Add("16","Nehemiah");
			bookNames.Add("17","Esther");
			bookNames.Add("18","Job");
			bookNames.Add("19","Psalms");
			bookNames.Add("20","Proverbs");
			bookNames.Add("21","Ecclesiastes");
			bookNames.Add("22","Song of Songs");
			bookNames.Add("23","Isaiah");
			bookNames.Add("24","Jeremiah");
			bookNames.Add("25","Lamentations");
			bookNames.Add("26","Ezekiel");
			bookNames.Add("27","Daniel");
			bookNames.Add("28","Hosea");
			bookNames.Add("29","Joel");
			bookNames.Add("30","Amos");
			bookNames.Add("31","Obadiah");
			bookNames.Add("32","Jonah");
			bookNames.Add("33","Micah");
			bookNames.Add("34","Nahum");
			bookNames.Add("35","Habakkuk");
			bookNames.Add("36","Zephaniah");
			bookNames.Add("37","Haggai");
			bookNames.Add("38","Zechariah");
			bookNames.Add("39","Malachi");
			bookNames.Add("40","Matthew");
			bookNames.Add("41","Mark");
			bookNames.Add("42","Luke");
			bookNames.Add("43","John");
			bookNames.Add("44","Acts");
			bookNames.Add("45","Romans");
			bookNames.Add("46","1 Corinthians");
            bookNames.Add("47", "2 Corinthians");
			bookNames.Add("48","Galatians");
			bookNames.Add("49","Ephesians");
			bookNames.Add("50","Philippians");
			bookNames.Add("51","Colossians");
			bookNames.Add("52","1 Thessalonians");
            bookNames.Add("53", "2 Thessalonians");
			bookNames.Add("54","1 Timothy");
            bookNames.Add("55", "2 Timothy");
			bookNames.Add("56","Titus");
			bookNames.Add("57","Philemon");
			bookNames.Add("58","Hebrews");
			bookNames.Add("59","James");
			bookNames.Add("60","1 Peter");
			bookNames.Add("61","2 Peter");
			bookNames.Add("62","1 John");
			bookNames.Add("63","2 John");
			bookNames.Add("64","3 John");
			bookNames.Add("65","Jude");
			bookNames.Add("66","Revelation");

            bookNames2 = new Hashtable();

            bookNames2.Add("1", "gn");
            bookNames2.Add("2", "ex");
            bookNames2.Add("3", "lv");
            bookNames2.Add("4", "nu");
            bookNames2.Add("5", "dt");
            bookNames2.Add("6", "js");
            bookNames2.Add("7", "ju");
            bookNames2.Add("8", "ru");
            bookNames2.Add("9", "1s");
            bookNames2.Add("10", "2s");
            bookNames2.Add("11", "1k");
            bookNames2.Add("12", "2k");
            bookNames2.Add("13", "1c");
            bookNames2.Add("14", "2c");
            bookNames2.Add("15", "Er");
            bookNames2.Add("16", "ne");
            bookNames2.Add("17", "es");
            bookNames2.Add("18", "jb");
            bookNames2.Add("19", "ps");
            bookNames2.Add("20", "pr");
            bookNames2.Add("21", "ec");
            bookNames2.Add("22", "ca");
            bookNames2.Add("23", "is");
            bookNames2.Add("24", "je");
            bookNames2.Add("25", "lm");
            bookNames2.Add("26", "ek");
            bookNames2.Add("27", "da");
            bookNames2.Add("28", "ho");
            bookNames2.Add("29", "jl");
            bookNames2.Add("30", "am");
            bookNames2.Add("31", "ob");
            bookNames2.Add("32", "jn");
            bookNames2.Add("33", "mi");
            bookNames2.Add("34", "na");
            bookNames2.Add("35", "hb");
            bookNames2.Add("36", "zp");
            bookNames2.Add("37", "hg");
            bookNames2.Add("38", "zc");
            bookNames2.Add("39", "ma");
            bookNames2.Add("40", "Mat");
            bookNames2.Add("41", "Mrk");
            bookNames2.Add("42", "Luk");
            bookNames2.Add("43", "Jhn");
            bookNames2.Add("44", "Act");
            bookNames2.Add("45", "Rom");
            bookNames2.Add("46", "1Co");
            bookNames2.Add("47", "2Co");
            bookNames2.Add("48", "Gal");
            bookNames2.Add("49", "Eph");
            bookNames2.Add("50", "Php");
            bookNames2.Add("51", "Col");
            bookNames2.Add("52", "1Th");
            bookNames2.Add("53", "2Th");
            bookNames2.Add("54", "1Tm");
            bookNames2.Add("55", "2Tm");
            bookNames2.Add("56", "Tit");
            bookNames2.Add("57", "Phm");
            bookNames2.Add("58", "Heb");
            bookNames2.Add("59", "Jms");
            bookNames2.Add("60", "1Pe");
            bookNames2.Add("61", "2Pe");
            bookNames2.Add("62", "1Jn");
            bookNames2.Add("63", "2Jn");
            bookNames2.Add("64", "3Jn");
            bookNames2.Add("65", "Jud");
            bookNames2.Add("66", "Rev");
		}
	
		public static Hashtable LoadBookIds()
		{ 
			Hashtable bookIds = new Hashtable();

			bookIds.Add("gn","01");
			bookIds.Add("ex","02");
			bookIds.Add("lv","03");
			bookIds.Add("nu","04");
			bookIds.Add("dt","05");
			bookIds.Add("js","06");
			bookIds.Add("ju","07");
			bookIds.Add("ru","08");
			bookIds.Add("1s","09");
			bookIds.Add("2s","10");
			bookIds.Add("1k","11");
			bookIds.Add("2k","12");
			bookIds.Add("1c","13");
			bookIds.Add("2c","14");
			bookIds.Add("er","15");
			bookIds.Add("ne","16");
			bookIds.Add("es","17");
			bookIds.Add("jb","18");
			bookIds.Add("ps","19");
			bookIds.Add("pr","20");
			bookIds.Add("ec","21");
			bookIds.Add("ca","22");
			bookIds.Add("is","23");
			bookIds.Add("je","24");
			bookIds.Add("lm","25");
			bookIds.Add("ek","26");
			bookIds.Add("da","27");
			bookIds.Add("ho","28");
			bookIds.Add("jl","29");
			bookIds.Add("am","30");
			bookIds.Add("ob","31");
			bookIds.Add("jn","32");
			bookIds.Add("mi","33");
			bookIds.Add("na","34");
			bookIds.Add("hb","35");
			bookIds.Add("zp","36");
			bookIds.Add("hg","37");
			bookIds.Add("zc","38");
			bookIds.Add("ma","39");
			bookIds.Add("mat","40");
			bookIds.Add("mrk","41");
			bookIds.Add("luk","42");
			bookIds.Add("jhn","43");
			bookIds.Add("act","44");
			bookIds.Add("rom","45");
			bookIds.Add("1co","46");
			bookIds.Add("2co","47");
			bookIds.Add("gal","48");
			bookIds.Add("eph","49");
			bookIds.Add("php","50");
			bookIds.Add("col","51");
			bookIds.Add("1th","52");
			bookIds.Add("2th","53");
			bookIds.Add("1tm","54");
			bookIds.Add("2tm","55");
			bookIds.Add("tit","56");
			bookIds.Add("phm","57");
			bookIds.Add("heb","58");
			bookIds.Add("jms","59");
			bookIds.Add("1pe","60");
			bookIds.Add("2pe","61");
			bookIds.Add("1jn","62");
			bookIds.Add("2jn","63");
			bookIds.Add("3jn","64");
			bookIds.Add("jud","65");
			bookIds.Add("rev","66");

			return bookIds;
		}

        public static Hashtable LoadBookIds2()
        {
            Hashtable bookIds = new Hashtable();

            bookIds.Add("Genesis", "01");
            bookIds.Add("Exodus", "02");
            bookIds.Add("Leviticus", "03");
            bookIds.Add("Numbers", "04");
            bookIds.Add("Deuteronomy", "05");
            bookIds.Add("Joshua", "06");
            bookIds.Add("Judges", "07");
            bookIds.Add("Ruth", "08");
            bookIds.Add("1Samuel", "09");
            bookIds.Add("2Samuel", "10");
            bookIds.Add("1Kings", "11");
            bookIds.Add("2Kings", "12");
            bookIds.Add("1Chronicles", "13");
            bookIds.Add("2Chronicles", "14");
            bookIds.Add("Ezra", "15");
            bookIds.Add("Nehemiah", "16");
            bookIds.Add("Esther", "17");
            bookIds.Add("Job", "18");
            bookIds.Add("Psalm", "19");
            bookIds.Add("Proverbs", "20");
            bookIds.Add("Ecclesiastes", "21");
            bookIds.Add("SongofSolomon", "22");
            bookIds.Add("Songs", "22");
            bookIds.Add("Isaiah", "23");
            bookIds.Add("Jeremiah", "24");
            bookIds.Add("Lamentations", "25");
            bookIds.Add("Ezekiel", "26");
            bookIds.Add("Daniel", "27");
            bookIds.Add("Hosea", "28");
            bookIds.Add("Joel", "29");
            bookIds.Add("Amos", "30");
            bookIds.Add("Obadiah", "31");
            bookIds.Add("Jonah", "32");
            bookIds.Add("Micah", "33");
            bookIds.Add("Nahum", "34");
            bookIds.Add("Habakkuk", "35");
            bookIds.Add("Zephaniah", "36");
            bookIds.Add("Haggai", "37");
            bookIds.Add("Zechariah", "38");
            bookIds.Add("Malachi", "39");
            bookIds.Add("Matthew", "40");
            bookIds.Add("Mark", "41");
            bookIds.Add("Luke", "42");
            bookIds.Add("John", "43");
            bookIds.Add("Acts", "44");
            bookIds.Add("Romans", "45");
            bookIds.Add("1 Corinthians", "46");
            bookIds.Add("2 Corinthians", "47");
            bookIds.Add("Galatians", "48");
            bookIds.Add("Ephesians", "49");
            bookIds.Add("Philippians", "50");
            bookIds.Add("Colossians", "51");
            bookIds.Add("1 Thessalonians", "52");
            bookIds.Add("2 Thessalonians", "53");
            bookIds.Add("1 Timothy", "54");
            bookIds.Add("2 Timothy", "55");
            bookIds.Add("Titus", "56");
            bookIds.Add("Philemon", "57");
            bookIds.Add("Hebrews", "58");
            bookIds.Add("James", "59");
            bookIds.Add("1 Peter", "60");
            bookIds.Add("2 Peter", "61");
            bookIds.Add("1 John", "62");
            bookIds.Add("2 John", "63");
            bookIds.Add("3 John", "64");
            bookIds.Add("Jude", "65");
            bookIds.Add("Revelation", "66");

            return bookIds;
        }

        public static Hashtable LoadBookIds3()
        {
            Hashtable bookIds = new Hashtable();

            bookIds.Add("Gen", "01");
            bookIds.Add("Exod", "02");
            bookIds.Add("Lev", "03");
            bookIds.Add("Num", "04");
            bookIds.Add("Deut", "05");
            bookIds.Add("Josh", "06");
            bookIds.Add("Judg", "07");
            bookIds.Add("Ruth", "08");
            bookIds.Add("1Sam", "09");
            bookIds.Add("2Sam", "10");
            bookIds.Add("1Kgs", "11");
            bookIds.Add("2Kgs", "12");
            bookIds.Add("1Chr", "13");
            bookIds.Add("2Chr", "14");
            bookIds.Add("Ezra", "15");
            bookIds.Add("Neh", "16");
            bookIds.Add("Esth", "17");
            bookIds.Add("Job", "18");
            bookIds.Add("Ps", "19");
            bookIds.Add("Prov", "20");
            bookIds.Add("Eccl", "21");
            bookIds.Add("Song", "22");
            bookIds.Add("Isa", "23");
            bookIds.Add("Jer", "24");
            bookIds.Add("Lam", "25");
            bookIds.Add("Ezek", "26");
            bookIds.Add("Dan", "27");
            bookIds.Add("Hos", "28");
            bookIds.Add("Joel", "29");
            bookIds.Add("Amos", "30");
            bookIds.Add("Obad", "31");
            bookIds.Add("Jonah", "32");
            bookIds.Add("Mic", "33");
            bookIds.Add("Nah", "34");
            bookIds.Add("Hab", "35");
            bookIds.Add("Zeph", "36");
            bookIds.Add("Hag", "37");
            bookIds.Add("Zech", "38");
            bookIds.Add("Mal", "39");

            return bookIds;
        }

        public static Hashtable LoadBookIds4()
        {
            Hashtable bookIds = new Hashtable();

            bookIds.Add("Matt", "40");
            bookIds.Add("Mark", "41");
            bookIds.Add("Luke", "42");
            bookIds.Add("John", "43");
            bookIds.Add("Acts", "44");
            bookIds.Add("Rom", "45");
            bookIds.Add("1Cor", "46");
            bookIds.Add("2Cor", "47");
            bookIds.Add("Gal", "48");
            bookIds.Add("Eph", "49");
            bookIds.Add("Phil", "50");
            bookIds.Add("Col", "51");
            bookIds.Add("1Thess", "52");
            bookIds.Add("2Thess", "53");
            bookIds.Add("1Tim", "54");
            bookIds.Add("2Tim", "55");
            bookIds.Add("Titus", "56");
            bookIds.Add("Phlm", "57");
            bookIds.Add("Heb", "58");
            bookIds.Add("Jas", "59");
            bookIds.Add("1Pet", "60");
            bookIds.Add("2Pet", "61");
            bookIds.Add("1John", "62");
            bookIds.Add("2John", "63");
            bookIds.Add("3John", "64");
            bookIds.Add("Jude", "65");
            bookIds.Add("Rev", "66");

            return bookIds;
        }

        public static Hashtable LoadBookIds5()
        {
            Hashtable bookIds = new Hashtable();

            bookIds.Add("GEN", "01");
            bookIds.Add("EXO", "02");
            bookIds.Add("LEV", "03");
            bookIds.Add("NUM", "04");
            bookIds.Add("DEU", "05");
            bookIds.Add("JOS", "06");
            bookIds.Add("JDG", "07");
            bookIds.Add("RUT", "08");
            bookIds.Add("1SA", "09");
            bookIds.Add("2SA", "10");
            bookIds.Add("1KI", "11");
            bookIds.Add("2KI", "12");
            bookIds.Add("1CH", "13");
            bookIds.Add("2CH", "14");
            bookIds.Add("EZR", "15");
            bookIds.Add("NEH", "16");
            bookIds.Add("EST", "17");
            bookIds.Add("JOB", "18");
            bookIds.Add("PSA", "19");
            bookIds.Add("PRO", "20");
            bookIds.Add("ECC", "21");
            bookIds.Add("SNG", "22");
            bookIds.Add("ISA", "23");
            bookIds.Add("JER", "24");
            bookIds.Add("LAM", "25");
            bookIds.Add("EZK", "26");
            bookIds.Add("DAN", "27");
            bookIds.Add("HOS", "28");
            bookIds.Add("JOL", "29");
            bookIds.Add("AMO", "30");
            bookIds.Add("OBA", "31");
            bookIds.Add("JON", "32");
            bookIds.Add("MIC", "33");
            bookIds.Add("NAM", "34");
            bookIds.Add("HAB", "35");
            bookIds.Add("ZEP", "36");
            bookIds.Add("HAG", "37");
            bookIds.Add("ZEC", "38");
            bookIds.Add("MAL", "39");
            bookIds.Add("MAT", "40");
            bookIds.Add("MRK", "41");
            bookIds.Add("LUK", "42");
            bookIds.Add("JHN", "43");
            bookIds.Add("ACT", "44");
            bookIds.Add("ROM", "45");
            bookIds.Add("1CO", "46");
            bookIds.Add("2CO", "47");
            bookIds.Add("GAL", "48");
            bookIds.Add("EPH", "49");
            bookIds.Add("PHP", "50");
            bookIds.Add("COL", "51");
            bookIds.Add("1TH", "52");
            bookIds.Add("2TH", "53");
            bookIds.Add("1TI", "54");
            bookIds.Add("2TI", "55");
            bookIds.Add("TIT", "56");
            bookIds.Add("PHM", "57");
            bookIds.Add("HEB", "58");
            bookIds.Add("JAS", "59");
            bookIds.Add("1PE", "60");
            bookIds.Add("2PE", "61");
            bookIds.Add("1JN", "62");
            bookIds.Add("2JN", "63");
            bookIds.Add("3JN", "64");
            bookIds.Add("JUD", "65");
            bookIds.Add("REV", "66");

            return bookIds;
        }

		public string BeautifyVerseId( string vid )
		{
			string verseId = string.Empty;

			string book = (string) bookNames[Int32.Parse(vid.Substring(0, 2)).ToString()];
			string chapter = Int32.Parse(vid.Substring(2, 3)).ToString();
			string verse = Int32.Parse(vid.Substring(5)).ToString();

			verseId = book + " " + chapter + ":" + verse;

			return verseId;
		}

        public string BeautifyVerseId2(string vid)
        {
            string verseId = string.Empty;

            string book = (string)bookNames2[Int32.Parse(vid.Substring(0, 2)).ToString()];
            string chapter = Int32.Parse(vid.Substring(2, 3)).ToString();
            string verse = Int32.Parse(vid.Substring(5)).ToString();

            verseId = book + " " + chapter + ":" + verse;

            return verseId;
        }

		private Hashtable bookNames;
        private Hashtable bookNames2;

        public static string VerseID2VerseName(string vid)
        {
            string verseName = string.Empty;

            Hashtable bookNames = LoadBookNames();

            string book = (string)bookNames[Int32.Parse(vid.Substring(0, 2)).ToString()];
            string chapter = Int32.Parse(vid.Substring(2, 3)).ToString();
            string verse = Int32.Parse(vid.Substring(5)).ToString();

            verseName = book + " " + chapter + ":" + verse;

            return verseName;
        }

        public static string VerseID2VerseName2(string vid)
        {
            string verseName = string.Empty;

            Hashtable bookNames = LoadBookNames2();

            string book = (string)bookNames[Int32.Parse(vid.Substring(0, 2)).ToString()];
            string chapter = Int32.Parse(vid.Substring(2, 3)).ToString();
            string verse = Int32.Parse(vid.Substring(5)).ToString();

            verseName = book + chapter + ":" + verse;

            return verseName;
        }

        public static string VerseName2VerseID(string verseName, string otNt)
        {
            Hashtable bookIds = LoadBookIds();

            verseName = verseName.ToLower();

            string book = string.Empty;
            string chapter = string.Empty;

            if (otNt == "ot")
            {
                book = verseName.Substring(0, 2);
                chapter = verseName.Substring(2, verseName.IndexOf(":")-2);
            }
            else
            {
                book = verseName.Substring(0, 3);
                chapter = verseName.Substring(3, verseName.IndexOf(":")-3);
            }

            string bookId = (string)bookIds[book];
            string chapterId = Utils.Pad3(chapter);
            string verseId = Utils.Pad3(verseName.Substring(verseName.IndexOf(":") + 1));

            return bookId + chapterId + verseId;
        }

        public static Hashtable LoadBookNames2()
        {
            Hashtable bookNames2 = new Hashtable();

            bookNames2.Add("1", "gn");
            bookNames2.Add("2", "ex");
            bookNames2.Add("3", "lv");
            bookNames2.Add("4", "nu");
            bookNames2.Add("5", "dt");
            bookNames2.Add("6", "js");
            bookNames2.Add("7", "ju");
            bookNames2.Add("8", "ru");
            bookNames2.Add("9", "1s");
            bookNames2.Add("10", "2s");
            bookNames2.Add("11", "1k");
            bookNames2.Add("12", "2k");
            bookNames2.Add("13", "1c");
            bookNames2.Add("14", "2c");
            bookNames2.Add("15", "er");
            bookNames2.Add("16", "ne");
            bookNames2.Add("17", "es");
            bookNames2.Add("18", "jb");
            bookNames2.Add("19", "ps");
            bookNames2.Add("20", "pr");
            bookNames2.Add("21", "ec");
            bookNames2.Add("22", "ca");
            bookNames2.Add("23", "is");
            bookNames2.Add("24", "je");
            bookNames2.Add("25", "lm");
            bookNames2.Add("26", "ek");
            bookNames2.Add("27", "da");
            bookNames2.Add("28", "ho");
            bookNames2.Add("29", "jl");
            bookNames2.Add("30", "am");
            bookNames2.Add("31", "ob");
            bookNames2.Add("32", "jn");
            bookNames2.Add("33", "mi");
            bookNames2.Add("34", "na");
            bookNames2.Add("35", "hb");
            bookNames2.Add("36", "zp");
            bookNames2.Add("37", "hg");
            bookNames2.Add("38", "zc");
            bookNames2.Add("39", "ma");
            bookNames2.Add("40", "Mat");
            bookNames2.Add("41", "Mrk");
            bookNames2.Add("42", "Luk");
            bookNames2.Add("43", "Jhn");
            bookNames2.Add("44", "Act");
            bookNames2.Add("45", "Rom");
            bookNames2.Add("46", "1Co");
            bookNames2.Add("47", "2Co");
            bookNames2.Add("48", "Gal");
            bookNames2.Add("49", "Eph");
            bookNames2.Add("50", "Php");
            bookNames2.Add("51", "Col");
            bookNames2.Add("52", "1Th");
            bookNames2.Add("53", "2Th");
            bookNames2.Add("54", "1Tm");
            bookNames2.Add("55", "2Tm");
            bookNames2.Add("56", "Tit");
            bookNames2.Add("57", "Phm");
            bookNames2.Add("58", "Heb");
            bookNames2.Add("59", "Jms");
            bookNames2.Add("60", "1Pe");
            bookNames2.Add("61", "2Pe");
            bookNames2.Add("62", "1Jn");
            bookNames2.Add("63", "2Jn");
            bookNames2.Add("64", "3Jn");
            bookNames2.Add("65", "Jud");
            bookNames2.Add("66", "Rev");

            return bookNames2;
        }

        public static Hashtable LoadBookNames3()
        {
            Hashtable bookNames2 = new Hashtable();

            bookNames2.Add("01", "gn");
            bookNames2.Add("02", "ex");
            bookNames2.Add("03", "lv");
            bookNames2.Add("04", "nu");
            bookNames2.Add("05", "dt");
            bookNames2.Add("06", "js");
            bookNames2.Add("07", "ju");
            bookNames2.Add("08", "ru");
            bookNames2.Add("09", "1s");
            bookNames2.Add("10", "2s");
            bookNames2.Add("11", "1k");
            bookNames2.Add("12", "2k");
            bookNames2.Add("13", "1c");
            bookNames2.Add("14", "2c");
            bookNames2.Add("15", "er");
            bookNames2.Add("16", "ne");
            bookNames2.Add("17", "es");
            bookNames2.Add("18", "jb");
            bookNames2.Add("19", "ps");
            bookNames2.Add("20", "pr");
            bookNames2.Add("21", "ec");
            bookNames2.Add("22", "ca");
            bookNames2.Add("23", "is");
            bookNames2.Add("24", "je");
            bookNames2.Add("25", "lm");
            bookNames2.Add("26", "ek");
            bookNames2.Add("27", "da");
            bookNames2.Add("28", "ho");
            bookNames2.Add("29", "jl");
            bookNames2.Add("30", "am");
            bookNames2.Add("31", "ob");
            bookNames2.Add("32", "jn");
            bookNames2.Add("33", "mi");
            bookNames2.Add("34", "na");
            bookNames2.Add("35", "hb");
            bookNames2.Add("36", "zp");
            bookNames2.Add("37", "hg");
            bookNames2.Add("38", "zc");
            bookNames2.Add("39", "ma");
            bookNames2.Add("40", "Mat");
            bookNames2.Add("41", "Mrk");
            bookNames2.Add("42", "Luk");
            bookNames2.Add("43", "Jhn");
            bookNames2.Add("44", "Act");
            bookNames2.Add("45", "Rom");
            bookNames2.Add("46", "1Co");
            bookNames2.Add("47", "2Co");
            bookNames2.Add("48", "Gal");
            bookNames2.Add("49", "Eph");
            bookNames2.Add("50", "Php");
            bookNames2.Add("51", "Col");
            bookNames2.Add("52", "1Th");
            bookNames2.Add("53", "2Th");
            bookNames2.Add("54", "1Tm");
            bookNames2.Add("55", "2Tm");
            bookNames2.Add("56", "Tit");
            bookNames2.Add("57", "Phm");
            bookNames2.Add("58", "Heb");
            bookNames2.Add("59", "Jms");
            bookNames2.Add("60", "1Pe");
            bookNames2.Add("61", "2Pe");
            bookNames2.Add("62", "1Jn");
            bookNames2.Add("63", "2Jn");
            bookNames2.Add("64", "3Jn");
            bookNames2.Add("65", "Jud");
            bookNames2.Add("66", "Rev");

            return bookNames2;
        }

        public static Hashtable LoadBookNames5()
        {
            Hashtable bookNames = new Hashtable();

            bookNames.Add("1", "GEN");
            bookNames.Add("2", "EXO");
            bookNames.Add("3", "LEV");
            bookNames.Add("4", "NUM");
            bookNames.Add("5", "DEU");
            bookNames.Add("6", "JOS");
            bookNames.Add("7", "JDG");
            bookNames.Add("8", "RUT");
            bookNames.Add("9", "1SA");
            bookNames.Add("10", "2SA");
            bookNames.Add("11", "1KI");
            bookNames.Add("12", "2KI");
            bookNames.Add("13", "1CH");
            bookNames.Add("14", "2CH");
            bookNames.Add("15", "EZR");
            bookNames.Add("16", "NEH");
            bookNames.Add("17", "EST");
            bookNames.Add("18", "JOB");
            bookNames.Add("19", "PSA");
            bookNames.Add("20", "PRO");
            bookNames.Add("21", "ECC");
            bookNames.Add("22", "SNG");
            bookNames.Add("23", "ISA");
            bookNames.Add("24", "JER");
            bookNames.Add("25", "LAM");
            bookNames.Add("26", "EZK");
            bookNames.Add("27", "DAN");
            bookNames.Add("28", "HOS");
            bookNames.Add("29", "JOL");
            bookNames.Add("30", "AMO");
            bookNames.Add("31", "OBA");
            bookNames.Add("32", "JON");
            bookNames.Add("33", "MIC");
            bookNames.Add("34", "NAM");
            bookNames.Add("35", "HAB");
            bookNames.Add("36", "ZEP");
            bookNames.Add("37", "HAG");
            bookNames.Add("38", "ZEC");
            bookNames.Add("39", "MAL");
            bookNames.Add("40", "Mat");
            bookNames.Add("41", "Mrk");
            bookNames.Add("42", "Luk");
            bookNames.Add("43", "Jhn");
            bookNames.Add("44", "Act");
            bookNames.Add("45", "Rom");
            bookNames.Add("46", "1Co");
            bookNames.Add("47", "2Co");
            bookNames.Add("48", "Gal");
            bookNames.Add("49", "Eph");
            bookNames.Add("50", "Php");
            bookNames.Add("51", "Col");
            bookNames.Add("52", "1Th");
            bookNames.Add("53", "2Th");
            bookNames.Add("54", "1Tm");
            bookNames.Add("55", "2Tm");
            bookNames.Add("56", "Tit");
            bookNames.Add("57", "Phm");
            bookNames.Add("58", "Heb");
            bookNames.Add("59", "Jms");
            bookNames.Add("60", "1Pe");
            bookNames.Add("61", "2Pe");
            bookNames.Add("62", "1Jn");
            bookNames.Add("63", "2Jn");
            bookNames.Add("64", "3Jn");
            bookNames.Add("65", "Jud");
            bookNames.Add("66", "Rev");

            return bookNames;
        }

        public static Hashtable LoadBookNames6()  // for UBS
        {
            Hashtable bookNames = new Hashtable();

            bookNames.Add("1", "GEN");
            bookNames.Add("2", "EXO");
            bookNames.Add("3", "LEV");
            bookNames.Add("4", "NUM");
            bookNames.Add("5", "DEU");
            bookNames.Add("6", "JOS");
            bookNames.Add("7", "JDG");
            bookNames.Add("8", "RUT");
            bookNames.Add("9", "1SA");
            bookNames.Add("10", "2SA");
            bookNames.Add("11", "1KI");
            bookNames.Add("12", "2KI");
            bookNames.Add("13", "1CH");
            bookNames.Add("14", "2CH");
            bookNames.Add("15", "EZR");
            bookNames.Add("16", "NEH");
            bookNames.Add("17", "EST");
            bookNames.Add("18", "JOB");
            bookNames.Add("19", "PSA");
            bookNames.Add("20", "PRO");
            bookNames.Add("21", "ECC");
            bookNames.Add("22", "SNG");
            bookNames.Add("23", "ISA");
            bookNames.Add("24", "JER");
            bookNames.Add("25", "LAM");
            bookNames.Add("26", "EZK");
            bookNames.Add("27", "DAN");
            bookNames.Add("28", "HOS");
            bookNames.Add("29", "JOL");
            bookNames.Add("30", "AMO");
            bookNames.Add("31", "OBA");
            bookNames.Add("32", "JON");
            bookNames.Add("33", "MIC");
            bookNames.Add("34", "NAM");
            bookNames.Add("35", "HAB");
            bookNames.Add("36", "ZEP");
            bookNames.Add("37", "HAG");
            bookNames.Add("38", "ZEC");
            bookNames.Add("39", "MAL");
            bookNames.Add("40", "MAT");
            bookNames.Add("41", "MRK");
            bookNames.Add("42", "LUK");
            bookNames.Add("43", "JHN");
            bookNames.Add("44", "ACT");
            bookNames.Add("45", "ROM");
            bookNames.Add("46", "1CO");
            bookNames.Add("47", "2CO");
            bookNames.Add("48", "GAL");
            bookNames.Add("49", "EPH");
            bookNames.Add("50", "PHP");
            bookNames.Add("51", "COL");
            bookNames.Add("52", "1TH");
            bookNames.Add("53", "2TH");
            bookNames.Add("54", "1TI");
            bookNames.Add("55", "2TI");
            bookNames.Add("56", "TIT");
            bookNames.Add("57", "PHM");
            bookNames.Add("58", "HEB");
            bookNames.Add("59", "JAS");
            bookNames.Add("60", "1PE");
            bookNames.Add("61", "2PE");
            bookNames.Add("62", "1JN");
            bookNames.Add("63", "2JN");
            bookNames.Add("64", "3JN");
            bookNames.Add("65", "JUD");
            bookNames.Add("66", "REV");

            return bookNames;
        }

        public static Hashtable LoadBookNames()
        {
            Hashtable bookNames = new Hashtable();

            bookNames.Add("1", "Gen");
            bookNames.Add("2", "Exo");
            bookNames.Add("3", "Lev");
            bookNames.Add("4", "Num");
            bookNames.Add("5", "Deu");
            bookNames.Add("6", "Jsh");
            bookNames.Add("7", "Jdg");
            bookNames.Add("8", "Rth");
            bookNames.Add("9", "1Sm");
            bookNames.Add("10", "2Sm");
            bookNames.Add("11", "1Kn");
            bookNames.Add("12", "2Kn");
            bookNames.Add("13", "1Ch");
            bookNames.Add("14", "2Ch");
            bookNames.Add("15", "Ezr");
            bookNames.Add("16", "Neh");
            bookNames.Add("17", "Est");
            bookNames.Add("18", "Job");
            bookNames.Add("19", "Psm");
            bookNames.Add("20", "Pro");
            bookNames.Add("21", "Ecc");
            bookNames.Add("22", "Son");
            bookNames.Add("23", "Isa");
            bookNames.Add("24", "Jer");
            bookNames.Add("25", "Lam");
            bookNames.Add("26", "Ezk");
            bookNames.Add("27", "Dan");
            bookNames.Add("28", "Hos");
            bookNames.Add("29", "Joe");
            bookNames.Add("30", "Ams");
            bookNames.Add("31", "Obd");
            bookNames.Add("32", "Jna");
            bookNames.Add("33", "Mic");
            bookNames.Add("34", "Nhm");
            bookNames.Add("35", "Hab");
            bookNames.Add("36", "Zep");
            bookNames.Add("37", "Hag");
            bookNames.Add("38", "Zec");
            bookNames.Add("39", "Mal");
            bookNames.Add("40", "Mat");
            bookNames.Add("41", "Mrk");
            bookNames.Add("42", "Luk");
            bookNames.Add("43", "Jhn");
            bookNames.Add("44", "Act");
            bookNames.Add("45", "Rom");
            bookNames.Add("46", "1Co");
            bookNames.Add("47", "2Co");
            bookNames.Add("48", "Gal");
            bookNames.Add("49", "Eph");
            bookNames.Add("50", "Php");
            bookNames.Add("51", "Col");
            bookNames.Add("52", "1Th");
            bookNames.Add("53", "2Th");
            bookNames.Add("54", "1Tm");
            bookNames.Add("55", "2Tm");
            bookNames.Add("56", "Tit");
            bookNames.Add("57", "Phm");
            bookNames.Add("58", "Heb");
            bookNames.Add("59", "Jms");
            bookNames.Add("60", "1Pe");
            bookNames.Add("61", "2Pe");
            bookNames.Add("62", "1Jn");
            bookNames.Add("63", "2Jn");
            bookNames.Add("64", "3Jn");
            bookNames.Add("65", "Jud");
            bookNames.Add("66", "Rev");

            return bookNames;
        }

        public static Hashtable LoadFullBookNames()
        {
            Hashtable bookNames = new Hashtable();

            bookNames.Add("1", "Genesis");
            bookNames.Add("2", "Exodus");
            bookNames.Add("3", "Leviticus");
            bookNames.Add("4", "Numbers");
            bookNames.Add("5", "Deuteronomy");
            bookNames.Add("6", "Joshua");
            bookNames.Add("7", "Judges");
            bookNames.Add("8", "Ruth");
            bookNames.Add("9", "1 Samuel");
            bookNames.Add("10", "2 Samuel");
            bookNames.Add("11", "1 Kings");
            bookNames.Add("12", "2 Kings");
            bookNames.Add("13", "1 Chronicles");
            bookNames.Add("14", "2 Chronicies");
            bookNames.Add("15", "Ezra");
            bookNames.Add("16", "Nehemiah");
            bookNames.Add("17", "Esther");
            bookNames.Add("18", "Job");
            bookNames.Add("19", "Psalms");
            bookNames.Add("20", "Proverbs");
            bookNames.Add("21", "Ecclesiastes");
            bookNames.Add("22", "Song of Songs");
            bookNames.Add("23", "Isaiah");
            bookNames.Add("24", "Jeremiah");
            bookNames.Add("25", "Lamentations");
            bookNames.Add("26", "Ezekiel");
            bookNames.Add("27", "Daniel");
            bookNames.Add("28", "Hosea");
            bookNames.Add("29", "Joel");
            bookNames.Add("30", "Amos");
            bookNames.Add("31", "Obadiah");
            bookNames.Add("32", "Jonah");
            bookNames.Add("33", "Micah");
            bookNames.Add("34", "Nahum");
            bookNames.Add("35", "Habakkuk");
            bookNames.Add("36", "Zephaniah");
            bookNames.Add("37", "Haggai");
            bookNames.Add("38", "Zechariah");
            bookNames.Add("39", "Malachi");
            bookNames.Add("40", "Matthew");
            bookNames.Add("41", "Mark");
            bookNames.Add("42", "Luke");
            bookNames.Add("43", "John");
            bookNames.Add("44", "Acts");
            bookNames.Add("45", "Romans");
            bookNames.Add("46", "1 Corinthians");
            bookNames.Add("47", "2 Corinthians");
            bookNames.Add("48", "Galatians");
            bookNames.Add("49", "Ephesians");
            bookNames.Add("50", "Philippians");
            bookNames.Add("51", "Colossians");
            bookNames.Add("52", "1 Thessalonians");
            bookNames.Add("53", "2 Thessalonians");
            bookNames.Add("54", "1 Timothy");
            bookNames.Add("55", "2 Timothy");
            bookNames.Add("56", "Titus");
            bookNames.Add("57", "Philemon");
            bookNames.Add("58", "Hebrews");
            bookNames.Add("59", "James");
            bookNames.Add("60", "1 Peter");
            bookNames.Add("61", "2 Peter");
            bookNames.Add("62", "1 John");
            bookNames.Add("63", "2 John");
            bookNames.Add("64", "3 John");
            bookNames.Add("65", "Jude");
            bookNames.Add("66", "Revelation");

            return bookNames;
        }
	}
}
