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

namespace RegressionTest3
{
    /// <summary>
    /// Test that exercises GBI_Aligner as a basis for study and rework.
    /// </summary>
    /// 
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Regression Test 3.");

            string inputFolder = Path.Combine(".", "Input");
            string outputFolder = Path.Combine(".", "Output");
            string treeFolder =
                Path.Combine("..", "TestSandbox1", "SyntaxTrees");

            string InPath(string path) => Path.Combine(inputFolder, path);
            string OutPath(string path) => Path.Combine(outputFolder, path);

            string parallelSourceIdPath = InPath("source.id.txt");
            string parallelSourceIdLemmaPath = InPath("source.id.lemma.txt");
            string parallelTargetIdPath = InPath("target.id.txt");
            string transModelPath = InPath("transModel.txt");
            string alignModelPath = InPath("alignModel.txt");


            string jsonOutput = OutPath("alignment.json");


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

            IClear30ServiceAPI clearService =
                Clear30Service.FindOrCreate();

            IImportExportService importExportService =
                clearService.ImportExportService;

            IResourceService resourceService = clearService.ResourceService;
            resourceService.SetLocalResourceFolder("Resources");

            Uri treebankUri =
                new Uri("https://id.clear.bible/treebank/Clear3Dev");

            if (!resourceService.QueryLocalResources()
                .Any(r => r.Id.Equals(treebankUri)))
            {
                resourceService.DownloadResource(treebankUri);
            }

            ITreeService treeService =
                resourceService.GetTreeService(treebankUri);

            // Proposal: URIs of the form http://id.clear.bible/... serve
            // metadata about the resource, either as RDF or HTML.
            // See also: https://www.w3.org/TR/cooluris/
            // The metadata also points to a location in Github with
            // the gzipped data for the resource.
            // Clear3 uses the machine-readable metadata to download
            // resources when so requested.

            
            List<TranslationPair> translationPairs =
                importExportService.ImportTranslationPairsFromLegacy(
                    parallelSourceIdLemmaPath,
                    parallelTargetIdPath);

            TranslationModel transModel =
                importExportService.ImportTranslationModel(transModelPath);

            AlignmentModel alignmentModel =
                importExportService.ImportAlignmentModel(alignModelPath);
            
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

            Alignment2 alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    translationPairs,
                    treeService,
                    glossTable,
                    assumptions);

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonOutput, json);

            Console.WriteLine("Done.");
        }
    }
}
