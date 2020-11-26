using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Newtonsoft.Json;


using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;
using ClearBible.Clear3.SubTasks;
using ClearBible.Clear3.Subtasks;

namespace RegressionTest3
{
    /// <summary>
    /// Regression test that exercises the tree-based auto-aligner
    /// using input files.  Intended for study, rework, and smoke
    /// test.
    /// </summary>
    /// 
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Regression Test 3.");

            // Establish input and output folders.

            string inputFolder = Path.Combine(".", "Input");
            string outputFolder = Path.Combine(".", "Output");

            string InPath(string path) => Path.Combine(inputFolder, path);
            string OutPath(string path) => Path.Combine(outputFolder, path);

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
                 puncsPath: InPath("puncs.txt"),
                 stopWordsPath: InPath("stopWords.txt"),
                 sourceFuncWordsPath: InPath("sourceFuncWords.txt"),
                 targetFuncWordsPath: InPath("targetFuncWords.txt"),
                 manTransModelPath: InPath("manTransModel.txt"),
                 goodLinksPath: InPath("goodLinks.txt"),
                 badLinksPath: InPath("badLinks.txt"),
                 glossTablePath: InPath("Gloss.txt"),
                 groupsPath: InPath("groups.txt"),
                 oldAlignmentPath: InPath("oldAlignment.json"),
                 strongsPath: InPath("strongs.txt"));

            // Get the standard tree service.

            ITreeService treeService = GetStandardTreeServiceSubtask.Run(
                resourceFolder: "Resources");

            // Get ready to use the Clear3 API.

            IClear30ServiceAPI clearService = Clear30Service.FindOrCreate();

            IImportExportService importExportService =
                clearService.ImportExportService;


            // Import translation pairs from a file.

            List<ZoneAlignmentFacts> zoneAlignmentFactsList =
                importExportService.ImportZoneAlignmentFactsFromLegacy(
                    parallelSourcePath: InPath("source.id.lemma.txt"),
                    parallelTargetPath: InPath("target.id.txt"));

            // Import the translation model and alignment model, as
            // produced from a prior STM step, from files.

            TranslationModel transModel =
                importExportService.ImportTranslationModel(
                    InPath("transModel.txt"));

            AlignmentModel alignmentModel =
                importExportService.ImportAlignmentModel(
                    InPath("alignModel.txt"));

            // Specify the assumptions to be used during the
            // auto-alignment.
            
            IAutoAlignAssumptions assumptions =
                clearService.AutoAlignmentService.MakeStandardAssumptions(
                    translationModel: transModel,
                    manTransModel: manTransModel,
                    alignProbs: alignmentModel,
                    useAlignModel: true,
                    puncs: puncs,
                    stopWords: stopWords,
                    goodLinks: goodLinks,
                    goodLinkMinCount: 3,
                    badLinks: badLinks,
                    badLinkMinCount: 3,
                    oldLinks: oldLinks,
                    sourceFuncWords: sourceFunctionWords,
                    targetFuncWords: targetFunctionWords,
                    contentWordsOnly: true,
                    strongs: strongs,
                    maxPaths: 1000000);

            Console.WriteLine("Calling Auto Aligner.");

            // Auto-align the translation pairs, using the tree service,
            // glossary, and assumptions, to produce an alignment expressed
            // in the Alignment2 format.

            Alignment2 alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    zoneAlignmentFactsList,
                    treeService,
                    glossTable,
                    assumptions);

            // Export from the Alignment2 format to a file.

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(OutPath("alignment.json"), json);

            Console.WriteLine("Done.");
        }
    }
}
