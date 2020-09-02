using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;

namespace Utilities
{
    public class Versification
    {
        public static Hashtable LoadVersification(
            string xmlPath,
            string type,
            string format
            )
        {
            Hashtable versification = new Hashtable();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
//                if (Int32.Parse(mb) > 39) continue;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                if (mb == "1" && mc == "32" && mv == "1")
                {
                    Console.WriteLine();
                }

                string mVerse = string.Empty;
                string tVerse = string.Empty;

                if (format == "id")
                {
                    mVerse = GetVerseID(mb, mc, mv, bookNames);
                    tVerse = GetVerseID(tb, tc, tv, bookNames);
                }
                else
                {
                    mVerse = GetVerseName(mb, mc, mv, bookNames);
                    tVerse = GetVerseName(tb, tc, tv, bookNames);
                }

                if (versification.ContainsKey(mVerse))
                {
                    ArrayList verses = (ArrayList)versification[mVerse];
                    verses.Add(tVerse);
                }
                else
                {
                    ArrayList verses = new ArrayList();
                    verses.Add(tVerse);
                    versification.Add(mVerse, verses);
                }
            }

            return versification;
        }

        public static ArrayList LoadVersificationList(
            string xmlPath,
            string type,
            string format
            )
        {
            ArrayList versification = new ArrayList();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            VersePair currVersePair = new VersePair();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                if (mb == "66" && mc == "22" && mv == "20")
                {
                    ;
                }

                string mVerse = string.Empty;
                string tVerse = string.Empty;

                if (format == "id")
                {
                    mVerse = GetVerseID(mb, mc, mv, bookNames);
                    tVerse = GetVerseID(tb, tc, tv, bookNames);
                }
                else
                {
                    mVerse = GetVerseName(mb, mc, mv, bookNames);
                    tVerse = GetVerseName(tb, tc, tv, bookNames);
                }

                if (currVersePair.Mverses.Contains(mVerse))
                {
                    currVersePair.Tverses.Add(tVerse);
                }
                else if (currVersePair.Tverses.Contains(tVerse))
                {
                    currVersePair.Mverses.Add(mVerse);
                }
                else
                {
                    if (!(currVersePair.Mverses.Count == 0 && currVersePair.Tverses.Count == 0))
                    {
                        versification.Add(CopyOfVersePair(currVersePair));
                    }

                    ArrayList mVerses = new ArrayList();
                    ArrayList tVerses = new ArrayList();
                    mVerses.Add(mVerse);
                    tVerses.Add(tVerse);
                    currVersePair.Mverses = mVerses;
                    currVersePair.Tverses = tVerses;
                }
            }

            versification.Add(CopyOfVersePair(currVersePair));

            return versification;
        }


        public static Hashtable LoadVersificationTable(
            string xmlPath,
            string type,
            string format
            )
        {
            Hashtable versification = new Hashtable();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            VersePair currVersePair = new VersePair();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                string mVerse = string.Empty;
                string tVerse = string.Empty;

                if (format == "id")
                {
                    mVerse = GetVerseID(mb, mc, mv, bookNames);
                    tVerse = GetVerseID(tb, tc, tv, bookNames);
                }
                else
                {
                    mVerse = GetVerseName(mb, mc, mv, bookNames);
                    tVerse = GetVerseName(tb, tc, tv, bookNames);
                }

                if (currVersePair.Mverses.Contains(mVerse))
                {
                    currVersePair.Tverses.Add(tVerse);
                }
                else if (currVersePair.Tverses.Contains(tVerse))
                {
                    currVersePair.Mverses.Add(mVerse);
                }
                else
                {
                    if (!(currVersePair.Mverses.Count == 0 && currVersePair.Tverses.Count == 0))
                    {
                        string verseKey = GetVerseKey(currVersePair.Mverses);
                        versification.Add(verseKey, currVersePair.Tverses);
                    }

                    ArrayList mVerses = new ArrayList();
                    ArrayList tVerses = new ArrayList();
                    mVerses.Add(mVerse);
                    tVerses.Add(tVerse);
                    currVersePair.Mverses = mVerses;
                    currVersePair.Tverses = tVerses;
                }
            }

            return versification;
        }

        static string GetVerseKey(ArrayList verses)
        {
            string verseKey = string.Empty;

            foreach(string verse in verses)
            {
                verseKey += verse + "+";
            }

            return verseKey.Substring(0, verseKey.Length - 1);
        }

        public static ArrayList LoadVersification2(
            string xmlPath,
            string type,
            ArrayList chaptersM,
            ArrayList chaptersT,
            string version
            )
        {
            ArrayList versification = new ArrayList();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            VersePair currVersePair = new VersePair();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                //                if (Int32.Parse(mb) > 39) continue;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                if (mb == "1" && mc == "32" && mv == "1")
                {
                    Console.WriteLine();
                }

                string mVerse = string.Empty;
                string tVerse = string.Empty;

                mVerse = GetVerseName(mb, mc, mv, bookNames);
                tVerse = GetVerseName(tb, tc, tv, bookNames);

                if (!(chaptersM.Contains(mVerse.Substring(0, mVerse.IndexOf(":"))) && chaptersT.Contains(tVerse.Substring(0, tVerse.IndexOf(":")))))
                {
                    continue;
                }

                if (currVersePair.Mverses.Contains(mVerse))
                {
                    currVersePair.Tverses.Add(tVerse);
                }
                else if (currVersePair.Tverses.Contains(tVerse))
                {
                    currVersePair.Mverses.Add(mVerse);
                }
                else
                {
                    if (!(currVersePair.Mverses.Count == 0 && currVersePair.Tverses.Count == 0))
                    {
                        versification.Add(CopyOfVersePair(currVersePair));
                    }

                    ArrayList mVerses = new ArrayList();
                    ArrayList tVerses = new ArrayList();
                    mVerses.Add(mVerse);
                    tVerses.Add(tVerse);
                    if (mVerse == "gn32:1")
                    {
                        ;
                    }
                    currVersePair.Mverses = mVerses;
                    currVersePair.Tverses = tVerses;
                }
            }

            versification.Add(CopyOfVersePair(currVersePair));

/*            string xpath2 = "/Versifications/Type[@Switch='" + version + "']/VSF";

            XmlNodeList verseList2 = xmlDoc.SelectNodes(xpath2);

            VersePair currVersePair2 = new VersePair();

            ArrayList pairsToRemove = new ArrayList();

            for (int i = 0; i < verseList2.Count; i++)
            {
                XmlNode verseNode = verseList2[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                //                if (Int32.Parse(mb) > 39) continue;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                string mVerse = string.Empty;
                string tVerse = string.Empty;

                mVerse = GetVerseName(mb, mc, mv, bookNames);
                tVerse = GetVerseName(tb, tc, tv, bookNames);

                if (!(chaptersM.Contains(mVerse.Substring(0, mVerse.IndexOf(":"))) && chaptersT.Contains(tVerse.Substring(0, mVerse.IndexOf(":")))))
                {
                    continue;
                }

                VersePair pairToRemove = AddVersePair(mVerse, tVerse, ref versification);
                if (pairsToRemove != null)
                {
                    pairsToRemove.Add(pairToRemove);
                }
            }

            foreach (VersePair pair in pairsToRemove)
            {
                versification.Remove(pair);
            } */

            return versification;
        }

        public static ArrayList LoadVersification3(
            string xmlPath,
            string type,
            ArrayList chaptersM,
            ArrayList chaptersT,
            string version
            )
        {
            ArrayList versification = new ArrayList();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            VersePair currVersePair = new VersePair();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                //                if (Int32.Parse(mb) > 39) continue;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                if (mb == "1" && mc == "32" && mv == "1")
                {
                    Console.WriteLine();
                }

                string mVerse = string.Empty;
                string tVerse = string.Empty;
                string mVerse2 = string.Empty;
                string tVerse2 = string.Empty;

                mVerse = GetVerseID(mb, mc, mv, bookNames);
                tVerse = GetVerseID(tb, tc, tv, bookNames);

                mVerse2 = GetVerseName(mb, mc, mv, bookNames);
                tVerse2 = GetVerseName(tb, tc, tv, bookNames);

                if (!(chaptersM.Contains(mVerse2.Substring(0, mVerse2.IndexOf(":"))) && chaptersT.Contains(tVerse2.Substring(0, tVerse2.IndexOf(":")))))
                {
                    continue;
                }

                if (currVersePair.Mverses.Contains(mVerse))
                {
                    currVersePair.Tverses.Add(tVerse);
                }
                else if (currVersePair.Tverses.Contains(tVerse))
                {
                    currVersePair.Mverses.Add(mVerse);
                }
                else
                {
                    if (!(currVersePair.Mverses.Count == 0 && currVersePair.Tverses.Count == 0))
                    {
                        versification.Add(CopyOfVersePair(currVersePair));
                    }

                    ArrayList mVerses = new ArrayList();
                    ArrayList tVerses = new ArrayList();
                    mVerses.Add(mVerse);
                    tVerses.Add(tVerse);
                    currVersePair.Mverses = mVerses;
                    currVersePair.Tverses = tVerses;
                }
            }

            versification.Add(CopyOfVersePair(currVersePair));

            return versification;
        }

        private static VersePair AddVersePair(string mVerse, string tVerse, ref ArrayList versification)
        {
            VersePair p = null;

            foreach (VersePair pair in versification)
            {
                if (pair.Tverses.Contains(tVerse) && !pair.Mverses.Contains(mVerse))
                {
                    pair.Mverses.Add(mVerse);
                }
                if (pair.Mverses.Contains(mVerse) && pair.Tverses.Contains(mVerse))
                {
                    p = pair;
                    break;
                }
            }

            return p;
        }

        private static VersePair CopyOfVersePair(VersePair currVersePair)
        {
            VersePair copy = new VersePair();
            copy.Mverses = currVersePair.Mverses;
            copy.Tverses = currVersePair.Tverses;
            return copy;
        }

        private static VerseTriple CopyOfVerseTriple(VerseTriple currVerseTriple)
        {
            VerseTriple copy = new VerseTriple();
            copy.Mverses = currVerseTriple.Mverses;
            copy.Tverses1 = currVerseTriple.Tverses1;
            copy.Tverses2 = currVerseTriple.Tverses2;
            return copy;
        }

        public static string GetVerseName(string book, string chapter, string verse, Hashtable bookNames)
        {
            string bookName = (string)bookNames[UnPad(book)];
            return bookName + UnPad(chapter) + ":" + UnPad(verse);
        }

        private static string UnPad(string s)
        {
            return Int32.Parse(s).ToString();
        }

        private static string GetVerseID(string book, string chapter, string verse, Hashtable bookNames)
        {
            return Utils.Pad2(book) + Utils.Pad3(chapter) + Utils.Pad3(verse);
        }

        public static Hashtable LoadBookNames()
        {
            Hashtable bookNames = new Hashtable();

            bookNames.Add("1", "gn");
            bookNames.Add("2", "ex");
            bookNames.Add("3", "lv");
            bookNames.Add("4", "nu");
            bookNames.Add("5", "dt");
            bookNames.Add("6", "js");
            bookNames.Add("7", "ju");
            bookNames.Add("8", "ru");
            bookNames.Add("9", "1s");
            bookNames.Add("10", "2s");
            bookNames.Add("11", "1k");
            bookNames.Add("12", "2k");
            bookNames.Add("13", "1c");
            bookNames.Add("14", "2c");
            bookNames.Add("15", "er");
            bookNames.Add("16", "ne");
            bookNames.Add("17", "es");
            bookNames.Add("18", "jb");
            bookNames.Add("19", "ps");
            bookNames.Add("20", "pr");
            bookNames.Add("21", "ec");
            bookNames.Add("22", "ca");
            bookNames.Add("23", "is");
            bookNames.Add("24", "je");
            bookNames.Add("25", "lm");
            bookNames.Add("26", "ek");
            bookNames.Add("27", "da");
            bookNames.Add("28", "ho");
            bookNames.Add("29", "jl");
            bookNames.Add("30", "am");
            bookNames.Add("31", "ob");
            bookNames.Add("32", "jn");
            bookNames.Add("33", "mi");
            bookNames.Add("34", "na");
            bookNames.Add("35", "hb");
            bookNames.Add("36", "zp");
            bookNames.Add("37", "hg");
            bookNames.Add("38", "zc");
            bookNames.Add("39", "ma");
            bookNames.Add("40", "mat");
            bookNames.Add("41", "mrk");
            bookNames.Add("42", "luk");
            bookNames.Add("43", "jhn");
            bookNames.Add("44", "act");
            bookNames.Add("45", "rom");
            bookNames.Add("46", "1co");
            bookNames.Add("47", "2co");
            bookNames.Add("48", "gal");
            bookNames.Add("49", "eph");
            bookNames.Add("50", "php");
            bookNames.Add("51", "col");
            bookNames.Add("52", "1th");
            bookNames.Add("53", "2th");
            bookNames.Add("54", "1tm");
            bookNames.Add("55", "2tm");
            bookNames.Add("56", "tit");
            bookNames.Add("57", "phm");
            bookNames.Add("58", "heb");
            bookNames.Add("59", "jms");
            bookNames.Add("60", "1pe");
            bookNames.Add("61", "2pe");
            bookNames.Add("62", "1jn");
            bookNames.Add("63", "2jn");
            bookNames.Add("64", "3jn");
            bookNames.Add("65", "jud");
            bookNames.Add("66", "rev");

            return bookNames;
        }

        public static ArrayList LoadTripleVersificationList(
            string xmlPath,
            string type1,
            string type2
            )
        {
            ArrayList versification = new ArrayList();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            VersificationTables vt1 = BuildVersificationTables(xmlPath, type1);
            VersificationTables vt2 = BuildVersificationTables(xmlPath, type2);

            string xpath = "/Versifications/Type[@Switch='" + type1 + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            VerseTriple currVerseTriple = new VerseTriple();

            ArrayList seenMverses= new ArrayList();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
 //               string tb = verseNode.Attributes.GetNamedItem("TB").Value;
 //               string tc = verseNode.Attributes.GetNamedItem("TC").Value;
 //               string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                string mVerse = string.Empty;
//                string tVerse = string.Empty;

                mVerse = GetVerseID(mb, mc, mv, bookNames);
                if (mVerse == "42001025")
                {
                    ;
                }
//                tVerse = GetVerseID(tb, tc, tv, bookNames);

                ArrayList tVerses1 = (ArrayList)vt1.M2T_Table[mVerse];
                ArrayList tVerses2 = (ArrayList)vt2.M2T_Table[mVerse];

//                if (!seenMverses.Contains(mVerse))
                if (!currVerseTriple.Mverses.Contains(mVerse))
                {
                    if (currVerseTriple.Mverses.Count > 0)
                    {
                        versification.Add(CopyOfVerseTriple(currVerseTriple));
                        currVerseTriple = new VerseTriple();
                    }
                }

                if (currVerseTriple.Mverses.Contains(mVerse))
                {
                    foreach (string tv1 in tVerses1)
                    {
                        if (!currVerseTriple.Tverses1.Contains(tv1))
                        {
                            currVerseTriple.Tverses1.Add(tv1);
                        }
                        ArrayList mVerses1 = (ArrayList)vt1.T2M_Table[tv1];
                        foreach (string mv1 in mVerses1)
                        {
                            if (!currVerseTriple.Mverses.Contains(mv1))
                            {
                                currVerseTriple.Mverses.Add(mv1);
                                seenMverses.Add(mv1);
                            }
                        }
                    }
                    foreach (string tv2 in tVerses2)
                    {
                        if (!currVerseTriple.Tverses2.Contains(tv2))
                        {
                            currVerseTriple.Tverses2.Add(tv2);
                        }
                        ArrayList mVerses2 = (ArrayList)vt2.T2M_Table[tv2];
                        foreach (string mv2 in mVerses2)
                        {
                            if (!currVerseTriple.Mverses.Contains(mv2))
                            {
                                currVerseTriple.Mverses.Add(mv2);
                                seenMverses.Add(mv2);
                            }
                        }
                    }
                }
                else
                {
                    currVerseTriple.Mverses.Add(mVerse);
                    seenMverses.Add(mVerse);
                    currVerseTriple.Tverses1 = tVerses1;
                    currVerseTriple.Tverses2 = tVerses2;
                    foreach (string tv1 in tVerses1)
                    {
                        if (!currVerseTriple.Tverses1.Contains(tv1))
                        {
                            currVerseTriple.Tverses1.Add(tv1);
                        }
                        ArrayList mVerses1 = (ArrayList)vt1.T2M_Table[tv1];
                        foreach (string mv1 in mVerses1)
                        {
                            if (!currVerseTriple.Mverses.Contains(mv1))
                            {
                                currVerseTriple.Mverses.Add(mv1);
                            }
                        }
                    }
                    foreach (string tv2 in tVerses2)
                    {
                        if (!currVerseTriple.Tverses2.Contains(tv2))
                        {
                            currVerseTriple.Tverses2.Add(tv2);
                        }
                        ArrayList mVerses2 = (ArrayList)vt2.T2M_Table[tv2];
                        foreach (string mv2 in mVerses2)
                        {
                            if (!currVerseTriple.Mverses.Contains(mv2))
                            {
                                currVerseTriple.Mverses.Add(mv2);
                            }
                        }
                    }
                }
            }

            versification.Add(CopyOfVerseTriple(currVerseTriple));

            return versification;
        }

        static VersificationTables BuildVersificationTables(string xmlPath, string type)
        {
            VersificationTables vt = new VersificationTables();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            Hashtable bookNames = LoadBookNames();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                string mVerse = string.Empty;
                string tVerse = string.Empty;

                mVerse = GetVerseID(mb, mc, mv, bookNames);
                tVerse = GetVerseID(tb, tc, tv, bookNames);

                if (vt.M2T_Table.ContainsKey(mVerse))
                {
                    ArrayList verses = (ArrayList)vt.M2T_Table[mVerse];
                    verses.Add(tVerse);
                }
                else
                {
                    ArrayList verses = new ArrayList();
                    verses.Add(tVerse);
                    vt.M2T_Table.Add(mVerse, verses);
                }
                if (vt.T2M_Table.ContainsKey(tVerse))
                {
                    ArrayList verses = (ArrayList)vt.T2M_Table[tVerse];
                    verses.Add(mVerse);
                }
                else
                {
                    ArrayList verses = new ArrayList();
                    verses.Add(mVerse);
                    vt.T2M_Table.Add(tVerse, verses);
                }
            }

            return vt;
        }
    }

    public class VersePair
    {
        public ArrayList Mverses = new ArrayList();
        public ArrayList Tverses = new ArrayList();
    }

    public class VerseTriple
    {
        public ArrayList Mverses = new ArrayList();
        public ArrayList Tverses1 = new ArrayList();
        public ArrayList Tverses2 = new ArrayList();
    }

    public class VersificationTables
    {
        public Hashtable M2T_Table = new Hashtable();
        public Hashtable T2M_Table = new Hashtable();
    }

}
