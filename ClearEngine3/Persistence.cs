using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;


using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;
using ClearBible.Clear3.SubTasks;

namespace ClearEngine3
{
    public class Persistence
    {
        public static void ExportParallelCorpora(
            ParallelCorpora parallelCorpora,
            string sourceFilename,
            string sourceIdFilename,
            string targetFileName,
            string targetIdFilename)
        {
            using (StreamWriter sw1 = new StreamWriter(sourceFilename, false, Encoding.UTF8))
            using (StreamWriter sw2 = new StreamWriter(sourceIdFilename, false, Encoding.UTF8))
            using (StreamWriter sw3 = new StreamWriter(targetFileName, false, Encoding.UTF8))
            using (StreamWriter sw4 = new StreamWriter(targetIdFilename, false, Encoding.UTF8))
            {
                foreach (ZonePair zp in parallelCorpora.List)
                {
                    sw1.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => s.Lemma.Text)));
                    sw2.WriteLine(string.Join(" ",
                        zp.SourceZone.List.Select(s => $"x_{s.SourceID.AsCanonicalString}")));
                    sw3.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => t.TargetText.Text.ToLower())));
                    sw4.WriteLine(string.Join(" ",
                        zp.TargetZone.List.Select(t => $"x_{t.TargetID.AsCanonicalString}")));
                }
            }
        }

        /*
        public record Source(
            SourceText SourceText,
            Lemma Lemma,
            SourceID SourceID);

        public record Target(
            TargetText TargetText,
            TargetID TargetID);
        */
        public static ParallelCorpora ImportParallelCorpora(
            string sourceFilename,
            string sourceIdFilename,
            string targetFileName,
            string targetIdFilename)
        {
            // Prepare to collect ZonePair objects.
            List<ZonePair> zonePairs = new();

            string[] sourceLines = File.ReadAllLines(sourceFilename);
            foreach (string line in sourceLines)
            {
                string[] parts = line.Split(' ');
                foreach (var part in parts)
                {

                }
            }

            string[] sourceIdLines = File.ReadAllLines(sourceIdFilename);
            foreach (string line in sourceIdLines)
            {
                string[] parts = line.Split(' ');
                foreach (var part in parts)
                {

                }
            }

            string[] targetLines = File.ReadAllLines(targetFileName);
            foreach (string line in targetLines)
            {
                string[] parts = line.Split(' ');
                foreach (var part in parts)
                {

                }
            }

            string[] targetIdLines = File.ReadAllLines(targetIdFilename);
            foreach (string line in targetIdLines)
            {
                string[] parts = line.Split(' ');
                foreach (var part in parts)
                {

                }
            }
            /*

            foreach (SimpleZoneSpec zoneSpec in simpleVersification.List)
            {
                // Get the Target objects for the verses in this zone,
                // in order.
                List<Target> targets =
                    zoneSpec.TargetVerses
                    .SelectMany(tVerseID =>
                    {
                        if (targetVerseTable.TryGetValue(tVerseID,
                            out TargetVerse targetVerse))
                        {
                            return targetVerse.List;
                        }
                        else return Enumerable.Empty<Target>();
                    })
                    .ToList();

                    // Get the Source objects for the zone.
                    List<Source> sources =
                        zoneSpec.SourceVerses
                        .SelectMany(sVerseID =>
                            treeService.GetSourceVerse(sVerseID).List)
                        .ToList();

                    // If any Source objects were found:

                        // Add a new ZonePair to the collection.
                        zonePairs.Add(
                            new ZonePair(
                                new SourceZone(sources),
                                new TargetZone(targets)));

            }
            */

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
            WriteTransModelSorted(model, filename);
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
                        Console.WriteLine("Duplicate Data in WriteTransModelSorted: {0} {1} with probability {2}. Old probability is {3}.", source, target, prob, oldProb);
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "// Duplicate Data in WriteTransModelSorted: source = {0} target = {1} with probability {2}. Old probability is {3}.", source, target, prob, oldProb));
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

            WriteAlignModelSorted(table, filename);
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

    }
}
