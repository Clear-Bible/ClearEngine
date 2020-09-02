using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.IO;

namespace ParallelFiles
{
    class VerseText
    {
        public static Hashtable CreateVerseTable(string file, bool lowercase)
        {
            Hashtable verseTable = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach(string line in lines)
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

    public class SourceWrd
    {
        public string MorphID;
        public string Encode;
        public string EncodeAccent;
        public string EncodeConsonant;
        public string Analysis;
        public string Category;
        public string StrongNumberX;
        public string Lemma;
    }

    public class TargetWrd
    {
        public string WordID;
        public string Text;
        public string Lemma;
        public string Category;
    }
}
