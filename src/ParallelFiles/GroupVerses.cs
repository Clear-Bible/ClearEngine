using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;

using Utilities;

namespace ParallelFiles
{
    public class GroupVerses
    {
        static Hashtable sourceTable = null;
        static Hashtable sourceIdTable = null;
        static Hashtable sourceIdLemmaTable = null;

        public static void CreateParallelFiles(
            string sourceFile, // original source file
            string sourceIdFile, // original source file with word IDs
            string sourceIdLemmaFile, // original source file in lemmas and with IDs
            string targetFile, // original target file
            string parallelSourceFile, // source file with grouped verses
            string parallelSourceIdFile, // source ID file with grouped verses        
            string parallelSourceIdLemmaFile, // source ID lemma file with grouped verses         
            string parallelTargetFile, // target file with grouped verses
            string parallelTargetIdFile, // target ID file with grouped verses
            ArrayList versificationList // list of verse pairs
            )
        {
            string targetIdFile0 = targetFile.Substring(0, targetFile.Length - 4) + ".0.txt";

            CreateTargetIdFile(targetFile, targetIdFile0);  // puts word-number suffix on each word
            
            if (sourceTable == null)
                sourceTable = VerseText.CreateVerseTable(sourceFile, false);
                // sourceTable = Hashtable(verse-id => verse text)
            if (sourceIdTable == null)
                sourceIdTable = VerseText.CreateVerseTable(sourceIdFile, false);
            if (sourceIdLemmaTable == null)
                sourceIdLemmaTable = VerseText.CreateVerseTable(sourceIdLemmaFile, false);

            Hashtable targetTable = VerseText.CreateVerseTable(targetFile, true);
            Hashtable targetIdTable = VerseText.CreateVerseTable(targetIdFile0, false);

            StreamWriter swSource = new StreamWriter(parallelSourceFile, false, Encoding.UTF8);
            StreamWriter swSourceId = new StreamWriter(parallelSourceIdFile, false, Encoding.UTF8);
            StreamWriter swSourceIdLemma = new StreamWriter(parallelSourceIdLemmaFile, false, Encoding.UTF8);

            StreamWriter swTarget = new StreamWriter(parallelTargetFile, false, Encoding.UTF8);
            StreamWriter swTargetId = new StreamWriter(parallelTargetIdFile, false, Encoding.UTF8);

            foreach (VersePair vp in versificationList)  // VersePair = { ArrayList MVerses, TVerses }
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
                    if (targetTable.ContainsKey(tVerse))
                    {
                        tText += (string)targetTable[tVerse] + " ";
                        tTextWithID += (string)targetIdTable[tVerse] + " ";
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
//            swTargetLowerId.Close();
            swTargetId.Close();
        }

        static string GetVersesID(ArrayList verses)
        {
            string versesId = string.Empty;

            foreach (string verse in verses)
            {
                versesId += verse + " ";
            }

            return versesId;
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
}

