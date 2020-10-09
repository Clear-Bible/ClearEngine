using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using Utilities;
using GBI_Aligner;
using Newtonsoft.Json;

namespace AlignmentTool
{
    public class Data
    {
        public static Hashtable BuildGlossTableFromFile(string glossFile)
        {
            Hashtable glossTable = new Hashtable();

            string[] lines = File.ReadAllLines(glossFile);
            foreach (string line in lines)
            {
                string[] groups = line.Split("#".ToCharArray());

                if (groups.Length == 3)
                {
                    string morphID = groups[0].Trim();

                    Gloss g = new Gloss();
                    g.Gloss1 = groups[1].Trim();
                    g.Gloss2 = groups[2].Trim();

                    glossTable.Add(morphID, g);
                }
            }

            return glossTable;
        }

        public static ArrayList GetWordList(string file)
        {
            ArrayList wordList = new ArrayList();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                wordList.Add(line.Trim());
            }

            return wordList;
        }

        // Translation model file has lines of the form
        //   source target probability
        // Reading the data file produces a data structure of the form
        //   Hashtable(source => Hashtable(target => probability))
        //
        public static Dictionary<string, Dictionary<string, double>> GetTranslationModel(string file)
        {
            Dictionary<string, Dictionary<string, double>> transModel =
                new Dictionary<string, Dictionary<string, double>>();

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
                        Dictionary<string, double> translations = transModel[source];
                        translations.Add(target, prob);
                    }
                    else
                    {
                        Dictionary<string, double> translations = new Dictionary<string, double>();
                        translations.Add(target, prob);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }

        // Used for reading the manTransModel file.
        // Input file has lines of the form
        //   source target count probability
        // Reading the file produces a data structure of the form
        //   Hashtable(source => Hashtable(target => Stats{count, probability})
        //
        public static Dictionary<string, Dictionary<string, Stats>> GetTranslationModel2(string file)
        {
            Dictionary<string, Dictionary<string, Stats>> transModel =
                new Dictionary<string, Dictionary<string, Stats>>();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split(" ".ToCharArray());
                if (groups.Length == 4)
                {
                    string source = groups[0].Trim();
                    string target = groups[1].Trim();
                    string sCount = groups[2].Trim();
                    string sProb = groups[3].Trim();
                    Stats s = new Stats();
                    s.Count = Int32.Parse(sCount);
                    s.Prob = Double.Parse(sProb);

                    if (transModel.ContainsKey(source))
                    {
                        Dictionary<string, Stats> translations = transModel[source];
                        translations.Add(target, s);
                    }
                    else
                    {
                        Dictionary<string, Stats> translations = new Dictionary<string, Stats>();
                        translations.Add(target, s);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }

        // Input file has lines of the form:
        //   pair probability
        // Reading the file produces a data structure of the form
        //   Hashtable(pair => probability)
        //
        public static Hashtable GetAlignmentModel(string alignFile)
        {
            Hashtable alignModel = new Hashtable();

            string[] lines = File.ReadAllLines(alignFile);
            foreach (string line in lines)
            {
                string[] groups = line.Split(" ".ToCharArray());
                if (groups.Length == 2)
                {
                    string pair = groups[0];
                    double prob = Double.Parse(groups[1]);
                    alignModel.Add(pair, prob);
                }
            }

            return alignModel;
        }


        // Input file has lines of the form
        //    ...source... # ...text... # primaryPosition
        //    for example:  ...<greek words>... # anyone who # 0 
        // Output datum is of the form
        //   Hashtable(...source... => ArrayList(TargetGroup{...text..., primaryPosition}))
        //
        public static Hashtable LoadGroups(string file)
        {
            Hashtable table = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split("#".ToCharArray());
                if (groups.Length == 3)
                {
                    string source = groups[0].Trim();
                    TargetGroup tg = new TargetGroup();
                    tg.Text = groups[1].Trim().ToLower();
                    tg.PrimaryPosition = Int32.Parse(groups[2].Trim());

                    if (table.ContainsKey(source))
                    {
                        ArrayList targets = (ArrayList)table[source];
                        targets.Add(tg);
                    }
                    else
                    {
                        ArrayList targets = new ArrayList();
                        targets.Add(tg);
                        table.Add(source, targets);
                    }
                }
            }

            return table;
        }

        public static Hashtable GetSimilarPhrases(string similarphraseFile)
        {
            Hashtable similarPhrases = new Hashtable();

            using (StreamReader sr = new StreamReader(similarphraseFile, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    string phrase = line.Substring(0, line.IndexOf(" # "));
                    string sPhrases = line.Substring(line.IndexOf(" # ") + 1);
                    ArrayList sPhraseList = GetsPhraseList(sPhrases);
                    similarPhrases.Add(phrase, sPhraseList);
                }
            }

            return similarPhrases;
        }

        static ArrayList GetsPhraseList(string sPhrases)
        {
            ArrayList phraseList = new ArrayList();

            string[] phrases = sPhrases.Split("#".ToCharArray());

            for (int i = 0; i < phrases.Length; i++)
            {
                string phrase = phrases[i].Trim();
                if (phrase != string.Empty)
                {
                    phraseList.Add(phrase);
                }
            }

            return phraseList;
        }

        // This function is used to build the tm.  (translation memory?)
        // Input file has lines of the form:
        //   source # target # count
        // Output datum has the form
        //   Hashtable(source => ArrayList(TargetTrans{ target, count }))
        //
        public static Hashtable BuildTable(string phraseFile)
        {
            Hashtable table = new Hashtable();

            string[] lines = File.ReadAllLines(phraseFile);
            foreach (string line in lines)
            {
                string[] groups = line.Split("#".ToCharArray());
                if (groups.Length == 3)
                {
                    string source = groups[0].Trim();
                    string target = groups[1].Trim();
                    string sCount = groups[2].Trim();
                    int count = Int32.Parse(sCount);

                    TargetTrans trans = new TargetTrans();  //  TargetTrans { string Text; int Count }
                    trans.Text = target;
                    trans.Count = count;

                    if (table.ContainsKey(source))
                    {
                        ArrayList translations = (ArrayList)table[source];
                        translations.Add(trans);
                    }
                    else
                    {
                        ArrayList translations = new ArrayList();
                        translations.Add(trans);
                        table.Add(source, translations);
                    }
                }
            }

            return table;
        }

        // Input file has lines of the form:
        //    ...phrase... # ...xxx...
        // Result is a datum of the form
        //     Hashtable(...phrase... => ...xxx...)
        //
        public static Hashtable GetFreqPhrases(string file)
        {
            Hashtable freqPhrases = new Hashtable();

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    string phrase = line.Substring(0, line.IndexOf("#") - 1);
                    if (!freqPhrases.ContainsKey(phrase))
                    {
                        freqPhrases.Add(phrase, line.Substring(line.IndexOf("#") + 2));
                    }
                }
            }

            return freqPhrases;
        }

        // Input file has lines of the form:
        //   link count
        // Output datum is of the form
        //   Hashtable(link => count)
        //
        public static Hashtable GetXLinks(string file)
        {
            Hashtable xLinks = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split(" ".ToCharArray());
                if (groups.Length == 2)
                {
                    string badLink = groups[0].Trim();
                    int count = Int32.Parse(groups[1]);
                    xLinks.Add(badLink, count);
                }
            }

            return xLinks;
        }

        public static ArrayList GetStopWords(string file)
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

        // Input is of the form: Hashtable(pair => probability)
        //   where pair is a string of the form ...src...-...tgt...
        // Output is of the form:
        //   Hashtable(...src... => ...tgt...)
        // If there are multiple possibilities for ...src... we take
        // the first one encountered.
        //
        public static Hashtable BuildPreAlignmentTable(Hashtable alignModel)
        {
            Hashtable preAlignedTable = new Hashtable();

            IDictionaryEnumerator modelEnum = alignModel.GetEnumerator();
            while (modelEnum.MoveNext())
            {
                string link = (string)modelEnum.Key;
                string sourceID = link.Substring(0, link.IndexOf("-"));
                string targetID = link.Substring(link.IndexOf("-") + 1);
                if (!preAlignedTable.ContainsKey(sourceID))
                {
                    preAlignedTable.Add(sourceID, targetID);
                }
            }

            return preAlignedTable;
        }

        public static Hashtable BuildStrongTable(string strongFile)
        {
            Hashtable strongTable = new Hashtable();

            string[] strongLines = File.ReadAllLines(strongFile);

            foreach (string strongLine in strongLines)
            {
                string[] items = strongLine.Split(" ".ToCharArray());

                string wordId = items[0].Trim();
                string strong = items[1].Trim();

                if (strongTable.ContainsKey(strong))
                {
                    Hashtable wordIds = (Hashtable)strongTable[strong];
                    wordIds.Add(wordId, 1);
                }
                else
                {
                    Hashtable wordIds = new Hashtable
                    {
                        { wordId, 1 }
                    };
                    strongTable.Add(strong, wordIds);
                }
            }

            return strongTable;
        }

        public static void UpdateGroups(ref Hashtable groups, int[] sourceLinks, int[] targetLinks, Manuscript manuscript, Translation translation)
        {
            string sourceText = GetSourceText(sourceLinks, manuscript);
            TargetGroup targetGroup = GetTargetText(targetLinks, translation);

            if (groups.ContainsKey(sourceText))
            {
                ArrayList translations = (ArrayList)groups[sourceText];
                if (!HasGroup(translations, targetGroup))
                {
                    translations.Add(targetGroup);
                }
            }
            else
            {
                ArrayList translations = new ArrayList();
                translations.Add(targetGroup);
                groups.Add(sourceText, translations);
            }
        }

        public static bool HasGroup(ArrayList translations, TargetGroup targetGroup)
        {
            bool hasGroup = false;

            foreach (TargetGroup tg in translations)
            {
                if (tg.Text == targetGroup.Text)
                {
                    hasGroup = true;
                    break;
                }
            }

            return hasGroup;
        }

        static string GetSourceText(int[] sourceLinks, Manuscript manuscript)
        {
            string text = string.Empty;

            for (int i = 0; i < sourceLinks.Length; i++)
            {
                int sourceLink = sourceLinks[i];
                string lemma = manuscript.words[sourceLink].lemma;
                text += lemma + " ";
            }

            return text.Trim();
        }

        static TargetGroup GetTargetText(int[] targetLinks, Translation translation)
        {
            string text = string.Empty;
            int primaryIndex = targetLinks[0];
            Array.Sort(targetLinks);

            TargetGroup tg = new TargetGroup();
            tg.PrimaryPosition = GetPrimaryPosition(primaryIndex, targetLinks);

            int prevIndex = -1;
            for (int i = 0; i < targetLinks.Length; i++)
            {
                int targetLink = targetLinks[i];
                string word = string.Empty;
                if (prevIndex >= 0 && (targetLink - prevIndex) > 1)
                {
                    word = "~ " + translation.words[targetLink].text;
                }
                else
                {
                    word = translation.words[targetLink].text;
                }
                tg.Text += word + " ";
                prevIndex = targetLink;
            }

            tg.Text = tg.Text.Trim().ToLower();

            return tg;
        }

        static int GetPrimaryPosition(int primaryIndex, int[] targetLinks)
        {
            int primaryPosition = 0;

            for (int i = 0; i < targetLinks.Length; i++)
            {
                if (primaryIndex == targetLinks[i])
                {
                    primaryPosition = i;
                    break;
                }
            }

            return primaryPosition;
        }

        // public class Line
        //{
        //    public Manuscript manuscript;
        //    public Translation translation;
        //
        //    //public int[][][] links;
        //    [JsonConverter(typeof(LinkJsonConverter))]
        //    public List<Link> links;
        //}
        //public class Manuscript
        //{
        //    public ManuscriptWord[] words;
        //}
        //public class ManuscriptWord
        //{
        //    public long id;
        //    public string altId;
        //    public string text;
        //    public string strong;
        //    public string gloss;
        //    public string gloss2;
        //    public string lemma;
        //    public string pos;
        //    public string morph;
        //}
        //public class Translation
        //{
        //    public TranslationWord[] words;
        //}
        //public class TranslationWord
        //{
        //    public long id;
        //    public string altId;
        //    public string text;
        //}
        //public class Link
        //{
        //    public int[] source;
        //    public int[] target;
        //    public double? cscore;
        //}
        // 
        //
        // Deserializes the JSON in the input file into a Line[] with
        // types as above.
        // Returns a datum of the form:
        //   Hashtable(verseId => Hashtable(manuscriptWord.AltId => translationWord.AltId))
        //   where the verseId is the first 8 characters of the manuscriptWord.Id
        //   and the entries come from the one-to-one links
        //
        // In addition, this routine calls UpdateGroups() whenever it encounters
        // a link that is not one-to-one.
        //
        public static Hashtable GetOldLinks(string jsonFile, ref Hashtable groups)
        {
            Hashtable oldLinks = new Hashtable();

            string jsonText = File.ReadAllText(jsonFile);
            Line[] lines = JsonConvert.DeserializeObject<Line[]>(jsonText);
            if (lines == null) return oldLinks;

            for (int i = 0; i < lines.Length; i++)
            {
                Line line = lines[i];

                for (int j = 0; j < line.links.Count; j++)
                {
                    Link link = line.links[j];
                    int[] sourceLinks = link.source;
                    int[] targetLinks = link.target;

                    if (sourceLinks.Length > 1 || targetLinks.Length > 1)
                    {
                        UpdateGroups(ref groups, sourceLinks, targetLinks, line.manuscript, line.translation);
                    }
                    else
                    {
                        int sourceLink = sourceLinks[0];
                        int targetLink = targetLinks[0];
                        ManuscriptWord mWord = line.manuscript.words[sourceLink];
                        TranslationWord tWord = line.translation.words[targetLink];

                        string verseID = mWord.id.ToString().PadLeft(12, '0').Substring(0, 8);

                        if (oldLinks.ContainsKey(verseID))
                        {
                            Hashtable verseLinks = (Hashtable)oldLinks[verseID];
                            verseLinks.Add(mWord.altId, tWord.altId);
                        }
                        else
                        {
                            Hashtable verseLinks = new Hashtable();
                            verseLinks.Add(mWord.altId, tWord.altId);
                            oldLinks.Add(verseID, verseLinks);
                        }
                    }
                }
            }

            return oldLinks;  // Hashtable(verseID => Hashtable(mWord.altId => tWord.altId))
        }

        public static void FilterOutFunctionWords(string file, string cwFile, ArrayList funcWords)
        {
            StreamWriter sw = new StreamWriter(cwFile, false, Encoding.UTF8);

            string[] lines = File.ReadAllLines(file);

            foreach(string line in lines)
            {
                string cwLine = string.Empty;
                string[] words = line.Split(" ".ToArray());
                foreach(string word in words)
                {
                    if (word.Contains("_"))
                    {
                        string w = word.Substring(0, word.LastIndexOf("_"));
                        if (!funcWords.Contains(w))
                        {
                            cwLine += word + " ";
                        }
                    }
                    else if (!funcWords.Contains(word))
                    {
                        cwLine += word + " ";
                    }
                }
                sw.WriteLine(cwLine.Trim());
            }

            sw.Close();
        }
    }
}
