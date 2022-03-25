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
