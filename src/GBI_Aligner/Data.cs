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
        public static List<CandidateChain> SortPaths(Dictionary<CandidateChain, double> pathProbs)
        {
            int hashCodeOfWordsInPath(CandidateChain path) =>
                Align.GetTargetWordsInPath(path).GetHashCode();

            return pathProbs
                .OrderByDescending(kvp => kvp.Value)
                .ThenByDescending(kvp =>
                    hashCodeOfWordsInPath(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();
        }



        // pathProbs ::= TargetWord => score
        public static List<TargetWord> SortWordCandidates(Dictionary<TargetWord, double> pathProbs)
        {
            int hashCodeOfWordAndPosition(TargetWord tw) =>
                $"{tw.Text}-{tw.Position}".GetHashCode();

            return
                pathProbs
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenByDescending(kvp =>
                        hashCodeOfWordAndPosition(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();           
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

            List<XmlNode> terminalNodes = Terminals.GetTerminalXmlNodes(tree);

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
