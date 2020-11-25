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


using ClearBible.Clear3.Impl.Data;

using Gloss = ClearBible.Clear3.API.Gloss;
using GroupTranslationsTable = ClearBible.Clear3.API.GroupTranslationsTable;
using SourceLemmasAsText = ClearBible.Clear3.API.SourceLemmasAsText;
using PrimaryPosition = ClearBible.Clear3.API.PrimaryPosition;
using TargetGroupAsText = ClearBible.Clear3.API.TargetGroupAsText;
using Stats2 = DeadEndWip.Stats2;
using TranslationModel_Old = DeadEndWip.TranslationModel_Old;
using Translations = DeadEndWip.Translations;


namespace AlignmentTool
{
    public class Data
    {
        public static Dictionary<string, Gloss> BuildGlossTableFromFile(string glossFile)
        {
            Dictionary<string, Gloss> glossTable = new Dictionary<string, Gloss>();

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

        public static List<string> GetWordList(string file)
        {
            List<string> wordList = new List<string>();

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
        public static TranslationModel_Old GetTranslationModel(string file)
        {
            TranslationModel_Old transModel = new TranslationModel_Old();

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

                    if (transModel.ContainsSourceLemma(source))
                    {
                        Translations translations = transModel.TranslationsForSourceLemma(source);
                        translations.AddTranslation(target, prob);
                    }
                    else
                    {
                        Translations translations = new Translations();
                        translations.AddTranslation(target, prob);
                        transModel.AddTranslations(source, translations);
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
        public static Dictionary<string, Dictionary<string, Stats2>> GetTranslationModel2(string file)
        {
            Dictionary<string, Dictionary<string, Stats2>> transModel =
                new Dictionary<string, Dictionary<string, Stats2>>();

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
                    Stats2 s = new Stats2();
                    s.Count = Int32.Parse(sCount);
                    s.Prob = Double.Parse(sProb);

                    if (transModel.ContainsKey(source))
                    {
                        Dictionary<string, Stats2> translations = transModel[source];
                        translations.Add(target, s);
                    }
                    else
                    {
                        Dictionary<string, Stats2> translations = new Dictionary<string, Stats2>();
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
        public static Dictionary<string, double> GetAlignmentModel(string alignFile)
        {
            Dictionary<string, double> alignModel = new Dictionary<string, double>();

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
        public static GroupTranslationsTable_Old LoadGroups(string file)
        {
            GroupTranslationsTable_Old table = new GroupTranslationsTable_Old();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split("#".ToCharArray());
                if (groups.Length == 3)
                {
                    string source = groups[0].Trim();
                    GroupTranslation_Old tg = new GroupTranslation_Old();
                    tg.TargetGroupAsText = groups[1].Trim().ToLower();
                    tg.PrimaryPosition = Int32.Parse(groups[2].Trim());

                    if (table.ContainsSourceGroupKey(source))
                    {
                        GroupTranslations_Old targets = table.TranslationsForSourceGroup(source);
                        targets.Add(tg);
                    }
                    else
                    {
                        GroupTranslations_Old targets = new GroupTranslations_Old();
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
        public static Dictionary<string, int> GetXLinks(string file)
        {
            Dictionary<string, int> xLinks = new Dictionary<string, int>();

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

        public static List<string> GetStopWords(string file)
        {
            List<string> wordList = new List<string>();

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
        public static Dictionary<string, string> BuildPreAlignmentTable(Dictionary<string, double> alignModel)
        {
            Dictionary<string, string> preAlignedTable =
                new Dictionary<string, string>();

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

        public static Dictionary<string, Dictionary<string, int>> BuildStrongTable(string strongFile)
        {
            Dictionary<string, Dictionary<string, int>> strongTable =
                new Dictionary<string, Dictionary<string, int>>();

            string[] strongLines = File.ReadAllLines(strongFile);

            foreach (string strongLine in strongLines)
            {
                string[] items = strongLine.Split(" ".ToCharArray());

                string wordId = items[0].Trim();
                string strong = items[1].Trim();

                if (strongTable.ContainsKey(strong))
                {
                    Dictionary<String, int> wordIds = strongTable[strong];
                    wordIds.Add(wordId, 1);
                }
                else
                {
                    Dictionary<string, int> wordIds = new Dictionary<string, int>();
                    wordIds.Add(wordId, 1);
                    strongTable.Add(strong, wordIds);
                }
            }

            return strongTable;
        }


        public static void UpdateGroups(
            GroupTranslationsTable groups,
            int[] sourceLinks,
            int[] targetLinks,
            Manuscript manuscript,
            Translation translation)
        {
            SourceLemmasAsText source = new SourceLemmasAsText(
                String.Join(
                    " ",
                    sourceLinks.Select(link => manuscript.words[link].lemma))
                .Trim());

            int firstTargetLink = targetLinks[0];

            int[] sortedTargetLinks = targetLinks.OrderBy(x => x).ToArray();

            PrimaryPosition primaryPosition = new PrimaryPosition(
                sortedTargetLinks
                .Select((link, newIndex) => Tuple.Create(link, newIndex))
                .First(x => x.Item1 == firstTargetLink)
                .Item2);

            TargetGroupAsText targetGroupAsText = new TargetGroupAsText(
                sortedTargetLinks
                .Aggregate(
                    Tuple.Create(-1, string.Empty),
                    (state, targetLink) =>
                    {
                        int prevIndex = state.Item1;
                        string text = state.Item2;
                        string sep =
                          (prevIndex >= 0 && (targetLink - prevIndex) > 1)
                              ? " ~ "
                              : " ";
                        return Tuple.Create(
                            targetLink,
                            text + sep + translation.words[targetLink].text);
                    })
                .Item2
                .Trim()
                .ToLower());

            Dictionary<
                SourceLemmasAsText,
                HashSet<Tuple<TargetGroupAsText, PrimaryPosition>>>
                inner = groups.Dictionary;

            if (!inner.TryGetValue(source, out var targets))
            {
                targets = new HashSet<
                    Tuple<TargetGroupAsText, PrimaryPosition>> ();
                inner[source] = targets;
            }

            targets.Add(Tuple.Create(targetGroupAsText, primaryPosition));            
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
        public static Dictionary<string, Dictionary<string, string>> GetOldLinks(string jsonFile, GroupTranslationsTable groups)
        {
            Dictionary<string, Dictionary<string, string>> oldLinks =
                new Dictionary<string, Dictionary<string, string>>();

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
                        UpdateGroups(groups, sourceLinks, targetLinks, line.manuscript, line.translation);
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
                            Dictionary<string, string> verseLinks = oldLinks[verseID];
                            verseLinks.Add(mWord.altId, tWord.altId);
                        }
                        else
                        {
                            Dictionary<string, string> verseLinks = new Dictionary<string, string>();
                            verseLinks.Add(mWord.altId, tWord.altId);
                            oldLinks.Add(verseID, verseLinks);
                        }
                    }
                }
            }

            return oldLinks; 
        }

        public static void FilterOutFunctionWords(string file, string cwFile, List<string> funcWords)
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
