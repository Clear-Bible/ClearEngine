using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace GBI_Aligner
{
    class Patterns
    {
        public static ManuscriptWord GetPrimaryWord(ManuscriptWord[] mWords)
        {
            string ptrn = GetPosPattern(mWords);
            if (ptrn == "adj det" || ptrn == "noun det" || ptrn == "noun prep" || ptrn == "verb conj" || ptrn == "noun conj") return mWords[0];
            if (ptrn == "adj conj" || ptrn == "verb det" || ptrn == "verb prep") return mWords[0];
            if (ptrn == "noun pron" || ptrn == "verb pron") return mWords[0];
            if (ptrn == "noun conj det" || ptrn == "noun prep det") return mWords[0];

            return null;
        }

        static string GetPosPattern(ManuscriptWord[] mWords)
        {
            string pattern = string.Empty;

            for (int i = 0; i < mWords.Length; i++)
            {
                pattern += mWords[i].pos + " ";
            }

            return pattern.Trim();
        }
    }
}
