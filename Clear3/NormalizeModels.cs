using System;
using System.Collections.Generic;
using System.Linq;

using TransModels;
using ClearBible.Clear3.API;

namespace Clear3
{
    public class NormalizeModels
    {
        // Need to write to and read from file because double numbers will change during this process.
        // We want to make sure if we reuse the files, the resulting models will have the same double values.
        public static void BuildNormalizedTransModel(
            TranslationModel translationModel,
            string transModelFileNorm)
        {
            var transModelNormalized = NormalizeTransModelProbabilities(translationModel);

            // BuildTransModels.WriteTransModel(transModelNormalized, transModelFileNorm);
            Persistence.ExportTranslationModel(transModelNormalized, transModelFileNorm);
            

            return;
        }

        public static void BuildNormalizedAlignModel(
            AlignmentModel alignmentModel,
            string alignModelFileNorm)
        {
            var alignModelNormalized = NormalizeAlignModelProbabilities(alignmentModel);

            // BuildTransModels.WriteAlignModel(alignModelNormalized, alignModelFileNorm);
            Persistence.ExportAlignmentModel(alignModelNormalized, alignModelFileNorm);

            return;
        }

        // Different SMT models, and processing of different languages, result in probabilties that have characterisitcs
        // that differ from each other.
        // However, in the auto alignment algorithm, there is a place where the alignment probability from the SMT model
        private static AlignmentModel NormalizeAlignModelProbabilities(AlignmentModel alignmentModel)
        {
            var originalProbs = alignmentModel.Dictionary.Values.ToList();

            (var mean, var stdDev) = CalculateStandardDeviation(originalProbs);

            Console.WriteLine("NormalizeAlignModelProbabilities() orig: mean = {0}, stdev = {1}", mean, stdDev);

            // We currently don't check here to see if the stdDev is zero, but we could to make things more efficient.

            double lowestProb = 0.05;
            double highestProb = 0.95;
            double span = highestProb - lowestProb;
            double newStdDev = span / 6;
            double newMean = (span / 2) + lowestProb;

            var alignmentModelNormalized = Normalize(alignmentModel, mean, stdDev, newStdDev, newMean);

            // Calculate new mean and standard deviation (out of curiosity)

            var newProbs = alignmentModelNormalized.Dictionary.Values.ToList();

            (var meanNorm, var stdDevNorm) = CalculateStandardDeviation(newProbs);

            Console.WriteLine("NormalizeAlignModelProbabilities() norm: mean = {0}, stdev = {1}", meanNorm, stdDevNorm);

            return alignmentModelNormalized;
        }

        //
        private static TranslationModel NormalizeTransModelProbabilities(TranslationModel translationModel)
        {
            var transModelNormalized = new Dictionary<SourceLemma, Dictionary<TargetLemma, Score>>();

            var originalProbs = CollectTransModelProbabilities(translationModel);

            (var mean, var stdDev) = CalculateStandardDeviation(originalProbs);

            Console.WriteLine("NormalizeTransModelProbabilities() orig: mean = {0}, stdev = {1}", mean, stdDev);

            // We currently don't check here to see if the stdDev is zero, but we could to make things more efficient.

            double lowestProb = 0.05;
            double highestProb = 0.95;
            double span = highestProb - lowestProb;
            double newStdDev = span / 6;
            double newMean = (span / 2) + lowestProb;

            foreach (var entry in translationModel.Dictionary)
            {
                var source = entry.Key;
                var translations = entry.Value;
                var translationsNormalized = Normalize(translations, mean, stdDev, newStdDev, newMean);

                transModelNormalized.Add(source, translationsNormalized);
            }

            var translationModelNormalized = new TranslationModel(transModelNormalized);

            var newProbs = CollectTransModelProbabilities(translationModelNormalized);

            (var meanNorm, var stdDevNorm) = CalculateStandardDeviation(newProbs);

            Console.WriteLine("NormalizeTransModelProbabilities() norm: mean = {0}, stdev = {1}", meanNorm, stdDevNorm);

            return translationModelNormalized;
        }


        private static List<Score> CollectTransModelProbabilities(TranslationModel translationModel)
        {
            var probs = new List<Score>();

            foreach (var entry in translationModel.Dictionary)
            {
                var translations = entry.Value;
                var newProbs = translations.Values.ToList();

                probs.AddRange(newProbs);
            }

            return probs;
        }

        private static AlignmentModel Normalize(AlignmentModel alignmentModel, double mean, double stdDev, double newStdDev, double newMean)
        {
            var normlizedModel = new Dictionary<BareLink, Score>();

            // We currently don't check here to see if the stdDev is zero, but we could to make things more efficient.

            foreach (var entry in alignmentModel.Dictionary)
            {
                var key = entry.Key;
                var prob = entry.Value;
                var newProb = GetNewProbability(prob, mean, stdDev, newStdDev, newMean);
                normlizedModel.Add(key, newProb);
            }

            return new AlignmentModel(normlizedModel);
        }

        private static Dictionary<TargetLemma, Score> Normalize(Dictionary<TargetLemma, Score> model, double mean, double stdDev, double newStdDev, double newMean)
        {
            var normlizedModel = new Dictionary<TargetLemma, Score>();

            // We currently don't check here to see if the stdDev is zero, but we could to make things more efficient.

            foreach (var entry in model)
            {
                var key = entry.Key;
                var prob = entry.Value;
                var newProb = GetNewProbability(prob, mean, stdDev, newStdDev, newMean);
                normlizedModel.Add(key, newProb);
            }

            return normlizedModel;
        }

        private static Score GetNewProbability(Score prob, double mean, double stdDev, double newStdDev, double newMean)
        {
            // IBM1 returns no probability for alignments so implementations will just return the same probability.
            // This results in a standard deviation of zero. We can't divide by zero.
            // So if the standard deviation of the original probabilities is zero, use a zScore of 0 instead.
            double zScore = 0;
            if (stdDev != 0)
            {
                zScore = (prob.Double - mean) / stdDev;
            }
            

            // Make outliners the maximum possible
            if (zScore > 3)
            {
                zScore = 3;
            }
            else if (zScore < -3)
            {
                zScore = -3;
            }

            // Interpolate to get the value
            double newProb = (newStdDev * zScore) + newMean;

            return new Score(newProb);
        }


        // Return the standard deviation of an array of Doubles.
        //
        // If the second argument is True, evaluate as a sample.
        // If the second argument is False, evaluate as a population.
        /*
        private static (double, double) CalculateStandardDeviation(IEnumerable<double> values,
            bool as_sample)
        {
            // Get the mean.
            double mean = values.Sum() / values.Count();
            double standardDeviation = 0;

            // Get the sum of the squares of the differences
            // between the values and the mean.
            var squares_query =
                from double value in values
                select (value - mean) * (value - mean);
            double sum_of_squares = squares_query.Sum();

            if (as_sample)
            {
                standardDeviation = Math.Sqrt(sum_of_squares / (values.Count() - 1));
            }
            else
            {
                standardDeviation = Math.Sqrt(sum_of_squares / values.Count());
            }

            return (mean, standardDeviation);
        }
        */

        // Population Standard Deviation for double values
        // For Sample Standard Deviation, divide sum by N (i.e. values.Count()), not N-1
        private static (double, double) CalculateStandardDeviation(List<Score> scores)
        {
            double standardDeviation = 0;
            double avg = 0;
            var values = ConvertToIEnumerableDouble(scores);

            if (values.Any())
            {
                // Compute the average.     
                avg = values.Average();

                // Perform the Sum of (value-avg)_2_2.      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                // Put it all together.      
                standardDeviation = Math.Sqrt((sum) / (values.Count() - 1));
            }

            return (avg, standardDeviation);
        }

        private static List<double> ConvertToIEnumerableDouble(List<Score> scores)
        {
            var ieDouble = new List<double>();

            foreach (var score in scores)
            {
                ieDouble.Add(score.Double);
            }

            return ieDouble;
        }
    }
}
