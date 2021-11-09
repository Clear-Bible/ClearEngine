using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lemmatizer
{
    public class Malayalam
    {
        //
        public static void CreateWordToLemmaFile(string file)
        {
            // This is where we would either run a script or a C# program or code to create the WordToLemma file that is needed.
            // For now, it does nothing.

            // Need to write it based upon the Python code that creates that file.
        }

        // If the lemmas are not in the table, just use the word as it is as the lemma for Malayam.
        // Do not want to do the default lowercase method to lemmatize for Malayalam
        // We do Unicode normalization here since we don't want to possibly confuse mlmorph if the internal data is not normalized.
        // Since we don't normalize the lemmas that are part of the table at this time, we will not normalize it here.
        public static (bool, string[]) Lemmatize(string word, Dictionary<string, string[]> lemmaData, string lowerCaseMethod, CultureInfo targetCultureInfo)
        {
            bool lemmatized = true;
            string[] lemmas;

            if (lemmaData.ContainsKey(word))
            {
                lemmas = lemmaData[word];

                // Should we loop through the lemmas and make sure they are all Unicode normalized?
            }
            else
            {
                lemmas = word.Split();
            }

            return (lemmatized, lemmas);
        }
    }
}
