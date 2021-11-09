using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;


using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;
using ClearBible.Clear3.SubTasks;

namespace Clear3
{
    public class Persistence
    {
        // For Clear3, we don't need to export Lemmas since ParallelCorpora does not store lemmas, but they could be used by Clear2 and converted in another place in Clear3.
        // For Clear2, it doesn't need the source text, but it is part of ParallelCopora in Clear3.
        // It would be nice to make what Clear2 and Clear3 uses to be the same and minimal.
        public static void ExportParallelCorpora(
            ParallelCorpora parallelCorpora,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceIdFile,
            string sourceLemmaCatFile,
            string targetTextFile,
            string targetLemmaFile,
            string targetIdFile)
        {
            using (StreamWriter swSourceTextFile = new StreamWriter(sourceTextFile, false, Encoding.UTF8))
            using (StreamWriter swSourceLemmaFile = new StreamWriter(sourceLemmaFile, false, Encoding.UTF8))
            using (StreamWriter swSourceIdFile = new StreamWriter(sourceIdFile, false, Encoding.UTF8))
            using (StreamWriter swSourceLemmaCatFile = new StreamWriter(sourceLemmaCatFile, false, Encoding.UTF8))
            using (StreamWriter swTargetTextFile = new StreamWriter(targetTextFile, false, Encoding.UTF8))
            using (StreamWriter swTargetLemmaFile = new StreamWriter(targetLemmaFile, false, Encoding.UTF8))
            using (StreamWriter swTargetIdFile = new StreamWriter(targetIdFile, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    swSourceTextFile.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => s.SourceText.Text)));
                    swSourceLemmaFile.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => s.SourceLemma.Text)));                   
                    swSourceIdFile.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => s.SourceID.AsCanonicalString)));
                    swSourceLemmaCatFile.WriteLine(CreateLemmaCat(
                        zp.SourceZone.List.Select(s => s.SourceLemma.Text),
                        zp.SourceZone.List.Select(s => s.Category.Text)));
                    swTargetTextFile.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => t.TargetText.Text)));
                    swTargetLemmaFile.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => t.TargetLemma.Text)));
                    swTargetIdFile.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => t.TargetID.AsCanonicalString)));
                }
            }
        }

        // CL: There may be a better way to do this using Linq, but for now, this works.
        private static string CreateLemmaCat(IEnumerable<string> lemmas1, IEnumerable<string> categories1)
        {
            var lemmaList = lemmas1.ToList();
            var categoryList = categories1.ToList();
            string lemmaCatLine = string.Empty;

            for (int i = 0; i < lemmaList.Count; i++)
            {
                lemmaCatLine += string.Format("{0}_{1} ", lemmaList[i], categoryList[i]);
            }

            return lemmaCatLine.Trim();
        }

        // Even though we write all the files associated with a parallel corpus at the same time,
        // We want to import them based upon being give three files for source and target.
        // We will not distinguish between lemma and lemma_cat here, which means we will not read in
        // a category since it is not used by ClearEngine3. We just added it so we could write it out.
        public static ParallelCorpora ImportParallelCorpus(
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceIdFile,
            string targetTextFile,
            string targetLemmaFile,
            string targetIdFile)
        {
            string[] sourceTextLines = File.ReadAllLines(sourceTextFile);
            string[] sourceLemmaLines = File.ReadAllLines(sourceLemmaFile);            
            string[] sourceIdLines = File.ReadAllLines(sourceIdFile);
            string[] targetTextLines = File.ReadAllLines(targetTextFile);
            string[] targetLemmaLines = File.ReadAllLines(targetLemmaFile);
            string[] targetIdLines = File.ReadAllLines(targetIdFile);

            if (sourceIdLines.Length != targetIdLines.Length)
            {
                throw new InvalidDataException(
                    "Parallel files must have same number of lines.");
            }

            List<ZonePair> zonePairs = new();

            for (int i = 0; i < sourceIdLines.Length; i++)
            {
                string[] sourceText = sourceTextLines[i].Split();
                string[] sourceLemmas = sourceLemmaLines[i].Split();
                string[] sourceIDs = sourceIdLines[i].Split();
                string[] targetText = targetTextLines[i].Split();
                string[] targetLemmas = targetLemmaLines[i].Split();
                string[] targetIDs = targetIdLines[i].Split();


                List<Source> sourceList = new();
                for (int j = 0; j < sourceIDs.Length; j++)
                {
                    var source = new Source(
                        new SourceText(sourceText[j]),
                        new SourceLemma(sourceLemmas[j]),
                        new Category(string.Empty),
                        new SourceID(sourceIDs[j]));
                    sourceList.Add(source);
                }

                List<Target> targetList = new();
                for (int j = 0; j < targetIDs.Length; j++)
                {
                    var target = new Target(
                        new TargetText(targetText[j]),
                        new TargetLemma(targetLemmas[j]),
                        new TargetID(targetIDs[j]));
                    targetList.Add(target);
                }

                zonePairs.Add(new ZonePair(new SourceZone(sourceList), new TargetZone(targetList)));
            }

            return new ParallelCorpora(zonePairs);
        }



        public static void ExportTranslationModel(TranslationModel model, string filename)
        {
            StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8);

            foreach (var entry in model.Dictionary)
            {
                var source = entry.Key;
                var translations = entry.Value;

                foreach (var entry2 in translations)
                {
                    var target = entry2.Key;
                    var probability = entry2.Value;

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", source.Text, target.Text, probability.Double));
                }
            }

            sw.Close();

            // 2020.07.09 CL: Modified to also write a sorted file. More convenient than running another program after this is done in order to compare.
            // WriteTransModelSorted(model, filename);
        }
        

        // 2020.07.09 CL: Although this is inefficient code, by reading in the file again, we make sure we will have the same double value (replicate read method)
        // Basically uses the same code as the independent program previously written. Could use model rather than read in file.
        static void WriteTransModelSorted(TranslationModel model, string file)
        {
            int extIndex = file.LastIndexOf('.');
            string fileOut = file.Substring(0, extIndex) + "-sorted" + file.Substring(extIndex);

            StreamWriter sw = new StreamWriter(fileOut, false, Encoding.UTF8);

            var translationsProb = new SortedDictionary<string, double>(); // Use this to sort the data

            foreach (var entry in model.Dictionary)
            {
                var source = entry.Key.Text;
                var translations = entry.Value;

                foreach (var entry2 in translations)
                {
                    var target = entry2.Key.Text;
                    var prob = entry2.Value.Double;

                    string keyFraction = string.Format("{0}", 1 - prob); // Subtract from 1 to get probability in decreasing order.
                    string keyProb = source + " \t " + keyFraction + " \t " + target; // Need to include Ibaas in key since keys but be unique.

                    if (!translationsProb.ContainsKey(keyProb)) // Entry doesn't exist
                    {
                        translationsProb.Add(keyProb, prob); // Frequency order is from low to high. We would have to change the default function for comparing strings to change this order.
                    }
                    else // This should never be the case that there would be duplicate key but different probablity.
                    {
                        double oldProb = translationsProb[keyProb];
                        string errorMsg = string.Format(CultureInfo.InvariantCulture, "Duplicate Data in WriteTransModelSorted: {0} {1} with probability {2}. Old probability is {3}.", source, target, prob, oldProb);
                        Console.WriteLine(errorMsg);
                        sw.WriteLine(errorMsg);
                    }
                }
            }

            // Write ordered file by source then frequency
            foreach (KeyValuePair<string, double> entry in translationsProb)
            {
                string[] parts = entry.Key.Split('\t');
                string source = parts[0].Trim();
                string target = parts[2].Trim();
                double prob = entry.Value;

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", source, target, prob)); // If we want to make this sorted file more readable, we could change it to .txt files with " # " or "\t#\t" as the separator.
            }

            sw.Close();
        }

        //
        public static TranslationModel ImportTranslationModel(string filename)
        {
            var translationModel = new Dictionary<SourceLemma, Dictionary<TargetLemma, Score>>();

            string[] lines = File.ReadAllLines(filename);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                string[] parts = line.Split('\t');

                if (parts.Length == 3)
                {
                    var source = parts[0];
                    var target = parts[1];
                    double prob = double.Parse(parts[2]);

                    var sourceLemma = new SourceLemma(source);
                    var targetLemma = new TargetLemma(target);
                    var score = new Score(prob);

                    if (translationModel.ContainsKey(sourceLemma))
                    {
                        var translations = translationModel[sourceLemma];
                        translations.Add(targetLemma, score);
                    }
                    else
                    {
                        var translations = new Dictionary<TargetLemma, Score>();
                        translations.Add(targetLemma, score);
                        translationModel.Add(sourceLemma, translations);
                    }
                }
                else
                {
                    Console.WriteLine("ERROR in ImportTranslationModel() - Improper format of line {0}: {1}", i, line);
                }
            }

            return new TranslationModel(translationModel);
        }

        //
        public static void ExportAlignmentModel(AlignmentModel table, string filename)
        {
            StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8);

            foreach (var entry in table.Dictionary)
            {
                var bareLink = entry.Key;
                var prob = entry.Value;

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}-{1}\t{2}", bareLink.SourceID.AsCanonicalString, bareLink.TargetID.AsCanonicalString, prob.Double));
            }

            sw.Close();

            // WriteAlignModelSorted(table, filename);
        }
        

        // 2020.07.09 CL: Although this is inefficient code, by reading in the file again, we make sure we will have the same double value (replicate read method)
        // Basically uses the same code as the independent program previously written. Could use model rather than read in file.
        static void WriteAlignModelSorted(AlignmentModel table, string file)
        {
            int extIndex = file.LastIndexOf('.');
            string fileOut = file.Substring(0, extIndex) + "-sorted" + file.Substring(extIndex);

            StreamWriter sw = new StreamWriter(fileOut, false, Encoding.UTF8);

            var sortedModel = new SortedDictionary<string, double>(); // Use this to sort the data

            foreach (var entry in table.Dictionary)
            {
                var bareLink = entry.Key;
                var prob = entry.Value.Double;
                string pair = bareLink.SourceID.AsCanonicalString + "-" + bareLink.TargetID.AsCanonicalString;

                if (!sortedModel.ContainsKey(pair)) // Entry doesn't exist
                {
                    sortedModel.Add(pair, prob);
                }
                else // This should never be the case, but it might if the previous file was not created correctly.
                {
                    double oldProb = sortedModel[pair];
                    Console.WriteLine("Duplicate Data: {0} with probability {1}. Old probablity is {2}.", pair, prob, oldProb);
                }
            }

            // Write ordered file
            foreach (KeyValuePair<string, double> entry in sortedModel)
            {
                string pair = entry.Key;
                double prob = entry.Value;

                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}", pair, prob)); // If we want to make this sorted file more readable, we could change it to .txt files with " # " or "\t#\t" as the separator.
            }
            sw.Close();
        }

        //
        public static AlignmentModel ImportAlignmentModel(string filename)
        {
            var alignmentModel = new Dictionary<BareLink, Score>();

            string[] lines = File.ReadAllLines(filename);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                string[] parts = line.Split('\t');

                if (parts.Length == 2)
                {
                    var ids = parts[0].Split('-');
                    var sourceID = new SourceID(ids[0]);
                    var targetID = new TargetID(ids[1]);
                    double prob = double.Parse(parts[1]);

                    var bareLink = new BareLink(sourceID, targetID);
                    var score = new Score(prob);

                    if (!alignmentModel.ContainsKey(bareLink))
                    {
                        alignmentModel.Add(bareLink, score);
                    }
                    else
                    {
                        Console.WriteLine("ERROR in ImportAlignmentModel() - Duplicate link in line {0}: {1}", i, line);
                    }
                }
                else
                {
                    Console.WriteLine("ERROR in ImportAlignmentModel() - Improper format of line {0}: {1}", i, line);
                }
            }

            return new AlignmentModel(alignmentModel);
        }

        // We will need to eventually change the TargetVerseCorpus data structure to reflect that there is no longer a one-to-one relationship between surface word and lemma.
        // For now, we will just not write out the lemma file since it will be 
        public static void ExportTargetVerseCorpus(TargetVerseCorpus targetVerseCorpus, string textFile, string lemmaFile, string idFile)
        {
            using (StreamWriter swText = new StreamWriter(textFile, false, Encoding.UTF8))
            using (StreamWriter swLemma = new StreamWriter(lemmaFile, false, Encoding.UTF8))
            using (StreamWriter swID = new StreamWriter(idFile, false, Encoding.UTF8))
            {
                foreach (var targetVerse in targetVerseCorpus.List)
                {
                    string verseID = targetVerse.List[0].TargetID.AsCanonicalString.Substring(0, 8);
                    string textLine = string.Empty;
                    string lemmaLine = string.Empty;
                    string idLine = string.Empty;

                    foreach (var target in targetVerse.List)
                    {
                        textLine += target.TargetText.Text + " ";
                        lemmaLine += target.TargetLemma.Text + " ";
                        idLine += target.TargetID.AsCanonicalString + " ";
                    }

                    swText.WriteLine("{0}  {1}", verseID, textLine.Trim());
                    // swLemma.WriteLine("{0}  {1}", verseID, lemmaLine.Trim());
                    swID.WriteLine("{0}  {1}", verseID, idLine.Trim());
                }
            }
        }

        // We will need to eventually change the TargetVerseCorpus data structure to reflect that there is no longer a one-to-one relationship between surface word and lemma.
        // For now, we will just not read in the lemma file since it will be 
        public static TargetVerseCorpus ImportTargetVerseCorpus(string textFile, string lemmaFile, string idFile)
        {
            var targetVerseCorpus = new List<TargetVerse>();

            string[] textLines = File.ReadAllLines(textFile);
            string[] lemmaLines = File.ReadAllLines(lemmaFile);
            string[] idLines = File.ReadAllLines(idFile);
            
            for (int i = 0; i < lemmaLines.Length; i++)
            {
                var textLine = textLines[i];
                var lemmaLine = lemmaLines[i];
                var idLine = idLines[i];

                var words = textLine.Substring(textLine.IndexOf(' ')).Trim().Split();
                var lemmas = lemmaLine.Substring(lemmaLine.IndexOf(' ')).Trim().Split();
                var ids = idLine.Substring(idLine.IndexOf(' ')).Trim().Split();

                var verse = new List<Target>();

                for (int j = 0; j < lemmas.Length; j++)
                {
                    var word = words[j];
                    var lemma = lemmas[j];
                    var id = ids[j];

                    var targetText = new TargetText(word);
                    var targetLemma = new TargetLemma(lemma);
                    var targetID = new TargetID(id);

                    var target = new Target(targetText, targetLemma, targetID);
                    verse.Add(target);
                }

                var targetVerse = new TargetVerse(verse);
                targetVerseCorpus.Add(targetVerse);
            }           

            return new TargetVerseCorpus(targetVerseCorpus);
        }
    }
}
