using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

using Utilities;
using Trees;
using Newtonsoft.Json;
using TransModels;
using Tokenizer;
using ParallelFiles;

namespace GBI_Aligner
{
    public class AutoAligner
    {
        // User story #1: Align two parallel files of one or more verses
//        public static string AutoAlign2(string rawText, string oldJson, )
        public static void AutoAlign(
            string source,  // name of file with source IDs
            string sourceLemma,  // name of file with source lemma IDs
            string target, // name of tokens.txt file, after alignment
            string jsonOutput, // output of aligner, alignment.json file
            Dictionary<string, Dictionary<string, double>> transModel, // source => target => probability
            Hashtable manTransModel, // translation model from manually checked alignments
                                     // comes from Data.GetTranslationModel2(manTransModelFile)
                                     // of the form: Hashtable(source => Hashtable(target => Stats{ count, probability})
                                     // source = strongs, target = lower-cased translated text
            string treeFolder, // the folder where syntatic trees are kept.
            Hashtable bookNames, // for getting booknames that are used in the tree files
            Hashtable alignProbs, // alignment probabilities
                                  // comes from Data.GetAlignmentModel(alignModel.txt)
                                  //   Hashtable(pair => probability)
                                  //   the pair is a string of the form: bbcccvvvwwwn-bbcccvvvwww 
                                  //      for example: 400010010011-40001001001 
            Hashtable preAlignment, // alignments from the decoder of the statisical aligner
                                    // comes from Data.BuildPreAlignmentTable(alignProbs)
                                    //   of the form Hashtable(bbcccvvvwwwn => bbcccvvvwww)
            bool useAlignModel, // use the alignProbs and preAlignment only in batch mode where the verses 
                                // to be aligned are the same as the verses used in building the models
            int maxPaths, // the maximal number paths we can keep at any point
            ArrayList puncs, // list of punctuation marks
            Hashtable groups, // one-to-many, many-to-one, and many-to-many mappings
                              // comes from Data.LoadGroups("groups.txt")
                              //   of the form Hashtable(...source... => ArrayList(TargetGroup{...text..., primaryPosition}))
            ArrayList stopWords, // target words not to be linked
            Hashtable goodLinks, // list of word pairs that should be linked
                                 // from Data.GetXLinks("goodLinks.txt")
                                 //   of the form Hashtable(link => count)
            int goodLinkMinCount, // the mininmal counts required for a good link to be used
            Hashtable badLinks, // list of word pairs that should not be linked, also Hashtable(link => count)
            int badLinkMinCount, // the mininmal counts required for a bad link to be considered
            Hashtable glossTable, // gloss information of the source text 
            Hashtable oldLinks, // Hashtable(verseID => Hashtable(mWord.altId => tWord.altId))
            ArrayList sourceFuncWords, // function words in Hebrew and Greek
            ArrayList targetFuncWords,
            bool contentWordsOnly,
            Hashtable strongs
            )
        {
 //           Hashtable oldLinks = OldLinks.GetOldLinks(oldJson, ref groups);

//            Alignment2 align = Align.AlignCorpus(source, sourceLemma, target, targetLower, transModel, manTransModel, alignProbs, preAlignment, useAlignModel, groups, treeFolder, bookNames, jsonOutput, 1000000, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, glossTable, oldLinks, contentWordsOnly, strongs);
            Alignment2 align = Align.AlignCorpus(source, sourceLemma, target, transModel, manTransModel, alignProbs, preAlignment, useAlignModel, groups, treeFolder, bookNames, jsonOutput, 1000000, puncs, stopWords, goodLinks, goodLinkMinCount, badLinks, badLinkMinCount, glossTable, oldLinks, sourceFuncWords, targetFuncWords, contentWordsOnly, strongs);

            // Create JSON file
            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }

        // User story #2: use the existing alignments to update the translation model and translation memory
        public static void IncrementalUpdate2(
            string jsonFile, // existing (manually checked) alignment in JSON
            ref Hashtable groups, // many-to-many links
            string groupFile, // the text file that contains group information
            ref Hashtable manTransModel, // translation model
            string manTransModelFile, // the text file that contains the translation model
            string tmFile, // the text file that contains the translation memory
            ref Hashtable tm, // translation memory
            Hashtable freqPhrases, // phrases that occur more than once
            string treeFolder, // the folder where syntactic trees are stored           
            ref Hashtable goodLinks, // links to functional words that are otherwise ignored
            string goodLinkFile, // the text file that contains good links
            ArrayList sourceFuncWords
            )
        {
            string jsonText = File.ReadAllText(jsonFile);
            Line[] lines = JsonConvert.DeserializeObject<Line[]>(jsonText);

            Hashtable wordLinks = new Hashtable();

            bool isOne2One = true;

            for (int i = 0; i < lines.Length; i++)
            {
                Line line = lines[i];
                for (int j = 0; j < line.links.Count; j++)
                {
                    Link link = line.links[j];
                    int[] sourceLinks = link.source;
                    int[] targetLinks = link.target;

                    // Updating groups with many-to-many links
                    if (sourceLinks.Length > 1 || targetLinks.Length > 1)
                    {
                        isOne2One = false;
                        UpdateGroups(ref groups, sourceLinks, targetLinks, line.manuscript, line.translation);
                    }
                    else
                    {
                        isOne2One = true;
                    }

                    if (sourceLinks.Length > 1) // Many to one
                    {
                        ManuscriptWord[] mWords = GetManuscriptWords(sourceLinks, line.manuscript.words);
                        ManuscriptWord primaryMWord = Patterns.GetPrimaryWord(mWords);
                        if (primaryMWord != null) // if the source fits certain patterns where there is a primary word that is equivalent to the target
                        {
                            TranslationWord[] tWords = GetTranslationWords(targetLinks, line.translation.words);
                            UpdateTM2(primaryMWord, tWords, ref tm);
                        }
                    }

                    for (int k = 0; k < sourceLinks.Length; k++)
                    {
                        int sourceLink = sourceLinks[k];

                        for (int l = 0; l < targetLinks.Length; l++)
                        {
                            int targetLink = targetLinks[l];
                            ManuscriptWord mWord = line.manuscript.words[sourceLink];
                            TranslationWord tWord = line.translation.words[targetLink];
                            if (isOne2One)
                            {
                                UpdateManTransModel(mWord.lemma, tWord.text.ToLower(), ref manTransModel);
                                //                           UpdateTransModel(mWord.lemma, tWord.text.ToLower(), ref transModel);
                                UpdateGoodLinks(mWord, tWord.text.ToLower(), ref goodLinks, sourceFuncWords);
                                UpdateTM(mWord.strong, tWord.text.ToLower(), ref tm, freqPhrases);
                            }
                            if (wordLinks.ContainsKey(mWord.id.ToString().PadLeft(12, '0')))
                            {
                                ArrayList tLinks = (ArrayList)wordLinks[mWord.id.ToString().PadLeft(12, '0')];
                                tLinks.Add(tWord.id.ToString().PadLeft(11, '0'));
                            }
                            else
                            {
                                ArrayList tLinks = new ArrayList();
                                tLinks.Add(tWord.id.ToString().PadLeft(11, '0'));
                                wordLinks.Add(mWord.id.ToString().PadLeft(12, '0'), tLinks);
                            }
                        }
                    }
                }

                UpdatePhraseTM(line, wordLinks, treeFolder, freqPhrases, ref tm);
            }



            // Write the models to files
            WriteToFile1(manTransModel, manTransModelFile);
            WriteToFile2(tm, tmFile);
            WriteToFile3(groups, groupFile);
            WriteToFile4(goodLinks, goodLinkFile);
        }

        // Translation model: source ID => (target ID => { count, probability })
        // Translation memory: source lemma => [(target lemma, count), ...]

        // User story #3: Rebuild the translation model, the alignment model, and the translation memory using the translations that have been produced so far plus any existing translations from a different version
        public static void GlobalUpdate2(
            string jsonFile, // all the manually checked alignments from the new translation
            ref Hashtable manTransModel, // "Manual translation model" created from manually checked alignments
                                         // of the form: Hashtable(source => Hashtable(target => Stats{ count, probability})
            string manTransModelFile, // the text file that contains MAN-TM
            string groupFile, // the file containing groups, to be rebuilt
            ref Hashtable groups,  // of the form Hashtable(...source... => ArrayList(TargetGroup{...text..., primaryPosition}))
            ref Hashtable goodLinks, // of the form Hashtable(link => count)
                                     //   link is of the form s#t where s = source text and t = lower-cased target text
            string goodLinkFile,
            ref Hashtable tm, // I think tm means translation memory, Andi says it is different than translation model
                              // Hashtable(strong => ArrayList(TargetTrans{ translationText, count }))
                              // also known as wordTM, also known as phraseTM
            string tmFile, // text file that contains the translation memory; to be loaded into the transModel Hashtable when the system starts
            Hashtable freqPhrases,
            string treeFolder,
            ArrayList sourceFuncWords // (uses the lemma)
            )
        {
            string jsonText = File.ReadAllText(jsonFile);
            Line[] lines = JsonConvert.DeserializeObject<Line[]>(jsonText);

            // Line { manuscript { ManuscriptWord[] words }; translation { TranslationWord[] words }; List<Link> links }
            // ManuscriptWord { long id; string altId; string text; string lemma; ... }
            // TranslationWord { long id; string altId; string text; }
            // public class Link { int[] source; int[] target; double? cscore; }

            tm.Clear();
            manTransModel.Clear();
            groups.Clear();
            goodLinks.Clear();

            Hashtable wordLinks = new Hashtable();  // Hashtable(s => ArrayList(t))
                                                    // where s is the 12-character source word ID
                                                    // and t is the 11-character target word ID


            bool isOne2One = true;

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
                        isOne2One = false;
                        UpdateGroups(ref groups, sourceLinks, targetLinks, line.manuscript, line.translation);
                    }
                    else
                    {
                        isOne2One = true;
                    }

                    if (sourceLinks.Length > 1) // Many to one
                    {
                        ManuscriptWord[] mWords = GetManuscriptWords(sourceLinks, line.manuscript.words);
                        ManuscriptWord primaryMWord = Patterns.GetPrimaryWord(mWords); // (depends on the parts of speech)
                        if (primaryMWord != null) // if the source fits certain patterns where there is a primary word that is equivalent to the target
                        {
                            TranslationWord[] tWords = GetTranslationWords(targetLinks, line.translation.words);
                            UpdateTM2(primaryMWord, tWords, ref tm);
                        }
                    }

                    for (int k = 0; k < sourceLinks.Length; k++)
                    {
                        int sourceLink = sourceLinks[k];

                        for (int l = 0; l < targetLinks.Length; l++)
                        {
                            int targetLink = targetLinks[l];
                            ManuscriptWord mWord = line.manuscript.words[sourceLink];
                            TranslationWord tWord = line.translation.words[targetLink];
                            if (isOne2One)
                            {
                                UpdateManTransModel(mWord.lemma, tWord.text.ToLower(), ref manTransModel);
                                //                           UpdateTransModel(mWord.lemma, tWord.text.ToLower(), ref transModel);
                                UpdateGoodLinks(mWord, tWord.text.ToLower(), ref goodLinks, sourceFuncWords);
                                UpdateTM(mWord.strong, tWord.text.ToLower(), ref tm, freqPhrases); // freqPhrases not actually used
                            }
                            if (wordLinks.ContainsKey(mWord.id.ToString().PadLeft(12, '0')))
                            {
                                ArrayList tLinks = (ArrayList)wordLinks[mWord.id.ToString().PadLeft(12, '0')];
                                tLinks.Add(tWord.id.ToString().PadLeft(11, '0'));
                            }
                            else
                            {
                                ArrayList tLinks = new ArrayList();
                                tLinks.Add(tWord.id.ToString().PadLeft(11, '0'));
                                wordLinks.Add(mWord.id.ToString().PadLeft(12, '0'), tLinks);
                            }
                        }
                    }
                }

                UpdatePhraseTM(line, wordLinks, treeFolder, freqPhrases, ref tm);
            }

            // Write the models to files
            WriteToFile1(manTransModel, manTransModelFile);
            WriteToFile2(tm, tmFile);
            WriteToFile3(groups, groupFile);
            WriteToFile4(goodLinks, goodLinkFile);
        }

 

        // Part of User story #4
        // Align target translation to gateway translation through the alignment between manuscript and target translation
        public static void AlignG2TviaM2G(
            string m2tJsonFile, // alignment between manuscript and target translation, from auto aligner
            string m2gJsonFile, // alignment between manuscript and gateway translation
            string g2tJsonFile // output file
                               // alignment between gateway translation and target translation
                               // serialization of an Alignment3 
            )
        {
            string m2tJsonText = File.ReadAllText(m2tJsonFile);
            Line[] m2tLines = JsonConvert.DeserializeObject<Line[]>(m2tJsonText); // Manuscript to gateway from auto aligner
            string m2gJsonText = File.ReadAllText(m2gJsonFile);
            Line3[] m2gLines = JsonConvert.DeserializeObject<Line3[]>(m2gJsonText); // Manuscript to gateway, Line3[]

            ArrayList  m2tLinks = Links.Getm2tLinks(m2tLines);  // m2tLinks = ArrayList("mid-tid", ...)
            Links.Linkg2tViaM2T(m2gLines, m2tLines, m2tLinks, g2tJsonFile);
        }

        // User Story #5
        // Align target to manuscript through the checked gateway-target alignment
        // g2tJsonText = json for checked links between gateway and target (CLEAR format = Line3[])
        //               from the links member
        // auto_m_t_text = unchecked auto alignment (Line[])
        //
        // returns JSON for an Alignment2 that aligns manuscript directly to target
        // without the gateway.  It's the alignment from g2tJsonText, supplemented by
        // the auto alignment in auto_m_t_text for those words that are not linked
        // in the gateway.
        //
        public static string AlignM2TviaM2G(string g2tJsonText, string auto_m_t_text)
        {
            Line3[] m2gLines = JsonConvert.DeserializeObject<Line3[]>(g2tJsonText);
            Line[] auto_m_t_lines = JsonConvert.DeserializeObject<Line[]>(auto_m_t_text);

            ArrayList g2tLinks = Links.Getg2tLinks(m2gLines);  // ArrayList("gid-tid", ...), goes through links not glinks
            ArrayList auto_m_t_links = Links.Getm2tLinks(auto_m_t_lines); // ArrayList("mid-tid", ...)
            Hashtable m2tLinks = Links.Getm2tLinks(m2gLines, g2tLinks, auto_m_t_links);
            // Hashtable("mid-tid" => int)
            // auto_m_t_links is used to supplement the mapping for manuscript
            // words that occur in the auto-alignment but are not mapped to anything
            // in the gateway language

            ArrayList m2tGroups = Links.Getm2tGroups(m2gLines, m2tLinks);
            // m2tGroups = ArrayList(Group2, ...) where the Group2's are
            // relating manuscript segments to target segments

            Alignment2 align = new Alignment2();
            align.Lines = new Line[m2gLines.Length];

            for (int i = 0; i < m2gLines.Length; i++)
            {
                Line3 line3 = m2gLines[i];
                Line line = new Line();
                line.manuscript = line3.manuscript;
                line.translation = line3.translation;

                Hashtable mPositionTable = Links.BuildPositionTable(line3.manuscript);
                Hashtable tPositionTable = Links.BuildPositionTable(line3.translation);

                List<Link> links = new List<Link>();

                for (int j = 0; j < m2tGroups.Count; j++)
                {
                    Group2 g = (Group2)m2tGroups[j];

                    int[] s = new int[g.mSegments.Count];
                    for (int m = 0; m < g.mSegments.Count; m++)
                    {
                        string sourceWord = (string)g.mSegments[m];
                        int sPosition = (int)mPositionTable[sourceWord];
                        s[m] = sPosition;
                    }
                    int[] t = new int[g.tSegments.Count];
                    for (int n = 0; n < g.tSegments.Count; n++)
                    {
                        string targetWord = (string)g.tSegments[n];
                        if (tPositionTable.ContainsKey(targetWord))
                        {
                            int tPosition = (int)tPositionTable[targetWord];
                            t[n] = tPosition;
                        }
                        else
                        {
                            continue; // This is not supposed to happen, but there can be some exceptions in the databases.
                        }
                    }

                    links.Add(new Link(){ source = s, target = t, cscore = 0.1});//dummy cscore

                }

                line.links = links;
                align.Lines[i] = line;
            }

            string json = JsonConvert.SerializeObject(align.Lines, Newtonsoft.Json.Formatting.Indented);
            return json;
        }

        public static void AlignM2TviaM2G(
            string gAlignment, // json in CLEAR format which includes the checked links between gateway and target
            string auto_m_t_alignment, // unchecked auto alignment
            string jsonOutput // alignment between manuscript and target
            )
        {
            string g2tJsonText = File.ReadAllText(gAlignment);
            string auto_m_t_text = File.ReadAllText(auto_m_t_alignment);
            string json = AlignM2TviaM2G(g2tJsonText, auto_m_t_text);
            
            File.WriteAllText(jsonOutput, json);
        }



        public static void WriteToFile1(Hashtable model, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            IDictionaryEnumerator modelEnum = model.GetEnumerator();

            while (modelEnum.MoveNext())
            {
                string source = (string)modelEnum.Key;
                Hashtable translations = (Hashtable)modelEnum.Value;

                IDictionaryEnumerator transEnum = translations.GetEnumerator();

                while (transEnum.MoveNext())
                {
                    string translation = (string)transEnum.Key;
                    Stats s = (Stats)transEnum.Value;
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", source, translation, s.Count, s.Prob));
                }
            }

            sw.Close();
        }

        static void WriteToFile2(Hashtable model, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            IDictionaryEnumerator modelEnum = model.GetEnumerator();

            while (modelEnum.MoveNext())
            {
                string source = (string)modelEnum.Key;
                ArrayList translations = (ArrayList)modelEnum.Value;

                foreach(TargetTrans translation in translations)
                {
 //                   sw.WriteLine("{0} # {1} # {2}", source, translation.Text, translation.Count);
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} # {1} # {2}", source, translation.Text, translation.Count));
                }
            }

            sw.Close();
        }

        static void WriteToFile3(Hashtable model, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            IDictionaryEnumerator modelEnum = model.GetEnumerator();

            while (modelEnum.MoveNext())
            {
                string source = (string)modelEnum.Key;
                ArrayList translations = (ArrayList)modelEnum.Value;
                foreach (TargetGroup translation in translations)
                {
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} # {1} # {2}", source, translation.Text, translation.PrimaryPosition));
                }
            }

            sw.Close();
        }

        static void WriteToFile4(Hashtable table, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);

            IDictionaryEnumerator tableEnum = table.GetEnumerator();

            while (tableEnum.MoveNext())
            {
                string badLink = (string)tableEnum.Key;
                int count = (int)tableEnum.Value;
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}", badLink, count));
            }

            sw.Close();
        }


        // source = lemma
        // target = lower-cased text of translation 
        //
        static void UpdateManTransModel(string source, string target, ref Hashtable manTransModel)
        {
            if (manTransModel.ContainsKey(source))
            {
                Hashtable translations = (Hashtable)manTransModel[source];
                if (translations.ContainsKey(target))
                {
                    Stats s = (Stats)translations[target];
                    s.Count = s.Count + 1;
                    translations[target] = s;
                }
                else
                {
                    Stats s = new Stats();
                    s.Count = 1;
                    s.Prob = 1.0;
                    translations.Add(target, s);
                }

                int totalCount = GetTotalCounts(translations);
                IDictionaryEnumerator transEnum = translations.GetEnumerator();

                Hashtable transProbs = new Hashtable();
                while (transEnum.MoveNext())
                {
                    string translation = (string)transEnum.Key;
                    Stats s = (Stats)transEnum.Value;
                    s.Prob = (double)s.Count / (double)totalCount;
                    transProbs.Add(translation, s);
                }

                manTransModel[source] = transProbs;
            }
            else
            {
                Hashtable translations = new Hashtable();
                Stats s = new Stats();
                s.Count = 1;
                s.Prob = 1.0;
                translations.Add(target, s);
                manTransModel.Add(source, translations);
            }
        }

        // goodLinks is of the form Hashtable(link => count)
        //   where link is made out of mWord.text + '#' + tText
        //   tText was the lower-cased translated text
        //
        static void UpdateGoodLinks(ManuscriptWord mWord, string tText, ref Hashtable goodLinks, ArrayList sourceFuncWords)
        {
            if (Align.IsContentWord(mWord.lemma, sourceFuncWords)) return;

            string link = mWord.text + "#" + tText;

            if (goodLinks.ContainsKey(link))
            {
                int count = (int)goodLinks[link];
                goodLinks[link] = count + 1;
            }
            else
            {
                goodLinks.Add(link, 1);
            }
        }


        // source is strongs for lemma of source word
        // target is string of translated text, might have multiple words
        // wordTM is Hashtable(strong => ArrayList(TargetTrans{ translationText, count }))
        //
        static void UpdateTM(string source, string target, ref Hashtable wordTM, Hashtable freqPhrases)
        {
//            if (source.Contains(" ") && !freqPhrases.ContainsKey(source)) return;

            if (wordTM.ContainsKey(source))
            {
                ArrayList translations = (ArrayList)wordTM[source];

                bool isNewTarget = true;

                for (int i = 0; i < translations.Count; i++)
                {
                    TargetTrans t = (TargetTrans)translations[i];
                    if (t.Text == target)
                    {
                        t.Count = t.Count + 1;
                        isNewTarget = false;
                        break;
                    }
                }
                
                if (isNewTarget)
                {
                    TargetTrans t = new TargetTrans();
                    t.Text = target;
                    t.Count = 1;
                    translations.Add(t);
                }
            }
            else
            {
                ArrayList translations = new ArrayList();
                TargetTrans t = new TargetTrans();
                t.Text = target;
                t.Count = 1;
                translations.Add(t);
                wordTM.Add(source, translations);
            }
        }

        static void UpdateTM2(ManuscriptWord primaryMWord, TranslationWord[] tWords, ref Hashtable wordTM)
        {
            string strong = primaryMWord.strong;
            if (strong == "G2048")
            {
                ;
            }
            string translationText = GetTranslationText(tWords);

            if (wordTM.ContainsKey(strong))
            {
                ArrayList translations = (ArrayList)wordTM[strong];

                bool isNewTarget = true;

                for (int i = 0; i < translations.Count; i++)
                {
                    TargetTrans t = (TargetTrans)translations[i];  // TargetTrans { string Text; int Count }
                    if (t.Text == translationText)
                    {
                        t.Count = t.Count + 1;
                        isNewTarget = false;
                        break;
                    }
                }

                if (isNewTarget)
                {
                    TargetTrans t = new TargetTrans();
                    t.Text = translationText;
                    t.Count = 1;
                    translations.Add(t);
                }
            }
            else
            {
                ArrayList translations = new ArrayList();
                TargetTrans t = new TargetTrans();
                t.Text = translationText;
                t.Count = 1;
                translations.Add(t);
                wordTM.Add(strong, translations);
            }
        }

        // wordLinks is of the form Hashtable(s => ArrayList(t))
        //   where s is the 12-character source word ID
        //   and t is the 11-character target word ID
        // phraseTM is of the form
        //   Hashtable(strong => ArrayList(TargetTrans{ translationText, count }))
        //
        static void UpdatePhraseTM(Line line, Hashtable wordLinks, string treeFolder, Hashtable freqPhrases, ref Hashtable phraseTM)
        {

//            XmlNode tree = GetTree(treeFolder, line.manuscript.words);
            XmlNode tree = GetTrees(treeFolder, line.manuscript.words);
            string translation = GetTranslation(line.translation.words);

            // translation is a string of the form "t1_id1 ... tn_idn"
            // where tj is the text of word j 
            // and idj is the 11-character ID of word j

            foreach (XmlNode childTree in tree)
            {                
                CollectPhrases(childTree.FirstChild, translation, wordLinks, ref phraseTM, freqPhrases);
            }
        }


        // translation is a string of the form "t1_id1 ... tn_idn"
        //   where tj is the text of word j 
        //   and idj is the 11-character ID of word j
        // wordLinks is of the form Hashtable(s => ArrayList(t))
        //   where s is the 12-character source word ID
        //   and t is the 11-character target word ID
        // phraseTable is of the form
        //   Hashtable(strong => ArrayList(TargetTrans{ translationText, count }))
        //
        static void CollectPhrases(XmlNode treeNode, string translation, Hashtable wordLinks, ref Hashtable phraseTable, Hashtable freqPhrases)
        {
            if (treeNode.FirstChild.NodeType.ToString() == "Text")
            {
                return;
            }

            if (Utils.GetAttribValue(treeNode, "VirtualNode") == "True")
            {
                ;
            }

            if (treeNode.ChildNodes.Count > 1)
            {
                string lang = Utils.GetAttribValue(treeNode, "Language");
                if (lang == "A") lang = "H";
                string strongs = GetStrong(treeNode);
                string ids = GetIDs(treeNode);

                // ids is of the form "id1 ... idn"
                //  where idj is the 12-character morph ID of the j-th terminal node

                string nodeId = Utils.GetAttribValue(treeNode, "nodeId");
                if (nodeId == "400060330010100")
                {
                    ;
                }
                string phraseText = GetPhraseText(ids, translation, wordLinks);
                // phraseText is a possibly empty subrange of translation of the form
                //   "t_id ..." where t is the text of a word and id is its ID

                if (phraseText.Contains("_"))
                {
                    UpdateTM(strongs, RemoveIDs(phraseText), ref phraseTable, freqPhrases);
                }
            }

            foreach (XmlNode childNode in treeNode.ChildNodes)
            {
                CollectPhrases(childNode, translation, wordLinks, ref phraseTable, freqPhrases);
            }
        }


        // ids is of the form "id1 ... idn"
        //  where idj is the 12-character morph ID of the j-th terminal node
        //  for a particular syntax tree node
        // targetVerse is a string of the form "t1_id1 ... tn_idn"
        //   where tj is the text of word j 
        //   and idj is the 11-character ID of word j
        // wordLinks is of the form Hashtable(s => ArrayList(t))
        //   where s is the 12-character source word ID
        //   and t is the 11-character target word ID
        //
        // result is a possibly empty string of the form
        //   "text_id ..."
        //   made out of subrange of targetVerse
        //
        static string GetPhraseText(string ids, string targetVerse, Hashtable wordLinks)
        {
            string phraseText = string.Empty;

            string[] idList = ids.Split(" ".ToCharArray());
            ArrayList targets = new ArrayList();
            ArrayList linkedSources = new ArrayList();

            for (int i = 0; i < idList.Length; i++)
            {
                string id = idList[i];  // id is a 12-character source ID for a terminal node
                if (wordLinks.ContainsKey(id))
                {
                    linkedSources.Add(id);
                    ArrayList trgts = (ArrayList)wordLinks[id];

                    foreach (string trgt in trgts)
                    {
                        targets.Add(trgt);
                    }
                }
            }

            // linkedSources is array of the source IDs of terminal nodes
            // that had an entry in wordLinks.
            // targets is array of the target IDs that were linked to something
            // in linkedSources.

            Span s = GetRange(linkedSources);  // span is a pair of source IDs

            // If there are any targets and the span of the linked sources is at least
            // half the span of this tree node:
            if (targets.Count > 0 && ((double)(s.End - s.Start + 1) / (double)idList.Length) >= 0.5)
            {
                int targetLength = GetTargetLength(targets);  // targetLength measures extent in word positions
                if (((double)targetLength / (double)idList.Length) >= 0.5 || (targetLength > 1 && ((double)targetLength / (double)idList.Length) >= 0.4)
                    && (((double)targets.Count / (double)targetLength) >= 0.3 || EdgesAligned(idList, targets, wordLinks) || (double)(s.End - s.Start + 1) / (double)idList.Length >= 0.66)
                    && (((double)targets.Count / (double)idList.Length) >= 0.4 || EdgesAligned(idList, targets, wordLinks))
                    )
                {
                    Span r = GetRange(targets);
                    string[] targetWords = targetVerse.Split(" ".ToCharArray());
                    for (int i = r.Start - 1; i < r.End; i++)
                    {

                        phraseText += targetWords[i] + " ";
                    }
                }
            } 

/*            if (targets.Count > 0)
            {
                Span r = GetRange(targets);
                string[] targetWords = targetVerse.Split(" ".ToCharArray());
                for (int i = r.Start - 1; i < r.End; i++)
                {

                    phraseText += targetWords[i] + " ";
                }
            } */

            return phraseText.Trim();
        }

        static string GetStrong(XmlNode treeNode)
        {
            string strongs = string.Empty;

            ArrayList terminals = Terminals.GetTerminalXmlNodes(treeNode);
            string lang = GetLanguage((XmlNode)terminals[0]);
            foreach (XmlNode terminal in terminals)
            {
                strongs += Utils.GetAttribValue(terminal, "StrongNumberX") + " ";
            }

            return lang + strongs.Trim();
        }

        static string GetLanguage(XmlNode node)
        {
            return Utils.GetAttribValue(node, "Language");
        }

        static int GetTargetLength(ArrayList targets)
        {
            targets.Sort();
            string firstID = (string)targets[0];
            string lastID = (string)targets[targets.Count - 1];
            
            int targetLength = Int32.Parse(lastID.Substring(8)) - Int32.Parse(firstID.Substring(8)) + 1;

            if (targetLength < 0) // happens when the mapping crosses verse boundaries
            {
                targetLength = Int32.Parse(firstID.Substring(8)) > Int32.Parse(lastID.Substring(8)) ? Int32.Parse(firstID.Substring(8)) : Int32.Parse(lastID.Substring(8)); 
                // Using the longer side; to be computed accurately later
            }

            return targetLength;
        }

        static bool EdgesAligned(string[] sources, ArrayList targets, Hashtable wordLinks)
        {
            targets.Sort();
            string firstS = sources[0];
            string firstT = (string)targets[0];
            string lastS = sources[sources.Length - 1];
            string lastT = (string)targets[targets.Count - 1];
            if (IsLink(firstS, firstT, wordLinks) && IsLink(lastS, lastT, wordLinks)) return true;

            return false;
        }

        static bool IsLink(string source, string target, Hashtable wordLinks)
        {
            if (wordLinks.ContainsKey(source))
            {
                ArrayList targets = (ArrayList)wordLinks[source];
                if (targets.Contains(target))
                {
                    return true;
                }
            }

            return false;
        }

        static string RemoveIDs(string phrase)
        {
            string text = string.Empty;

            string[] words = phrase.Split(" ".ToCharArray());
            for (int i = 0; i < words.Length; i++)
            {
                int len = (words[i].IndexOf("_") >= 0) ? words[i].IndexOf("_") : words[i].Length;
                text += words[i].Substring(0, len) + " ";
            }

            return text.Trim();
        }

        // Returns a string of the form "id1 ... idn"
        //  where idj is the 12-character morph ID of the j-th terminal node
        //
        static string GetIDs(XmlNode treeNode)
        {
            string ids = string.Empty;

            ArrayList terminalNodes = Terminals.GetTerminalXmlNodes(treeNode);

            foreach (XmlNode terminalNode in terminalNodes)
            {
                string morphID = Utils.GetAttribValue(terminalNode, "morphId");
                if (morphID.Length == 11) morphID += "1";
                ids += morphID + " ";
            }

            return ids.Trim();
        }

        static Span GetRange(ArrayList targets)
        {
            Span r = new Span();

            foreach (string target in targets)
            {
                int pos = Int32.Parse(target.Substring(8, 3));
                if (r.Start == -1 || pos < r.Start)
                {
                    r.Start = pos;
                    r.First = target;
                }
                if (r.End == -1 || pos > r.End)
                {
                    r.End = pos;
                    r.Last = target;
                }
            }

            return r;
        }

  
        static XmlNode GetTrees(string treeFolder, ManuscriptWord[] mWords)
        {
            XmlNode tree = null;
            ArrayList treeNodes = new ArrayList();
            Hashtable bookNames = BookTables.LoadBookNames3();
            string chapter = mWords[0].id.ToString().PadLeft(12, '0').Substring(0, 5);
            Dictionary<string, XmlNode> trees = new Dictionary<string, XmlNode>();
            VerseTrees.GetChapterTree2(chapter, treeFolder, trees, bookNames);
            ArrayList verses = GetVerses(mWords);
            string sStartVerseID = (string)verses[0];
            if (sStartVerseID.StartsWith("11022043"))
            {
                ;
            }
            string sEndVerseID = (string)verses[verses.Count - 1];
            tree = Align.GetTreeNode(sStartVerseID, sEndVerseID, trees);

            return tree;
        }

        static ArrayList GetVerses(ManuscriptWord[] words)
        {
            ArrayList verses = new ArrayList();

            for(int i = 0; i < words.Length; i++)
            {
                ManuscriptWord word = words[i];
                string verseID = word.id.ToString().PadLeft(12, '0').Substring(0, 8);
                if (!verses.Contains(verseID))
                {
                    verses.Add(verseID);
                }
            }

            return verses;
        }

        // Result is a string of the form "t1_id1 ... tn_idn"
        // where tj is the text of word j 
        // and idj is the 11-character ID of word j
        //
        static string GetTranslation(TranslationWord[] words)
        {
            string transText = string.Empty;

            for (int i = 0; i < words.Length; i++)
            {
                string text = words[i].text;
                //                string id = words[i].id.ToString();
                string id = words[i].id.ToString().PadLeft(11, '0');
                transText += text + "_" + id + " ";
            }

            return transText.Trim().ToLower();
        }



 

 
        // groups is of the form Hashtable(sourceText => ArrayList(TargetGroup))
        //
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

            foreach(TargetGroup tg in translations)
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

        // Returns TargetGroup { text, primaryPosition }
        //   where text is made from the text of the links after sorting them in numerical order
        //   and with some ~ characters in there somehow,
        //   and primaryPosition is the 0-based position of the first target word (before the
        //   links were sorted) within the text
        //
        static TargetGroup GetTargetText(int[] targetLinks, Translation translation)
        {
            string text = string.Empty;
            int primaryIndex = targetLinks[0];
            Array.Sort(targetLinks);

            TargetGroup tg = new TargetGroup();  // TargetGroup { string Text; int PrimaryPosition; }
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

        static ManuscriptWord[] GetManuscriptWords(int[] sourceLinks, ManuscriptWord[] words)
        {
            ManuscriptWord[] mWords = new ManuscriptWord[sourceLinks.Length];

            for (int i = 0; i < sourceLinks.Length; i++)
            {
                ManuscriptWord mWord = words[sourceLinks[i]];
                mWords[i] = mWord;
            }

            return mWords;
        }

        static TranslationWord[] GetTranslationWords(int[] targetLinks, TranslationWord[] words)
        {
            TranslationWord[] tWords = new TranslationWord[targetLinks.Length];

            for (int i = 0; i < targetLinks.Length; i++)
            {
                TranslationWord tWord = words[targetLinks[i]];
                tWords[i] = tWord;
            }

            return tWords;
        }

        static string GetTranslationText(TranslationWord[] words)
        {
            string text = string.Empty;

            for (int i = 0; i < words.Length; i++)
            {
                text += words[i].text + " ";
            }

            return text.Trim();
        }

 

        static int GetTotalCounts(Hashtable translations)
        {
            int totalCount = 0;

            IDictionaryEnumerator transEnum = translations.GetEnumerator();

            while (transEnum.MoveNext())
            {
                Stats s = (Stats)transEnum.Value;
                totalCount += s.Count;
            }

            return totalCount;
        }
    }



    public class TargetTrans
    {
        public string Text;
        public int Count;
    }

    public class Span
    {
        public string First;
        public string Last;
        public int Start = -1;
        public int End = -1;
    }

    public class Stats
    {
        public int Count;
        public double Prob;
    }
}
