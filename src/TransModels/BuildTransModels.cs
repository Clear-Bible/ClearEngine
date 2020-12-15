using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Globalization;

using Models;

namespace TransModels
{
    public class BuildTransModels
    {
        // Max's model builder: Given parallel files, build both the translation model and alignment model
        public static void BuildModels(
            string sourceFile, // source text in verse per line format
            string targetFile, // target text in verse per line format
            string sourceIdFile, // source text in verse per line format, with ID for each word
            string targetIdFile, // target text in verse per line format, with ID for each word
            string runSpec, // specification for the number of iterations to run for the IBM model and the HMM model (e.g. 1:10;H:5 -- IBM model 10 iterations and HMM model 5 iterations)
            double epsilon, // threhold for a translation pair to be kept in translation model (e.g. 0.1 -- only pairs whole probability is greater than or equal to 0.1 are kept)
            string transModel, // name of the file containing the translation model
            string alignModel // name of the file containing the translation model
            )
        {
            AlignModels models = new AlignModels();
            ModelBuilder modelBuilder = new ModelBuilder();

            // These are required
            modelBuilder.SourceFile = sourceFile;
            modelBuilder.TargetFile = targetFile;
            modelBuilder.RunSpecification = runSpec;

            // If you don't specify, you get no symmetry
            modelBuilder.Symmetrization = SymmetrizationType.Min;

            //Train the model
            using (ConsoleProgressBar progressBar = new ConsoleProgressBar(Console.Out))
            {
                modelBuilder.Train(progressBar);
            }

            // Dump the translation table with epsilon
            models.TransModel = modelBuilder.GetTranslationTable(epsilon);

            StreamWriter sw2 = new StreamWriter(alignModel, false, Encoding.UTF8);

            ArrayList sourceIdList = GetTexts(sourceIdFile);
            ArrayList targetIdList = GetTexts(targetIdFile);
            Models.Alignments allAlignments = modelBuilder.GetAlignments(0);
            int i = 0;
            foreach (List<Alignment> alignments in allAlignments)
            {
                string sourceWords = (string)sourceIdList[i];
                string targetWords = (string)targetIdList[i];
                targetWords = targetWords.Replace("  ", " ");
                string[] sWords = sourceWords.Split(" ".ToCharArray());
                string[] tWords = targetWords.Split(" ".ToCharArray());
                foreach (Alignment alignment in alignments)
                {
                    int sourceIndex = alignment.Source;
                    int targetIndex = alignment.Target;
                    double prob = alignment.AlignProb;
                    try
                    {
                        string sourceWord = sWords[sourceIndex];
                        string targetWord = tWords[targetIndex];
                        string sourceID = sourceWord.Substring(sourceWord.LastIndexOf("_") + 1);
                        string targetID = targetWord.Substring(targetWord.LastIndexOf("_") + 1);
                        string pair = sourceID + "-" + targetID;
                        models.AlignModel.Add(pair, prob);
                        sw2.WriteLine("{0}\t{1}", pair, prob);
                    }
                    catch
                    {
                        Console.WriteLine("BuildTransModel() Index out of bound: {0} {1}", sourceIndex, targetIndex);
                    }
                }

                i++;
            }

            sw2.Close();

            StreamWriter sw = new StreamWriter(transModel, false, Encoding.UTF8);

            IDictionaryEnumerator modelEnum = models.TransModel.GetEnumerator();

            while (modelEnum.MoveNext())
            {
                string source = (string)modelEnum.Key;
                Hashtable translations = (Hashtable)modelEnum.Value;

                IDictionaryEnumerator transEnum = translations.GetEnumerator();

                while (transEnum.MoveNext())
                {
                    string translation = (string)transEnum.Key;
                    double transPro = (double)transEnum.Value;

                    sw.WriteLine("{0}\t{1}\t{2}", source, translation, transPro);
                }
            }

            sw.Close();
        }

        public static Hashtable GetTranslationModel(string file)
        {
            Hashtable transModel = new Hashtable();

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                string[] groups = line.Split("\t".ToCharArray());
                if (groups.Length == 3)
                {
                    string source = groups[0].Trim();
                    string target = groups[1].Trim();
                    string sProb = groups[2].Trim();
                    double prob = Double.Parse(sProb);

                    if (transModel.ContainsKey(source))
                    {
                        Hashtable translations = (Hashtable)transModel[source];
                        translations.Add(target, prob);
                    }
                    else
                    {
                        Hashtable translations = new Hashtable();
                        translations.Add(target, prob);
                        transModel.Add(source, translations);
                    }
                }
            }

            return transModel;
        }

        static Hashtable GetAlignmentModel(string alignFile)
        {
            Hashtable alignModel = new Hashtable();

            string[] lines = File.ReadAllLines(alignFile);
            foreach (string line in lines)
            {
                string[] groups = line.Split("\t".ToCharArray());
                if (groups.Length == 2)
                {
                    string pair = groups[0];
                    double prob = Double.Parse(groups[1]);
                    alignModel.Add(pair, prob);
                }
            }

            return alignModel;
        }

        public static ArrayList GetTexts(string file)
        {
            ArrayList texts = new ArrayList();

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {

                    texts.Add(line.Trim());
                }
            }

            return texts;
        }
    }

    public class AlignModels
    {
        public Hashtable TransModel = new Hashtable();
        public Hashtable AlignModel = new Hashtable();
    }
}

