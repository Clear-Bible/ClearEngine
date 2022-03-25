using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;

// using Utilities;
// using GBI_Aligner;
using Newtonsoft.Json;
using ClearBible.Clear3.API;

namespace Clear3
{
    class Data
    {
        /*
        // 2020.06.24 CL: Should put WriteGlossTable() here if it is generated in CLEAR.


        // 2020.06.24 CL: Used to be GetGlossTableFromFile()
        // 2020.07.10 CL: Changed to be a tab separated values (.tsv) file rather than separated by " # "
        public static Dictionary<string, Gloss> ReadGlossTable(string glossFile)
        {
            var glossTable = new Dictionary<string, Gloss>();

            string[] lines = File.ReadAllLines(glossFile);
            foreach (string line in lines)
            {
                string[] parts = line.Split("\t".ToCharArray()); 

                if (parts.Length == 3)
                {
                    string morphID = parts[0];

                    Gloss g = new Gloss();
                    g.Gloss1 = parts[1];
                    g.Gloss2 = parts[2];

                    glossTable.Add(morphID, g);
                }
            }

            return glossTable;
        }
        */

        /*
        // 2020.06.24 CL: Should put WriteWordList() here if it is generated in CLEAR.
        // 2020.06.24 CL: Used to be GetWordList()
        // 2021.03.03 CL: Check if file exists. If not, return an empty model.
        // 2022.03.23 CL: Since the file can be mannually edited/created, added normalizing the data so it will match to the source/target words/lemmas.
        public static HashSet<string> ReadWordSet(string file, bool mustExist)
        {
            var wordSet = new HashSet<string>();

            if (!mustExist && !File.Exists(file)) return wordSet;

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                wordSet.Add(line.Trim().Normalize(NormalizationForm.FormC));
            }

            return wordSet;
        }

        // 2020.06.24 CL: should put WriteSimilarPhrases() here if it is generated in CLEAR.

        // 2020.06.24 CL: Not currently used.
        // Used to be GetSimilarPhrases()
        // 2020.07.10 CL: Modified to read in a tab separated values (.tsv) file rather than separated by " # ".
        public static Hashtable ReadSimilarPhrases(string filename)
        {
            Hashtable similarPhrases = new Hashtable();


            using (StreamReader sr = new StreamReader(filename, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    string phrase = line.Substring(0, line.IndexOf("\t"));
                    string sPhrases = line.Substring(line.IndexOf("\t") + 1);
                    ArrayList sPhraseList = GetsPhraseList(sPhrases);
                    similarPhrases.Add(phrase, sPhraseList);
                }
            }

            return similarPhrases;
        }

        static ArrayList GetsPhraseList(string sPhrases)
        {
            ArrayList phraseList = new ArrayList();

            string[] phrases = sPhrases.Split("\t".ToCharArray());

            for (int i = 0; i < phrases.Length; i++)
            {
                string phrase = phrases[i];
                if (phrase != string.Empty)
                {
                    phraseList.Add(phrase);
                }
            }

            return phraseList;
        }
        

        // 2020.06.24 CL: should put WriteFreqPhrases() here if it is generated in CLEAR.
               
        // 2020.06.24 CL: Used to be GetFreqPhrases()
        // 2020.07.10 CL: Modified to be a tab separated values (.tsv) file rather than separated by " # "
        public static Hashtable ReadFreqPhrases(string filename)
        {
            Hashtable freqPhrases = new Hashtable();

            string[] lines = File.ReadAllLines(filename);
            foreach (string line in lines)
            {
                string[] parts = line.Split("\t".ToCharArray());
                if (parts.Length == 2)
                {
                    string phrase = parts[0];
                    string freq = parts[1]; // 2020.07.10 CL: Why is the frequency kept as a string and not convereted to in int?

                    if (!freqPhrases.ContainsKey(phrase))
                    {
                        freqPhrases.Add(phrase, freq);
                    }
                }
            }

            return freqPhrases;
        }

        
        // 2020.06.24 CL: should put WriteStopWords() here if it is generated in CLEAR.
        // 2020.06.24 CL: Used to be GetStopWords()
        // 2021.03.03 CL: Check if file exists. If not, return an empty model.
        // 2022.03.23 CL: Since the file can be mannually edited/created, added normalizing the data so it will match to the source/target words/lemmas.
        public static HashSet<string> ReadStopWords(string file, bool mustExist)
        {
            var wordSet = new HashSet<string>();

        if (!mustExist && !File.Exists(file)) return wordSet;

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    wordSet.Add(line.Trim().Normalize(NormalizationForm.FormC));
                }
            }

            return wordSet;
        }
        */

        /*
        // 2020.06.24 CL: preAlignedTable is almost the same as alignModel except it splits out the IDs that are the key in alignModel, and ignores probability
        // 2021.02.23 CL: In order to avoid variation of the preAlignedTable because alignModel is a Hashtable and so order is not guaranteed, I changed alignModel to OrderedDictionary.
        // We still should deal with the fact this will ignore a second or third targetID alignment to the sourceID, and may not choose the one with the highest probability.
        // 2021.03.10 CL: Using OrderedDictionary didn't solve the inconsistency problem.
        // I think it is best not to lose information in the preAlignmentTable, but instead use the Dictionary<string, List<string>> where we put the targetID in order of probability.
        public static Dictionary<string, List<TargetIdProb>> BuildPreAlignmentTable(Dictionary<string, double> alignModel)
        {
            var preAlignedTable = new Dictionary<string, List<TargetIdProb>>();
            var preAlignedTableSort = new Dictionary<string, SortedDictionary<double, List<string>>>();
            
            foreach (var modelEnum in alignModel)
            {
                string link = (string)modelEnum.Key;
                string sourceID = link.Substring(0, link.IndexOf("-")); // CL: Why not use .Split("-".ToCharArray())
                string targetID = link.Substring(link.IndexOf("-") + 1);
                double prob = 1 - modelEnum.Value; // want decreasing order

                // 2021.02.22 There is a problem with this algorithm.
                // Since alignModel is a hashtable, the order is not necessarily the same each time the program is run.
                // This means that the links with the sourceID that is first is the one that will be used.
                // This means it the AlignWord() function may get different targetID when using preAlignment.
                //
                // It may make more sense to make the alignModel a Hashtable of a Hashtable with the first Hashtable's key the SourceID, and the second Hashtable's key the TargetID, and the value of the second Hashtable the probability.
                // This is necessary since some SMT models will do many-to-many alignments.
                // We can than use the alignModel directly rather than converting to the preAlignment Hashtable.
                // AlignWord() would need to be changed so that it would use the TargetID with the highest probability, rather than just the TargetID that appears in the preAlignment table.

                // Sort targets by probability
                if (preAlignedTableSort.ContainsKey(sourceID))
                {
                    var targetProbs = preAlignedTableSort[sourceID];

                    if (targetProbs.ContainsKey(prob))
                    {
                        var targetIdList = targetProbs[prob];

                        targetIdList.Add(targetID);
                    }
                    else
                    {
                        var targetList = new List<string>();

                        targetList.Add(targetID);
                        targetProbs.Add(prob, targetList);
                    }
                }
                else
                {
                    var targetProbs = new SortedDictionary<double, List<string>>();
                    var targetIdList = new List<string>();

                    targetIdList.Add(targetID);
                    targetProbs.Add(prob, targetIdList);
                    preAlignedTableSort.Add(sourceID, targetProbs);
                }
            }

            // Build prealignment table
            foreach (var entry in preAlignedTableSort)
            {
                var sourceID = entry.Key;
                var targetProbs = entry.Value;
                var targetList = new List<TargetIdProb>();

                foreach (var targetProb in targetProbs)
                {
                    var prob = 1 - targetProb.Key; // convert back to actual probability
                    var targetIdList = targetProb.Value;

                    foreach (var targetID in targetIdList)
                    {
                        var targetIdProb = new TargetIdProb();

                        targetIdProb.TargetID = targetID;
                        targetIdProb.Probability = prob;

                        targetList.Add(targetIdProb);
                    }
                }

                preAlignedTable.Add(sourceID, targetList);
            }

            return preAlignedTable;
        }
        */

        /*

        // 2020.06.24 CL: should put WriteSTrongsTable() here if it is generated in CLEAR.
        // 2020.06.24 CL: Used to be BuildStrongTable()
        // 2021.03.03 CL: Check if file exists. If not, return an empty model.
        // 2021.03.10 CL: The created strongs table never uses the int value and we set it to 1 here so it is not even in the file.
        // Changed from Hashtable to Dictionary<string, Dictionary<string, int>> to Dictionary<string, HashSet<string>>
        public static Dictionary<string, HashSet<string>> ReadStrongsTable(string file, bool mustExist)
        {
            var strongTable = new Dictionary<string, HashSet<string>>();

            if (!mustExist && !File.Exists(file)) return strongTable;

            string[] strongLines = File.ReadAllLines(file);

            foreach (string strongLine in strongLines)
            {
                string[] items = strongLine.Split();

                string wordId = items[0].Trim();
                string strong = items[1].Trim();

                if (strongTable.ContainsKey(strong))
                {
                    var wordIds = strongTable[strong];
                    wordIds.Add(wordId);
                }
                else
                {
                    var wordIds = new HashSet<string>();

                    wordIds.Add(wordId);
                    strongTable.Add(strong, wordIds);
                }
            }

            return strongTable;
        }

        public static void UpdateGroups(ref Dictionary<string, List<TargetGroup>> groups, int[] sourceLinks, int[] targetLinks, Manuscript manuscript, Translation translation)
        {
            string sourceText = GetSourceText(sourceLinks, manuscript);
            TargetGroup targetGroup = GetTargetText(targetLinks, translation);

            if (groups.ContainsKey(sourceText))
            {
                var translations = groups[sourceText];
                if (!HasGroup(translations, targetGroup))
                {
                    translations.Add(targetGroup);
                }
            }
            else
            {
                var translations = new List<TargetGroup>();
                translations.Add(targetGroup);
                groups.Add(sourceText, translations);
            }
        }

        public static bool HasGroup(List<TargetGroup> translations, TargetGroup targetGroup)
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
            string text = string.Empty; // CL: text variable not used
            int primaryIndex = targetLinks[0];
            Array.Sort(targetLinks);

            TargetGroup tg = new TargetGroup();
            tg.PrimaryPosition = GetPrimaryPosition(primaryIndex, targetLinks);

            int prevIndex = -1;
            for (int i = 0; i < targetLinks.Length; i++)
            {
                int targetLink = targetLinks[i];
                string word = string.Empty; // CL: Unnecessary
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

        // 2020.06.24 CL: Used to be GetOldLinks()
        // Creates a Hashtable with the key as a VerseID, and the value is another Hashtable with the manuscript word altId as the key and translation word altId as the value.
        // 2021.03.03 CL: Check if file exists. If not, return an empty model.
        //
        // AltId works on the idea that they will stay in the same order in the sentence. However, it seems we want to have them stay with the same head that they are related to.
        // This way, if the head moves to a different location causing a change in order,
        // the related words will be linked to the closest target to that head, rather than worrying about the order in the sentence.
        // We can continue to use the order for the manuscript word, but we may want some other way to indicate which target it should be linked to.
        // Also, this could get messed up if the versification changes.
        // It seems keeping things related from source verse to target verse where the links occur makes more sense, though still not perfect if they move the target phrase
        // to a different verse. We could make the algortihm first find the target word in the original verse, but if it doens't exist anymore, than look at the whole target zone.
        // So the target word can have the verseID, the head word, and the target word, as the information for the target link to a source word.
        // The source side would have the source word, and the source head that it is related to. Therefore, there should be a two way match.
        // We might even want to put information about whether it was before or after the head in the target.
        // Also, current datastructure assumes a one-to-one or many-to-one between source and target AltIds, but not one-to-many.
        // That is, a source AltId can't have more than one target AltId linked to it.
        // I've modified the code and datastructure to allow for one-to-many.
        //
        // 2022.03.23 CL: We may get alignment files that didn't normalize the source/target words/lemmas, so need to do it when reading in alignment files.

        public static Dictionary<string, Dictionary<string, List<string>>> ReadOldAlignments(string jsonFile, ref Dictionary<string, List<TargetGroup>> groups, bool mustExist)
        {
            var oldLinks = new Dictionary<string, Dictionary<string, List<string>>>();

            if (!mustExist && !File.Exists(jsonFile)) return oldLinks;

            string jsonText = File.ReadAllText(jsonFile);
            Line[] lines = JsonConvert.DeserializeObject<Line[]>(jsonText);
            if (lines == null) return oldLinks;

            AutoAligner.UnicodeNormalizeAlignments(ref lines);

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
                            var verseLinks = oldLinks[verseID];

                            if (verseLinks.ContainsKey(mWord.altId))
                            {
                                var targetAltIds = verseLinks[mWord.altId];

                                targetAltIds.Add(tWord.altId);
                            }
                            else
                            {
                                var targetAltIds = new List<string>();

                                targetAltIds.Add(tWord.altId);
                                verseLinks.Add(mWord.altId, targetAltIds);
                            }
                        }
                        else
                        {
                            var verseLinks = new Dictionary<string, List<string>>();
                            var targetAltIds = new List<string>();

                            targetAltIds.Add(tWord.altId);
                            verseLinks.Add(mWord.altId, targetAltIds);
                            oldLinks.Add(verseID, verseLinks);
                        }
                    }
                }
            }

            return oldLinks;
        }
        */
        //
        // 2021.06.16 CL: After talking with ClearSuite team, they didn't think the extra "lemma" field in the word alignment file would be a problem.
        // However, I eventually decided that there may be other program that expect a legacy JSON alignment file where it could cause a problem so it was best to actually
        // write out a version that is 100% identical to the legacy version (which has no lemma for the target)
        public static void ConvertLemmaToWordAlignments(string jsonLemmasInput, string jsonOutput)
        {
            string jsonText = File.ReadAllText(jsonLemmasInput);
            LpaLemmaLine[] jsonLemmaLines = JsonConvert.DeserializeObject<LpaLemmaLine[]>(jsonText);
            var newJsonLines = new LpaLine[jsonLemmaLines.Length];

            // Next two lines are not necessary but they are here to avoid getting a warning that it is not used.
            // var newJson = new AlignmentLegacy();
            // newJson.Lines = newJsonLines;

            for (int i = 0; i < jsonLemmaLines.Length; i++)
            {
                var line = jsonLemmaLines[i];
                var tWords = line.translation.words;
                var links = line.links;

                // What changes are the Translations and Links
                var newTranslation = new LpaTranslation();
                var newLinks = new List<LpaLink>();
                var newLine = new LpaLine();
                newLine.translation = newTranslation;
                newLine.links = newLinks;
                newJsonLines[i] = newLine;

                // Manuscript stays the same
                newLine.manuscript = line.manuscript;

                // Need to map old link indices to new ones
                var indexMap = new Dictionary<int, int>();

                // Need to create new TranslationWords for the words.
                // Need to collect them all up first in a list so we know how many in order to create the array TranslationWord[]
                var newTranslationList = new List<LpaTranslationWord>();

                // Convert translation and create index mapping table
                int newIndex = 0;

                // Set up first word (prime the pump)
                var firstWord = tWords[0];
                // 2021.06.21 CL: Made the target morpheme id add two more digits, so it is BBCCCVVVWWWMM, which is 13 digits.
                // The normal wordID is just BBCCCVVVWWW, which is 11 digits.
                string currentWordID = firstWord.id.ToString().PadLeft(13, '0').Substring(0, 11);

                var currentWord = new LpaTranslationWord();
                currentWord.id = long.Parse(currentWordID);
                currentWord.altId = firstWord.altId;
                currentWord.text = firstWord.text;

                for (int origIndex = 0; origIndex < tWords.Length; origIndex++)
                {
                    var tWord = tWords[origIndex];

                    var wordID = tWord.id.ToString().PadLeft(13, '0').Substring(0, 11);

                    if (wordID != currentWordID)
                    {
                        // Finish off current word
                        newTranslationList.Add(currentWord);

                        // Start new word
                        currentWord = new LpaTranslationWord();
                        currentWord.id = long.Parse(wordID);
                        currentWord.altId = tWord.altId;
                        currentWord.text = tWord.text;

                        currentWordID = wordID;
                        newIndex++;
                    }

                    indexMap.Add(origIndex, newIndex);
                }
                // Handle last case
                // Finish off current word
                newTranslationList.Add(currentWord);

                // Create new array for translation words
                var newTranslationWords = new LpaTranslationWord[newTranslationList.Count];

                for (int j = 0; j < newTranslationList.Count; j++)
                {
                    newTranslationWords[j] = newTranslationList[j];
                }

                newTranslation.words = newTranslationWords;


                // Convert links
                foreach (LpaLink link in links)
                {
                    int[] targets = link.target;
                    int[] newTargets = new int[targets.Length];

                    for (int j = 0; j < targets.Length; j++)
                    {
                        newTargets[j] = indexMap[targets[j]];
                    }

                    var newLink = new LpaLink();
                    newLink.source = link.source;
                    newLink.target = newTargets;
                    newLink.cscore = link.cscore;

                    newLinks.Add(newLink);
                }
            }

            string json = JsonConvert.SerializeObject(newJsonLines, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);
        }
        

        // If all of the lemmas of a surface word are filtered out, then also filtered out the surface word
        public static void FilterOutWordsLemmaText(
            string lemmaFile,
            string lemmaIdFile,
            string textFile,
            string textIdFile,
            string lemmaFileNew,
            string lemmaIdFilenew,
            string textFileNew,
            string textIdFileNew,
            HashSet<string> wordsList)
        {
            using (StreamWriter swLemma = new StreamWriter(lemmaFileNew, false, Encoding.UTF8))
            using (StreamWriter swLemmaId = new StreamWriter(lemmaIdFilenew, false, Encoding.UTF8))
            using (StreamWriter swText = new StreamWriter(textFileNew, false, Encoding.UTF8))
            using (StreamWriter swTextId = new StreamWriter(textIdFileNew, false, Encoding.UTF8))
            {
                string[] linesLemma = File.ReadAllLines(lemmaFile);
                string[] linesLemmaId = File.ReadAllLines(lemmaIdFile);
                string[] linesText = File.ReadAllLines(textFile);
                string[] linesTextId = File.ReadAllLines(textIdFile);

                for (int i = 0; i < linesLemma.Length; i++)
                {
                    var lineLemma = linesLemma[i];
                    var lineLemmaId = linesLemmaId[i];
                    var lineText = linesText[i];
                    var lineTextId = linesTextId[i];

                    string lineLemmaCW = string.Empty;
                    string lineLemmaIdCW = string.Empty;
                    string lineTextCW = string.Empty;
                    string lineTextIdCW = string.Empty;

                    string[] lemmas = lineLemma.Split();
                    string[] lemmaIds = lineLemmaId.Split();
                    string[] words = lineText.Split();
                    string[] wordIds = lineTextId.Split();

                    // Need to use two indices, one for lemma and one for word
                    int wordIndex = 0;
                    var word = words[wordIndex];
                    var wordId = wordIds[wordIndex];
                    bool allLemmasInList = true;
                    for (int j = 0; j < lemmas.Length; j++)
                    {
                        var lemma = lemmas[j];
                        var lemmaId = lemmaIds[j];

                        var lemmaWordId = lemmaId.Substring(0, 11);

                        // Check if starting a new surface word
                        if (lemmaWordId != wordId)
                        {
                            if (!allLemmasInList)
                            {
                                lineTextCW += word + " ";
                                lineTextIdCW += wordId + " ";
                            }
                            wordIndex++;
                            word = words[wordIndex];
                            wordId = wordIds[wordIndex];
                            allLemmasInList = true;
                        }

                        if (!wordsList.Contains(lemma))
                        {
                            lineLemmaCW += lemma + " ";
                            lineLemmaIdCW += lemmaId + " ";

                            allLemmasInList = false;
                        }
                    }

                    // Handle end case
                    if (!allLemmasInList)
                    {
                        lineTextCW += word + " ";
                    }

                    swLemma.WriteLine(lineLemmaCW.Trim());
                    swLemmaId.WriteLine(lineLemmaIdCW.Trim());
                    swText.WriteLine(lineTextCW.Trim());
                    swTextId.WriteLine(lineTextIdCW.Trim());
                }
            }
        }

        // Removes the words in the wordsList (lemma) from the files. Used primarily to remove things before being processed by SMT models
        // Do only the word/lemma and id files since they are guaranteed to be the same length
        public static void FilterOutWords(
            string wordFile,
            string idFile,
            string wordFileNew,
            string idFilenew,
            HashSet<string> wordsList)
        {
            using (StreamWriter swWord = new StreamWriter(wordFileNew, false, Encoding.UTF8))
            using (StreamWriter swId = new StreamWriter(idFilenew, false, Encoding.UTF8))
            {
                string[] linesWord = File.ReadAllLines(wordFile);
                string[] linesId = File.ReadAllLines(idFile);

                for (int i = 0; i < linesWord.Length; i++)
                {
                    var lineWord = linesWord[i];
                    var lineId = linesId[i];

                    string lineWordFiltered = string.Empty;
                    string lineIdFiltered = string.Empty;

                    string[] words = lineWord.Split();
                    string[] ids = lineId.Split();

                    for (int j = 0; j < words.Length; j++)
                    {
                        var word = words[j];
                        var id = ids[j];

                        if (!wordsList.Contains(word))
                        {
                            lineWordFiltered += word + " ";
                            lineIdFiltered += id + " ";
                        }
                    }
                    swWord.WriteLine(lineWordFiltered.Trim());
                    swId.WriteLine(lineIdFiltered.Trim());
                }
            }
        }

        // Removes the words in the wordsList (lemma) from the files
        // Because the number of words in the target text and lemma are no longer guaranteed to be the same
        // We can't use this simple method. We could create a lemma function words list and a surface function words list
        // But this means we would also have to change the statistical program that creates function words.
        // I think it may be easier to create a new function that will check if all the lemmas of a word are function words, then
        // we will filter out the surface word.
        // It will be more algorithmically complex but will be easier to have just one function words list.
        public static void FilterOutWords(
            string lemmaFile,
            string idFile,
            string textFile,
            string lemmaFileNew,
            string idFilenew,
            string textFileNew,
            HashSet<string> wordsList)
        {
            using (StreamWriter swLemma = new StreamWriter(lemmaFileNew, false, Encoding.UTF8))
            using (StreamWriter swId = new StreamWriter(idFilenew, false, Encoding.UTF8))
            using (StreamWriter swText = new StreamWriter(textFileNew, false, Encoding.UTF8))
            {
                string[] linesText = File.ReadAllLines(textFile);
                string[] linesLemma = File.ReadAllLines(lemmaFile);
                string[] linesId = File.ReadAllLines(idFile);

                for (int i = 0; i < linesLemma.Length; i++)
                {
                    var lineText = linesText[i];
                    var lineLemma = linesLemma[i];
                    var lineId = linesId[i];

                    string lineTextFiltered = string.Empty;
                    string lineLemmaFiltered = string.Empty;
                    string lineIdFiltered = string.Empty;

                    string[] words = lineText.Split();
                    string[] lemmas = lineLemma.Split();
                    string[] ids = lineId.Split();

                    for (int j = 0; j < lemmas.Length; j++)
                    {
                        var word = words[j];
                        var lemma = lemmas[j];
                        var id = ids[j];

                        if (!wordsList.Contains(lemma))
                        {
                            lineTextFiltered += word + " ";
                            lineLemmaFiltered += lemma + " ";
                            lineIdFiltered += id + " ";
                        }
                    }
                    swText.WriteLine(lineTextFiltered.Trim());
                    swLemma.WriteLine(lineLemmaFiltered.Trim());
                    swId.WriteLine(lineIdFiltered.Trim());
                }
            }
        }

        // Removes the words in the wordsList (lemma) from the files
        public static void FilterOutWords(
            string lemmaFile,
            string idFile,
            string textFile,
            string lemmaCatFile,
            string lemmaFileNew,
            string idFileNew,
            string textFileNew,
            string lemmaCatFileNew,
            HashSet<string> wordsList)
        {
            using (StreamWriter swLemma = new StreamWriter(lemmaFileNew, false, Encoding.UTF8))
            using (StreamWriter swId = new StreamWriter(idFileNew, false, Encoding.UTF8))
            using (StreamWriter swText = new StreamWriter(textFileNew, false, Encoding.UTF8))
            using (StreamWriter swLemmaCat = new StreamWriter(lemmaCatFileNew, false, Encoding.UTF8))
            {
                string[] linesText = File.ReadAllLines(textFile);
                string[] linesLemma = File.ReadAllLines(lemmaFile);
                string[] linesId = File.ReadAllLines(idFile);
                string[] linesLemmaCat = File.ReadAllLines(lemmaCatFile);

                for (int i = 0; i < linesLemma.Length; i++)
                {
                    var lineText = linesText[i];
                    var lineLemma = linesLemma[i];
                    var lineId = linesId[i];
                    var lineLemmaCat = linesLemmaCat[i];

                    string lineTextFiltered = string.Empty;
                    string lineLemmaFiltered = string.Empty;
                    string lineIdFiltered = string.Empty;
                    string lineLemmaCatFiltered = string.Empty;

                    string[] words = lineText.Split();
                    string[] lemmas = lineLemma.Split();
                    string[] ids = lineId.Split();
                    string[] lemmaCats = lineLemmaCat.Split();

                    for (int j = 0; j < lemmas.Length; j++)
                    {
                        var word = words[j];
                        var lemma = lemmas[j];
                        var id = ids[j];
                        var lemmaCat = lemmaCats[j];

                        if (!wordsList.Contains(lemma))
                        {
                            lineTextFiltered += word + " ";
                            lineLemmaFiltered += lemma + " ";
                            lineIdFiltered += id + " ";
                            lineLemmaCatFiltered += lemmaCat + " ";
                        }
                    }
                    swText.WriteLine(lineTextFiltered.Trim());
                    swLemma.WriteLine(lineLemmaFiltered.Trim());
                    swId.WriteLine(lineIdFiltered.Trim());
                    swLemmaCat.WriteLine(lineLemmaCatFiltered.Trim());
                }
            }
        }
    }
}
