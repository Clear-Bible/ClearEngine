using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Lemmatizer
{
    public class Lexicon
    {
        // In order to be able to use target lexical information if available, we are going to use it for lemmatization.
        // It may not be easy to have a unified way to actually use lemmatization programs for different languages.
        // So instead of trying to run lemmatization directly from the ClearEngine, we will import lexical information instead.
        //
        // The most basic thing is to relate surface words to lemma(s). Since for many languages that have compound words,
        // or agglutinative languages, a surface word may have more than one lemma.
        // The format of the file expected is currently:
        //
        // <word><tab><lemma>(<space><lemma>)*
        //
        // The use of a space as the deliminter between lemmas may need to change if a lemma may have a space in between it.
        // This causes problems later on down the line so we currently assume a lemma will not have a space.
        // If for a language a lemma may have a space, then it must be converted to be without a space (i.e. replace a space with a tilda)
        // before creating this database.
        public static Dictionary<string, string[]> ReadWordToLemmasData(string file, string lang)
        {
            var lemmaData = new Dictionary<string, string[]>();

            if (!File.Exists(file))
            {
                TryToCreateWordToLemmaFile(file, lang);
            }

            if (File.Exists(file))
            {
                using (StreamReader srText = new StreamReader(file, Encoding.UTF8))
                {
                    string line = string.Empty;

                    while ((line = srText.ReadLine()) != null)
                    {
                        var parts = line.Split('\t');

                        if (parts.Length == 2)
                        {
                            var word = parts[0];
                            var lemmas = parts[1].Split();
                            lemmaData.Add(word, lemmas);
                        }
                    }
                }
            }

            return lemmaData;
        }

        // There may not be a way to create the file for every language
        private static void TryToCreateWordToLemmaFile(string file, string lang)
        {
            switch (lang)
            {
                case "Malayalam":            
                    Malayalam.CreateWordToLemmaFile(file);
                    break;
                default:
                    break;
            }
        }

    }
}
