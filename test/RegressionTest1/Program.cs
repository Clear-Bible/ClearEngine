
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;
using ClearBible.Clear3.SubTasks;

namespace RegressionTest1
{
    class Program
    {
        /// <summary>
        /// Regression test that imports a translation, uses a legacy
        /// versification to determine parallel zones, trains a statistical
        /// translation model, performs tree-based auto-alignment, and
        /// writes the result to a file in the legacy format.
        /// </summary>
        /// <remarks>
        /// This test was used during the initial development as Clear2 was
        /// incrementally transformed into the Clear3 prototype.  Some of these
        /// transformation steps changed the results slightly, thought to be
        /// because of issues being fixed and slight differences in the way
        /// that some of the algorithms are breaking ties.
        /// </remarks>
        /// 
        static void Main(string[] args)
        {
            Console.WriteLine("Regression Test 1");


            // Check that current directory is reasonable.

            DirectoryInfo dir = new DirectoryInfo(".");
            if (!dir.Name.Equals("TestSandbox1"))
            {
                Console.WriteLine("Expected current directory to be named TestSandbox1.");
                Console.WriteLine("You may need to set debug options, or perhaps modify this program.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Using sandbox directory:");
            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine();


            // Prepare for input and output.

            // option = 0 for Brief, 1 for Long.
            int option = 0;

            string[] inputFolders = { "InputBrief", "InputLong" };
            string[] outputFolders = { "OutputBrief", "OutputLong" };
            string[] referenceFolders = { "ReferenceBrief", "ReferenceLong" };

            string inputFolder = inputFolders[option];
            string outputFolder = outputFolders[option];
            string referenceFolder = referenceFolders[option];
            string commonFolder = "InputCommon";

            Func<string, string> prefix(string pre) =>
                s => Path.Combine(pre, s);

            Func<string, string>
                input = prefix(inputFolder),
                output = prefix(outputFolder),
                common = prefix(commonFolder),
                reference = prefix(referenceFolder);


            // Get ready to use the Clear3 API.

            IClear30ServiceAPI clearService =
                Clear30Service.FindOrCreate();

            IImportExportService importExportService =
                clearService.ImportExportService;

            IUtility utility = clearService.Utility;


            // Get the standard tree service.

            ITreeService treeService = GetStandardTreeServiceSubtask.Run(
                resourceFolder: "Resources");


            // Import auxiliary assumptions from files: punctuation,
            // stop words, function words, manual translation model,
            // good and bad links, old alignment, glossary table,
            // and Strongs data.

            (List<string> puncs,
             List<string> stopWords,
             List<string> sourceFunctionWords,
             List<string> targetFunctionWords,
             TranslationModel manTransModel,
             Dictionary<string, int> goodLinks,
             Dictionary<string, int> badLinks,
             Dictionary<string, Gloss> glossTable,
             GroupTranslationsTable groups,
             Dictionary<string, Dictionary<string, string>> oldLinks,
             Dictionary<string, Dictionary<string, int>> strongs)
             =
             ImportAuxAssumptionsSubTask.Run(
                 puncsPath: common("puncs.txt"),
                 stopWordsPath: common("stopWords.txt"),
                 sourceFuncWordsPath: common("sourceFuncWords.txt"),
                 targetFuncWordsPath: common("targetFuncWords.txt"),
                 manTransModelPath: common("manTransModel.txt"),
                 goodLinksPath: common("goodLinks.txt"),
                 badLinksPath: common("badLinks.txt"),
                 glossTablePath: common("Gloss.txt"),
                 groupsPath: common("groups.txt"),
                 oldAlignmentPath: common("oldAlignment.json"),
                 strongsPath: common("strongs.txt"));


            // Get the translation that is to be aligned.

            string versePath = input("Verse.txt");
            string lang = "English";

            Console.WriteLine("Tokenizing");

            TargetVerseCorpus targetVerseCorpus =
                importExportService.ImportTargetVerseCorpusFromLegacy(
                    versePath,
                    clearService.DefaultSegmenter,
                    puncs,
                    lang);


            // Import the versification.

            SimpleVersification simpleVersification =
                importExportService.ImportSimpleVersificationFromLegacy(
                    common("Versification.xml"),
                    "S1");
                    

            // Use the versification with the target verses to line up
            // translated zones with sourced zones.

            Console.WriteLine("Creating Parallel Corpora");

            ParallelCorpora parallelCorpora = utility.CreateParallelCorpora(
                targetVerseCorpus,
                treeService,
                simpleVersification);


            // Remove functions words from the parallel corpora, leaving
            // only the content words for the SMT step to follow.

            ParallelCorpora parallelCorporaCW =
               utility.FilterFunctionWordsFromParallelCorpora(
                   parallelCorpora,
                   sourceFunctionWords,
                   targetFunctionWords);


            // Train a statistical translation model using the parallel
            // corpora with content words only, producing an estimated
            // translation model and estimated alignment.

            Console.WriteLine("Building Models");
 
            (TranslationModel transModel2, AlignmentModel alignProbs2) =
                clearService.SMTService.DefaultSMT(
                    parallelCorporaCW);


            // Use the parallel corpora (with both the function words and
            // the content words included) to state the zone alignment
            // problems for the tree-based auto-aligner.

            List<ZoneAlignmentProblem> zoneAlignmentProblems =
                parallelCorpora.List
                .Select(zonePair =>
                    new ZoneAlignmentProblem(
                        zonePair.TargetZone,
                        zonePair.SourceZone.List.First().SourceID.VerseID,
                        zonePair.SourceZone.List.Last().SourceID.VerseID))
                .ToList();


            // Specify the assumptions to be used during the
            // tree-based auto-alignment.

            bool useAlignModel = true;
            int maxPaths = 1000000;
            int goodLinkMinCount = 3;
            int badLinkMinCount = 3;
            bool contentWordsOnly = true;

            IAutoAlignAssumptions assumptions =
                clearService.AutoAlignmentService.MakeStandardAssumptions(
                    transModel2,
                    manTransModel,
                    alignProbs2,
                    useAlignModel,
                    puncs,
                    stopWords,
                    goodLinks,
                    goodLinkMinCount,
                    badLinks,
                    badLinkMinCount,
                    oldLinks,
                    sourceFunctionWords,
                    targetFunctionWords,
                    contentWordsOnly,
                    strongs,
                    maxPaths);

            
            // Apply a tree-based auto-alignment to each of the zone
            // alignment problems, producing an alignment datum in the
            // persistent format.

            Console.WriteLine("Auto Alignment");

            LegacyPersistentAlignment alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    zoneAlignmentProblems,
                    treeService,
                    glossTable,
                    assumptions);


            // Export the persistent-format datum to a file.

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(output("alignment.json"), json);


            Console.WriteLine("Done");
        }
    }
}
