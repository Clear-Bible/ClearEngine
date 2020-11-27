using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using ClearBible.Clear3.API;

namespace RegressionTest1
{
    public class VersePair2
    {
        public ArrayList Mverses = new ArrayList();
        public ArrayList Tverses = new ArrayList();
    }

    public class Versification2
    {
        public static ArrayList LoadVersificationList(
            string xmlPath,
            string type)
        {
            ArrayList versification = new ArrayList();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            string xpath = "/Versifications/Type[@Switch='" + type + "']/VSF";

            XmlNodeList verseList = xmlDoc.SelectNodes(xpath);

            VersePair2 currVersePair = new VersePair2();

            for (int i = 0; i < verseList.Count; i++)
            {
                XmlNode verseNode = verseList[i];
                string mb = verseNode.Attributes.GetNamedItem("MB").Value;
                string mc = verseNode.Attributes.GetNamedItem("MC").Value;
                string mv = verseNode.Attributes.GetNamedItem("MV").Value;
                string tb = verseNode.Attributes.GetNamedItem("TB").Value;
                string tc = verseNode.Attributes.GetNamedItem("TC").Value;
                string tv = verseNode.Attributes.GetNamedItem("TV").Value;

                string mVerse = GetVerseID(mb, mc, mv);
                string tVerse = GetVerseID(tb, tc, tv);

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


        private static string GetVerseID(string book, string chapter, string verse)
        {
            int b = int.Parse(book);
            int c = int.Parse(chapter);
            int v = int.Parse(verse);
            return $"{b:D2}{c:D3}{v:D3}";
        }


        private static VersePair2 CopyOfVersePair(VersePair2 currVersePair)
        {
            VersePair2 copy = new VersePair2();
            copy.Mverses = currVersePair.Mverses;
            copy.Tverses = currVersePair.Tverses;
            return copy;
        }
    }



    public class GroupVerses2
    {
        static Hashtable sourceTable = null;
        static Hashtable sourceIdTable = null;
        static Hashtable sourceIdLemmaTable = null;

        public static void CreateParallelFiles(
            TargetVerseCorpus targetVerseCorpus,
            string sourceFile, // original source file
            string sourceIdFile, // original source file with word IDs
            string sourceIdLemmaFile, // original source file in lemmas and with IDs

            string parallelSourceFile, // source file with grouped verses
            string parallelSourceIdFile, // source ID file with grouped verses        
            string parallelSourceIdLemmaFile, // source ID lemma file with grouped verses         
            string parallelTargetFile, // target file with grouped verses
            string parallelTargetIdFile, // target ID file with grouped verses
            ArrayList versificationList // list of verse pairs
            )
        {
            if (sourceTable == null)
                sourceTable = VerseText2.CreateVerseTable(sourceFile, false);
            if (sourceIdTable == null)
                sourceIdTable = VerseText2.CreateVerseTable(sourceIdFile, false);
            if (sourceIdLemmaTable == null)
                sourceIdLemmaTable = VerseText2.CreateVerseTable(sourceIdLemmaFile, false);

            StreamWriter swSource = new StreamWriter(parallelSourceFile, false, Encoding.UTF8);
            StreamWriter swSourceId = new StreamWriter(parallelSourceIdFile, false, Encoding.UTF8);
            StreamWriter swSourceIdLemma = new StreamWriter(parallelSourceIdLemmaFile, false, Encoding.UTF8);

            StreamWriter swTarget = new StreamWriter(parallelTargetFile, false, Encoding.UTF8);
            StreamWriter swTargetId = new StreamWriter(parallelTargetIdFile, false, Encoding.UTF8);

            Dictionary<VerseID, TargetVerse> targetVerseTable =
                targetVerseCorpus.List
                .ToDictionary(
                    tv => tv.List[0].TargetID.VerseID,
                    tv => tv);

            foreach (VersePair2 vp in versificationList)  // VersePair = { ArrayList MVerses, TVerses }
            {
                string sText = string.Empty;
                string sTextWithID = string.Empty;
                string sTextWithIDLemma = string.Empty;

                foreach (string mVerse in vp.Mverses)
                {
                    if (sourceTable.ContainsKey(mVerse))
                    {
                        sText += (string)sourceTable[mVerse] + " ";
                        sTextWithID += (string)sourceIdTable[mVerse] + " ";
                        sTextWithIDLemma += (string)sourceIdLemmaTable[mVerse] + " ";
                    }
                }

                if (sText == string.Empty) continue;

                string tText = string.Empty;
                string tTextWithID = string.Empty;

                foreach (string tVerse in vp.Tverses)
                {
                    if (targetVerseTable.TryGetValue(new VerseID(tVerse),
                        out TargetVerse targetVerse))
                    {
                        string verseText = string.Join(" ",
                            targetVerse.List.Select(t =>
                                t.TargetText.Text.ToLower()));

                        string verseIDText = string.Join(" ",
                            targetVerse.List.Select(t =>
                                $"{t.TargetText.Text}_{t.TargetID.AsCanonicalString}"));

                        tText += verseText + " ";
                        tTextWithID += verseIDText + " ";
                    }
                }

                if (tText == string.Empty) continue;

                if (sText.Trim().Length > 1 && tText.Trim().Length > 1)
                {
                    swSource.WriteLine(sText.Trim().Replace("  ", " "));
                    swSourceId.WriteLine(sTextWithID.Trim().Replace("  ", " "));
                    swSourceIdLemma.WriteLine(sTextWithIDLemma.Trim().Replace("  ", " "));
                    swTarget.WriteLine(tText.Trim().Replace("  ", " "));
                    swTargetId.WriteLine(tTextWithID.Trim().Replace("  ", " "));
                }
            }

            swSource.Close();
            swSourceId.Close();
            swSourceIdLemma.Close();
            swTarget.Close();
            swTargetId.Close();
        }


        static void CreateTargetIdFile(string targetFile, string targetIdFile)
        {
            StreamWriter sw = new StreamWriter(targetIdFile, false, Encoding.UTF8);

            using (StreamReader sr = new StreamReader(targetFile, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    string verseID = line.Substring(0, line.IndexOf(" "));
                    string verseText = line.Substring(line.IndexOf(" ") + 1).Trim();
                    string verseIdText = string.Empty;

                    string[] words = verseText.Split(" ".ToCharArray());

                    for (int i = 0; i < words.Length; i++)
                    {
                        string wordID = verseID + (i + 1).ToString().PadLeft(3, '0');
                        verseIdText += words[i] + "_" + wordID + " ";
                    }

                    sw.WriteLine("{0} {1}", verseID, verseIdText);
                }
            }

            sw.Close();
        }
    }


    class VerseText2
    {
        public static Hashtable CreateVerseTable(string file, bool lowercase)
        {
            Hashtable verseTable = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string verseID = line.Substring(0, line.IndexOf(" "));
                string verseText = line.Substring(line.IndexOf(" ") + 1).Trim();
                if (lowercase) verseText = verseText.ToLower();
                verseTable.Add(verseID, verseText);
            }

            return verseTable;
        }

        public static ArrayList GetTexts(string file)
        {
            ArrayList texts = new ArrayList();

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {

                    texts.Add(line);
                }
            }

            return texts;
        }
    }
}
