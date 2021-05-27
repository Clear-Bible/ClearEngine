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
using ClearBible.Clear3.Impl.AutoAlign;

namespace RegressionTest3
{
    /// <summary>
    /// Regression test that exercises the tree-based auto-aligner
    /// using input files.  This test assumes that the alignment problem
    /// has already been stated and the statistical translation model has
    /// already been trained, and imports the appropriate collateral from
    /// instead of computing it again.
    /// </summary>
    /// <remarks>
    /// This test was used during the initial development as Clear2 was
    /// incrementally transformed into the Clear3 prototype.  Some of these
    /// transformation steps changed the results slightly, thought to be
    /// because of issues being fixed and slight differences in the way
    /// that some of the algorithms are breaking ties.
    /// </remarks>
    /// 
    /// 
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("---------------------------");
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
                 manTransModelPath: InPath("manTransModel.tsv"),
                 goodLinksPath: InPath("goodLinks.tsv"),
                 badLinksPath: InPath("badLinks.tsv"),
                 glossTablePath: InPath("Gloss.tsv"),
                 groupsPath: InPath("groups.tsv"),
                 oldAlignmentPath: InPath("oldAlignment.json"),
                 strongsPath: InPath("strongs.txt"));


            // Get the standard tree service.

            ITreeService treeService = GetStandardTreeServiceSubtask.Run(
                resourceFolder: "Resources");


            // Get ready to use the Clear3 API.

            IClear30ServiceAPI clearService = Clear30Service.FindOrCreate();

            IImportExportService importExportService =
                clearService.ImportExportService;


            // Import the translation that is to be aligned.

            List<ZoneAlignmentProblem> zoneAlignmentFactsList =
                importExportService.ImportZoneAlignmentProblemsFromLegacy(
                    parallelSourcePath: InPath("source.id.lemma.txt"),
                    parallelTargetPath: InPath("target.id.txt"));


            // Import the translation model and alignment model, as
            // produced from a prior STM step, from files.

            TranslationModel transModel =
                importExportService.ImportTranslationModel(
                    InPath("transModel.tsv"));

            AlignmentModel alignmentModel =
                importExportService.ImportAlignmentModel(
                    InPath("alignModel.tsv"));


            // Specify the assumptions to be used during the
            // auto-alignment.

            IAutoAlignAssumptions assumptions =
                clearService.AutoAlignmentService.MakeStandardAssumptions(
                    translationModel: transModel,
                    translationModelTC: transModel,
                    useLemmaCatModel: false,
                    manTransModel: manTransModel,
                    alignProbs: alignmentModel,
                    alignProbsPre: alignmentModel,
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

            Stopwatch stopwatch = Stopwatch.StartNew();

            LegacyPersistentAlignment alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    zoneAlignmentFactsList,
                    treeService,
                    glossTable,
                    assumptions);

            stopwatch.Stop();
            Console.WriteLine($"milliseconds: {stopwatch.ElapsedMilliseconds}");

            // Export from the Alignment2 format to a file.

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(OutPath("alignment.json"), json);

            Console.WriteLine("Done.");

            //Console.WriteLine($"Max Candidates: {AutoAlignmentService.MaxCandidates}");
        }
    }
}
