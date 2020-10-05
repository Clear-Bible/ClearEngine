using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Xml;

using Trees;
using Utilities;

namespace GBI_Aligner
{
    class Data
    {
        public static List<string> GetVerses(string file, bool lowercase)
        {
            List<string> verses = new List<string>();

            string[] lines = File.ReadAllLines(file);
            foreach(string line in lines)
            {
                if (lowercase) verses.Add(line.ToLower());
                else verses.Add(line);
            }

            return verses;
        }

        public static string GetWord(string word)
        {
            if (!word.Contains("_")) return string.Empty;

            string w = string.Empty;

            w = word.Substring(0, word.LastIndexOf("_"));

            return w;
        }

        // pathProbs :: path => score
        public static ArrayList SortPaths(Hashtable pathProbs)
        {
            int hashCodeOfWordsInPath(object path)
            {
                ArrayList wordsInPath = new ArrayList();
                Align.GetWordsInPath((ArrayList)path, ref wordsInPath);
                return GetWords(wordsInPath).GetHashCode();
            }

            return new ArrayList(
                pathProbs
                    .Cast<DictionaryEntry>()
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenByDescending(kvp => hashCodeOfWordsInPath(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList());
        }
 
        //// pathProbs :: Hashtable(Candidate, probability)
        //// returns ArrayList(Candidate)
        ////
        //public static ArrayList SortPaths(Hashtable pathProbs)
        //{
        //    ArrayList sortedPaths = Sort.SortTableDoubleDesc(pathProbs);
        //    return SecondarySort(sortedPaths, pathProbs);
        //}

        //// 
        //static public ArrayList SecondarySort(ArrayList paths, Hashtable pathProbs)
        //{
        //    ArrayList probGroups = new ArrayList();

        //    double currentProb = 10.0;
        //    Hashtable group = new Hashtable();

        //    for (int i = 0; i < paths.Count; i++)
        //    {
        //        ArrayList path = (ArrayList)paths[i];
        //        double prob = (double)pathProbs[path];
        //        ArrayList wordsInPath = new ArrayList();
        //        Align.GetWordsInPath(path, ref wordsInPath);
        //        string words = GetWords(wordsInPath);
        //        int hashCode = words.GetHashCode();
        //        if (prob != currentProb && group.Count > 0)
        //        {
        //            probGroups.Add(group.Clone());
        //            group.Clear();
        //            group.Add(path, hashCode);
        //        }
        //        else
        //        {
        //            group.Add(path, hashCode);
        //        }

        //        currentProb = prob;
        //    }

        //    probGroups.Add(group);

        //    ArrayList paths2 = new ArrayList();

        //    foreach(Hashtable probGroup in probGroups)
        //    {
        //        ArrayList sortedPaths = Sort.SortTableIntDesc(probGroup);
        //        foreach(ArrayList sortedPath in sortedPaths)
        //        {
        //            paths2.Add(sortedPath);
        //        }
        //    }

        //    return paths2;
        //}

        public static ArrayList SortWordCandidates(Hashtable pathProbs)
        {
            ArrayList sortedCandidates = Sort.SortTableDoubleDesc(pathProbs);
            return SecondarySort2(sortedCandidates, pathProbs);
        }

        static public ArrayList SecondarySort2(ArrayList candidates, Hashtable pathProbs)
        {
            ArrayList probGroups = new ArrayList();

            double currentProb = 10.0;
            Hashtable group = new Hashtable();

            for (int i = 0; i < candidates.Count; i++)
            {
                TargetWord tWord = (TargetWord)candidates[i];
                double prob = (double)pathProbs[tWord];
                string word = tWord.Text + "-" + tWord.Position;
                int hashCode = word.GetHashCode();
                if (prob != currentProb && group.Count > 0)
                {
                    probGroups.Add(group.Clone());
                    group.Clear();
                    group.Add(tWord, hashCode);
                }
                else
                {
                    group.Add(tWord, hashCode);
                }

                currentProb = prob;
            }

            probGroups.Add(group);

            ArrayList paths2 = new ArrayList();

            foreach (Hashtable probGroup in probGroups)
            {
                //               ArrayList sortedPaths = Sort.SortTableIntDesc2(probGroup);
                ArrayList sortedPaths = Sort.TableToListByInt(probGroup, true);
                foreach (TargetWord tWord in sortedPaths)
                {
                    paths2.Add(tWord);
                }
            }

            return paths2;
        }

        static string GetWords(ArrayList wordsInPath)
        {
            string words = string.Empty;

            foreach(TargetWord w in wordsInPath)
            {
                words += w.Text + "-" + w.Position + " ";
            }

            return words.Trim();
        }

        public static Hashtable GetTranslationModel(string file)
        {
            Hashtable transModel = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split(" ".ToCharArray());
                if (groups.Length == 3)
                {
                    string source = groups[0].Trim();
                    string target = groups[1].Trim();
                    string sProb = groups[2].Trim();
                    double prob = Double.Parse(sProb);

                    if (transModel.ContainsKey(source))
                    {
                        Hashtable translations = (Hashtable)transModel[source];
                        translations.Add(target, prob);
                    }
                    else
                    {
                        Hashtable translations = new Hashtable();
                        translations.Add(target, prob);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }

        public static Dictionary<string, WordInfo> BuildWordInfoTable(XmlNode tree)
        {
            Dictionary<string, WordInfo> morphTable = new Dictionary<string, WordInfo>();

            ArrayList terminalNodes = Terminals.GetTerminalXmlNodes(tree);

            foreach(XmlNode terminalNode in terminalNodes)
            {
                WordInfo wi = new WordInfo();
                string id = Utils.GetAttribValue(terminalNode, "morphId");
                if (id.StartsWith("09020042"))
                {
                    ;
                }
                if (id.Length == 11) id += "1";
                wi.Surface = Utils.GetAttribValue(terminalNode, "Unicode");
                wi.Lemma = Utils.GetAttribValue(terminalNode, "UnicodeLemma");
                wi.Lang = Utils.GetAttribValue(terminalNode, "Language");
                wi.Morph = Utils.GetAttribValue(terminalNode, "Analysis");
                wi.Strong = Utils.GetAttribValue(terminalNode, "StrongNumberX");
                wi.Cat = Utils.GetAttribValue(terminalNode, "Cat");
                string type = string.Empty;
                if (wi.Lang == "G")
                {
                    type = Utils.GetAttribValue(terminalNode, "Type");
                }
                else
                {
                    type = Utils.GetAttribValue(terminalNode, "NounType");
                }
                if (wi.Cat == "noun" && type == "Proper") wi.Cat = "Name";

                morphTable.Add(id, wi);
            }

            return morphTable;
        }
    }

    public class WordInfo
    {
        public string Lang;
        public string Strong;
        public string Surface;
        public string Lemma;
        public string Cat;
        public string Morph;
    }

    public class Gloss
    {
        public string Gloss1;
        public string Gloss2;
    }
}
