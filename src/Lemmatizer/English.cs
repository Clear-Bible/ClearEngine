using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lemmatizer
{
    public class English
    {
        public static (bool, string[]) Lemmatize(string word, Dictionary<string, string[]> lemmaData, string lowerCaseMethod, CultureInfo targetCultureInfo)
        {
            bool lemmatized = true;
            string[] lemmas;

            
            var wordLC = LowerCaseEnglishWords(word, lowerCaseMethod, targetCultureInfo);
            var normalizedWord2 = wordLC.Replace("'", "’"); // normalize for apostrophe (use curly single quote). Maybe we can assume this is done when creating the verses.

            if (lemmaData.ContainsKey(wordLC))
            {
                lemmas = lemmaData[wordLC];

                // Should we loop through the lemmas and make sure they are all Unicode normalized?
            }
            else if (BeVerb.Contains(wordLC))
            {
                lemmas = "be".Split();
            }
            else if (Contractions.ContainsKey(normalizedWord2))
            {
                var words = Contractions[normalizedWord2];
                lemmas = words.Split();
            }
            else if (normalizedWord2.EndsWith("’s")) // Possessive
            {
                string stem = wordLC.Substring(0, wordLC.Length - 2);

                // Split into two lemmas
                // string apostropheS = word.Substring(word.Length - 2, 1) + "s"; // use original apostrophe
                // lemmas = new string[] { Lemmas.LowerCase(stem, lowerCaseMethod, targetCultureInfo), apostropheS };

                // Simply removing the ’s results in better alignments
                lemmas = stem.Split();
            }
            else
            {
                // None of the above special cases so just use the lowercase form
                lemmas = wordLC.Split();
            }
            
            return (lemmatized, lemmas);
        }

        // This takes care of exceptions when we don't want to lowercase an English word.
        private static string LowerCaseEnglishWords(string word, string lowerCaseMethod, CultureInfo targetCultureInfo)
        {
            if (!KeepUpperCase.Contains(word))
            {
                word = Lemmas.LowerCase(word, lowerCaseMethod, targetCultureInfo);
            }

            return word;
        }

        // We could add more but this is a start
        private static HashSet<string> KeepUpperCase = new HashSet<string>()
        {
            "LORD", // Leave "LORD" all upper case to distinguish Yahweh and the common meaning of "lord".
            "LORD’s",
            "LORD's",
            "Mark", // Don’t want the name "Mark" to be confused with the verb or noun "mark" as in "to mark your head", "the mark of the devil"
            "Mark’s",
            "Mark's",
            "God", // Don’t want "God" to be confused with the common noun "god". Not sure whether the common "god" will occur at the beginning of a sentence.
            "God’s",
            "God's",
        };

        // For some of these, I’m not sure if we want to get rid of tense
        private static Dictionary<string, string> Contractions = new Dictionary<string, string>()
        {
            // -n’t contractions
            { "don’t", "not" },
            { "doesn’t", "not" },
            { "didn’t", "not" },
            { "haven’t", "not" },
            { "hasn’t", "not" },
            { "hadn’t", "not" },
            { "aren’t", "be not" },
            { "isn’t", "be not" },
            { "wasn’t", "be not" },
            { "weren’t", "be not" },
            { "can’t", "not" },
            { "wouldn’t", "not" },
            { "won’t", "not" },
            { "shouldn’t", "not" },
            // -’s contractions
            // expanded to "us"
            { "let’s", "let us" },
            // expended to "is"
            { "it’s", "it be" },
            { "he’s", "he be" },
            { "she’s", "she be" },
            // -’ll contractions
            { "we’ll", "we" },
            { "i’ll", "i" },
            // -’m contractions
            { "i’m", "i be" },
            // -’re contractions
            { "we’re", "we be" },
            { "they’re", "they be" },
            { "you’re", "you be" },
            // -'d contraction
            // This is ambiguous between had or would. It may not matter since Hebrew or Greek may not use a word to express these auxilliaries anyway
            // Or maybe simply remove the ’d?
            { "i’d", "i" },
            { "we’d", "we" },
            { "you’d", "you" },
            { "he’d", "he" },
            { "she’d", "she" },
            { "they’d", "they" },
            // Other
            { "’twas", "it be" },
            // Contractions without apostrophe
            { "its", "it" }, // ignore possessive
            { "gonna", "going to" },
            { "wanna", "want to" },
        };

        // For some of these, I’m not sure if we want to get rid of tense
        private static HashSet<string> BeVerb = new HashSet<string>()
        {
            "is",
            "am",
            "are",
            "was",
            "were",
        };


        /*
        // For some of these, I’m not sure if we want to get rid of tense
        private static Dictionary<string, string> Contractions = new Dictionary<string, string>()
        {
            // -n’t contractions
            { "don’t", "do not" },
            { "doesn’t", "does not" },
            { "didn’t", "did not" },
            { "haven’t", "have not" },
            { "hasn’t", "has not" },
            { "hadn’t", "had not" },
            { "aren’t", "are not" },
            { "isn’t", "is not" },
            { "wasn’t", "was not" },
            { "weren’t", "were not" },
            { "can’t", "can not" },
            { "wouldn’t", "would not" },
            { "won’t", "will not" },
            { "shouldn’t", "should not" },
            // -’s contractions
            // expanded to "us"
            { "let’s", "let us" },
            // expended to "is"
            { "it’s", "it is" },
            { "he’s", "he is" },
            { "she’s", "she is" },
            // -’ll contractions
            { "we’ll", "we will" },
            { "i’ll", "i will" },
            // -’m contractions
            { "i’m", "i am" },
            // -’re contractions
            { "we’re", "we are" },
            { "they’re", "they are" },
            { "you’re", "you are" },
            // -'d contraction
            // This is ambiguous between had or would. It may not matter since Hebrew or Greek may not use a word to express these auxilliaries anyway
            // Or maybe simply remove the ’d?
            { "i’d", "i would" },
            { "we’d", "we would" },
            { "you’d", "you would" },
            { "he’d", "he would" },
            { "she’d", "she would" },
            { "they’d", "they would" },
            // Other
            { "’twas", "it was" },
            // Contractions without apostrophe
            { "its", "it" }, // ignore possessive
            { "gonna", "going to" },
            { "wanna", "want to" },
        };
        */
    }
}
