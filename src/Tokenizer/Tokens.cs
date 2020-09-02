using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace Tokenizer
{
    public class Tokens
    {
        // Segment the verse text
        public static void Tokenize(
            string rawFile, // the original verse text file in verse-per-line format
            string tokFile, // the tokenized file in original case
//            string tokLowerFile, // the tokenized file all in lower case
            ArrayList puncs, // list of punctuation marks
            string lang // language of the verse text
            )
        {
            StreamWriter sw = new StreamWriter(tokFile, false, Encoding.UTF8);
//            StreamWriter sw2 = new StreamWriter(tokLowerFile, false, Encoding.UTF8);

            using (StreamReader sr = new StreamReader(rawFile, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Trim().Length < 9) continue;
                    string verseID = line.Substring(0, line.IndexOf(" "));
                    if (verseID == "01016011")
                    {
                        ;
                    }
                    string verseText = line.Substring(line.IndexOf(" ") + 1);
                    string puncText = string.Empty;
                    string puncLowerText = string.Empty;
                    SegPuncs(ref puncText, ref puncLowerText, verseText.Trim(), puncs, lang);
                    sw.WriteLine("{0} {1}", verseID, puncText);
 //                   sw2.WriteLine("{0} {1}", verseID, puncLowerText);
                }
            }

            sw.Close();
 //           sw2.Close();
        }

        static void SegPuncs(ref string puncText, ref string puncLowerText, string verseText, ArrayList puncs, string lang)
        {
            verseText = verseText.Replace("—", " — ");
            verseText = verseText.Replace("-", " - ");
            verseText = verseText.Replace(",“", ", “");
            //            verseText = verseText.Replace("’", " ’ ");
            verseText = verseText.Replace("  ", " ");
            if (lang == "Gbary")
            {
                verseText = verseText.Replace("^", "");
            }
            verseText = verseText.Trim();
            string[] words = verseText.Split(" ".ToCharArray());

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                char punc = FindPunc(word, puncs);
                string[] subWords = word.Split(punc);
                if (subWords.Length == 2)
                {
                    SepPuncs(ref puncText, ref puncLowerText, subWords[0], puncs, lang);
                    SepPuncs(ref puncText, ref puncLowerText, punc.ToString(), puncs, lang);
                    SepPuncs(ref puncText, ref puncLowerText, subWords[1], puncs, lang);
                }
                else
                {
                    SepPuncs(ref puncText, ref puncLowerText, word, puncs, lang);
                }
            }
        }

        static void SepPuncs(ref string puncText, ref string puncLowerText, string word, ArrayList puncs, string lang)
        {
            ArrayList postPuncs = new ArrayList();

            while (word.Length > 0 && (StartsWithPunc(word, puncs) || StartsWithPunc2(word, puncs)))
            {
                if (StartsWithPunc2(word, puncs))
                {
                    string firstChars = word.Substring(0, 2);
                    puncText += " " + firstChars;
                    puncLowerText += " " + firstChars;
                    word = word.Substring(2);
                }
                else
                {
                    string firstChar = word.Substring(0, 1);
                    puncText += " " + firstChar;
                    puncLowerText += " " + firstChar;
                    word = word.Substring(1);
                }
            }

            if (lang == "French")
            {
                string contractedWord = string.Empty;

                if (word.Contains("’"))
                {
                    contractedWord = GetContractedWord(word, "’");
                }
                if (word.Contains("ʼ"))
                {
                    contractedWord = GetContractedWord(word, "ʼ");
                }

                if (contractedWord.Length > 1)
                {
                    puncText += " " + contractedWord;
                    puncLowerText += " " + contractedWord;
                    word = word.Substring(contractedWord.Length);
                }
            }

            while (word.Length > 0 && (EndsWithPunc(word, puncs) || EndsWithPunc2(word, puncs)))
            {
                if (EndsWithPunc2(word, puncs))
                {
                    string lastChars = word.Substring(word.Length - 2);
                    postPuncs.Add(lastChars);
                    word = word.Substring(0, word.Length - 2);
                }
                else
                {
                    string lastChar = word.Substring(word.Length - 1);
                    postPuncs.Add(lastChar);
                    word = word.Substring(0, word.Length - 1);
                }
            }

            puncText += " " + word;
            puncLowerText += " " + word.ToLower();

            if (postPuncs.Count > 0)
            {
                for (int i = postPuncs.Count - 1; i >= 0; i--)
                {
                    string c = (string)postPuncs[i];
                    puncText += " " + c;
                    puncLowerText += " " + c;
                }
            }

            puncText = puncText.Trim();
            puncLowerText = puncLowerText.Trim();
        }

        static bool StartsWithPunc(string word, ArrayList puncs)
        {
            string firstChar = word.Substring(0, 1);
            if (puncs.Contains(firstChar))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool StartsWithPunc2(string word, ArrayList puncs)
        {
            if (word.Length > 1)
            {
                string firstChars = word.Substring(0, 2);
                if (puncs.Contains(firstChars))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        static bool EndsWithPunc(string word, ArrayList puncs)
        {
            string lastChar = word.Substring(word.Length - 1);
            if (puncs.Contains(lastChar))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool EndsWithPunc2(string word, ArrayList puncs)
        {
            if (word.Length > 1)
            {
                string lastChars = word.Substring(word.Length - 2);
                if (puncs.Contains(lastChars))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        static char FindPunc(string word, ArrayList puncs)
        {
            Regex r = new Regex("[0-9]+.+[0-9]+");
            Match m = r.Match(word);
            if (m.Success)
            {
                return (char) 0 ;
            }
            char[] chars = word.ToCharArray();
            for (int i = 1; i < chars.Length - 1; i++) // excluding puncs on the peripheral
            {
                if (puncs.Contains(chars[i].ToString()) && chars[i].ToString() != "’" && chars[i].ToString() != "'")
                {
                    return chars[i];
                }
            }

            return (char) 0;
        }

        static string GetContractedWord(string word, string apostraphe)
        {
            return word.Substring(0, word.IndexOf(apostraphe) + 1);
        }

        static ArrayList GetWordList(string file)
        {
            ArrayList wordList = new ArrayList();

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    wordList.Add(line.Trim());
                }
            }

            return wordList;
        }
    }
}
