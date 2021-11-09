using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Lemmatizer
{
    public class Lemmas
    {
        // Create the tokenized target lemma file along with its corresponding word id file.
        // It used to be that there was a one-to-one word-to-lemma assumption so the word id would be the same between the text and lemma file.
        // But now that we allow a one-to-many (and I suppose later, a many-to-one) relationship between word and lemma, we can no longer assume this.
        // Also, it used to be that to lemmatize the target just meant making it lowercase, but with the availablility of morphological analyszers for some languages,
        // we want to be able to take advantage of them, and we do it by having a file that is a word-to-lemmas dictionary.
        public static void Lemmatize(
            string tokenFile, // the original verse text file in verse-per-line format
            string tokenLemmaFile, // the lemmatized file either using a lexicon or in lowercase case (lemma) in verse-per-line
            string tokenLemmaIdFile, // the morphID file for the lemma file in verse-per-line
            string lang, // language of the verse text. Actually need to use the CSharpCulture value to specify the kind of lowercase to use.
            string lowerCaseMethod,
            string targetCSharpCulture,
            string lemmaDataFile // language specific data on mapping of word to one or more lemmas. May not always exist.
            )
        {
            var targetCultureInfo = new CultureInfo(targetCSharpCulture);
            var lemmaData = Lexicon.ReadWordToLemmasData(lemmaDataFile, lang);

            using (StreamReader srText = new StreamReader(tokenFile, Encoding.UTF8))
            using (StreamWriter swLemma = new StreamWriter(tokenLemmaFile, false, Encoding.UTF8))
            using (StreamWriter swLemmaID = new StreamWriter(tokenLemmaIdFile, false, Encoding.UTF8))
            {
                string line = string.Empty;

                while ((line = srText.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length < 9) continue;
                    if (line.Substring(0, 2) == "//") continue;

                    string verseID = line.Substring(0, line.IndexOf(' '));
                    string verseText = line.Substring(line.IndexOf(' ') + 1).Trim();
                    string verseLemma = string.Empty;
                    string verseLemmaID = string.Empty;

                    string[] words = verseText.Split();

                    for (int i = 0; i < words.Length; i++)
                    {
                        var word = words[i];
                        string wordID = verseID + (i + 1).ToString().PadLeft(3, '0');

                        // We don't want to do Unicode normalization of the word here since we are not sure if the language specific normalization expects normalized words.
                        // Let each one determine that for itself. We will assume the lemmas returned are normalized?
                        (var lemmatized, var lemmas) = Lemmatize(word, lemmaData, lang, lowerCaseMethod, targetCultureInfo);

                        if (lemmatized)
                        {
                            // Assumes no more than 9 lemmas otherwise lemmaID would be too long.
                            // If we encounter languages that might have more than 9, we need to add two digits for morpheme.
                            // This is highly possible with highly agglutinative languages.
                            // 2021.06.21 CL: This this is highly possible, I've decided to future proof this by having two digits for the morpheme for target languages.
                            for (int j = 0; j < lemmas.Length; j++)
                            {
                                // We do Unicode normalization here to guarantee the lemmas are all normalized
                                verseLemma += lemmas[j] + " ";
                                string lemmaID = wordID + (j + 1).ToString().PadLeft(2,'0');
                                verseLemmaID += lemmaID + " ";
                            }
                        }
                        else
                        {
                            // I'm not sure if this makes sense. I think if we do have a word-to-lemma dictionary, then it should always be used and not to use the lowercase method.
                            // For now, I'll leave it this way for cases where you may have a word-to-lemma dictionary, but only want to have words that there is a difference.
                            // The current algorithm lets you leave out all the words where the lemma is just going to be a lowercase version of the surface word.
                            // This also avoids having to put multiple entries in the dictionary for the same word for capitalized and all caps (e.g., "The" and "THE", being mapped to the lemma "the").                            
                            verseLemma += LowerCase(word, lowerCaseMethod, targetCultureInfo) + " ";

                            // Need to also add another digit, but add "0" to indicate it is not a real morpheme.
                            // This is different from the tree in that it always has a "1" for
                            // Originally didn't add this "0" but this will cause problems when reading it in since these are double numbers in the JSON file.
                            // When you convert to string, the books 1-9 will not have a leading zero but we know that because the number of characters is one less.
                            // But if we don't add the "0", then it could be one less than the lemmasIDs above because it is book 1-9 and not because it is not a lemma.
                            // 2021.06.21 CL: Changed to add "00" to have two digits for the morpheme. See not above.
                            verseLemmaID += wordID + "00" + " ";
                        }
                    }

                    swLemma.WriteLine("{0}  {1}", verseID, verseLemma.Trim());
                    swLemmaID.WriteLine("{0}  {1}", verseID, verseLemmaID.Trim());
                }
            }
        }

        // Check if the word is in the lemmaData, otherwise use special lemmatization depending on the language if it exists.
        private static (bool, string[]) Lemmatize(string word, Dictionary<string, string[]> lemmaData, string lang, string lowerCaseMethod, CultureInfo targetCultureInfo)
        {
            bool lemmatized = false;
            string[] lemmas = string.Empty.Split();


            // Use rule based or other methods to lemmatize the word that are specific to the language or for all languages
            switch (lang)
            {
                case "English":
                    (lemmatized, lemmas) = English.Lemmatize(word, lemmaData, lowerCaseMethod, targetCultureInfo);
                    break;
                case "Malayalam":
                    (lemmatized, lemmas) = Malayalam.Lemmatize(word, lemmaData, lowerCaseMethod, targetCultureInfo);
                    break;
                default:
                    // Put default lemmatization methods here
                    break;
            }


            return (lemmatized, lemmas);
        }

        //
        public static string LowerCase(string word, string lowerCaseMethod, CultureInfo targetCultureInfo)
        {
            switch (lowerCaseMethod)
            {
                case "ToLower":
                    word = word.ToLower();
                    break;
                case "ToLowerInvariant":
                    word = word.ToLowerInvariant();
                    break;
                case "CultureInfo":
                    word = word.ToLower(targetCultureInfo);
                    break;
                case "None":
                default:
                    break;
            }

            return word;

        }
    }
}
