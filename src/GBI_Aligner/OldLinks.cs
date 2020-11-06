using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;

using Newtonsoft.Json;

namespace GBI_Aligner
{
    public class OldLinks
    {
        public static Dictionary<string, string> CreateIdMap(List<SourceWord> sWords)
        {
            Dictionary<string, string> idMap = new Dictionary<string, string>();

            foreach(SourceWord sWord in sWords)
            {
                idMap.Add(sWord.ID, sWord.AltID);
            }

            return idMap;
        }

        public static TargetWord GetTarget(string altID, List<TargetWord> targetWords)
        {
            //foreach(TargetWord targetWord in targetWords)
            //{
            //    if (altID == targetWord.AltID)
            //    {
            //        return targetWord;
            //    }
            //}

            //return null;

            return
                targetWords.Where(tw => altID == tw.AltID).FirstOrDefault();
        }
    }
}
