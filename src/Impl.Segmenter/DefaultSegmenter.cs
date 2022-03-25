using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

using ClearBible.Clear3.API;


namespace ClearBible.Clear3.Impl.DefaultSegmenter
{
    /// <summary>
    /// Implementation of ISegmenter.  The code here was taken from Clear2
    /// with very little change.
    /// </summary>
    ///
    /// 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
    /// 
    public class DefaultSegmenter : ISegmenter
    {
        public (string[], string[]) GetSegments(
            string text,
            HashSet<string> puncs,
            string lang,
            string culture)
        {
            var cultureInfo = new CultureInfo(culture);
            string tokens = "", tokenLowercase = "";
            SegPuncs(ref tokens, ref tokenLowercase, text.Trim(), puncs, lang, cultureInfo);

            return (tokens.Split(), tokenLowercase.Split());
        }


        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private void SegPuncs(ref string puncText, ref string puncLowerText, string verseText, HashSet<string> puncs, string lang, CultureInfo cultureInfo)
        {
            // 2020.09.11 CL: Do we need these now that I run verse files through a program to clean up the verses? Will need to check.

            // verseText = verseText.Replace("—", " — ");
            // verseText = verseText.Replace("-", " - ");
            // verseText = verseText.Replace(",“", ", “");
            // verseText = verseText.Replace("  ", " ");
            if (lang == "Gbary")
            {
                verseText = verseText.Replace("^", "");
            }
            verseText = verseText.Trim();
            string[] words = verseText.Split();

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                char punc = FindPunc(word, puncs);
                string[] subWords = word.Split(punc);
                if (subWords.Length == 2)
                {
                    SepPuncs(ref puncText, ref puncLowerText, subWords[0], puncs, lang, cultureInfo);
                    SepPuncs(ref puncText, ref puncLowerText, punc.ToString(), puncs, lang, cultureInfo);
                    SepPuncs(ref puncText, ref puncLowerText, subWords[1], puncs, lang, cultureInfo);
                }
                else
                {
                    SepPuncs(ref puncText, ref puncLowerText, word, puncs, lang, cultureInfo);
                }
            }
        }

        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private char FindPunc(string word, HashSet<string> puncs)
        {
            Regex r = new Regex("[0-9]+.+[0-9]+");
            Match m = r.Match(word);
            if (m.Success)
            {
                return (char)0;
            }
            char[] chars = word.ToCharArray();
            for (int i = 1; i < chars.Length - 1; i++) // excluding puncs on the peripheral
            {
                if (puncs.Contains(chars[i].ToString()) && chars[i].ToString() != "’" && chars[i].ToString() != "'")
                {
                    return chars[i];
                }
            }

            return (char)0;
        }

        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private void SepPuncs(ref string puncText, ref string puncLowerText, string word, HashSet<string> puncs, string lang, CultureInfo cultureInfo)
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

            // 2021.05.26 CL: Modified to use the CultureInfo available in C# to do language/region specific lowercase.
            // puncLowerText += " " + word.ToLower();
            puncLowerText += " " + word.ToLower(cultureInfo);

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

        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private bool StartsWithPunc(string word, HashSet<string> puncs)
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

        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private bool StartsWithPunc2(string word, HashSet<string> puncs)
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


        private string GetContractedWord(string word, string apostraphe)
        {
            return word.Substring(0, word.IndexOf(apostraphe) + 1);
        }

        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private bool EndsWithPunc(string word, HashSet<string> puncs)
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

        // 2022.03.24 CL: Changed puncs to HashSet<string> from List<string>
        private bool EndsWithPunc2(string word, HashSet<string> puncs)
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
    }
}
