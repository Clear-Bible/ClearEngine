﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Globalization;

using Newtonsoft.Json;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;
using ClearBible.Clear3.SubTasks;


namespace ClearEngine3
{
    public class ActionsClear3
    {
        public static void SetProject(string argProject)
        {
            project = argProject;
        }

        public static void SetTestament(string argTestament)
        {
            testament = argTestament;
        }

        public static void SetContentWordsOnly(string argContentWordsOnly)
        {
            contentWordsOnly = (argContentWordsOnly == "true");
        }

        public static void SetRunSpec(string argRunSpec)
        {
            runSpec = argRunSpec;
        }

        public static void SetEpsilon(string arg)
        {
            epsilon = double.Parse(arg);
        }

        public static void SetThotModel(string arg)
        {
            thotModel = arg;
        }

        public static void SetThotHeuristic(string arg)
        {
            thotHeuristic = arg;
        }

        public static void SetThotIterations(string arg)
        {
            thotIterations = arg;
        }

        public static void SetSmtContentWordsOnly(string arg)
        {
            smtContentWordsOnly = (arg == "true");
        }

        public static void InitializeConfig()
        {
            Console.WriteLine();
            Console.WriteLine("Running ClearEngine 3");

            clearConfigFilename = "CLEAR.config"; // default configuration file
            ReadConfig(clearConfigFilename);
        }

        // CL: 2020.08.21 Modified to use a .config (XML) file to store all of the filenames and booleans rather than embedding them in the code.
        // They can be initialized from the configuration file, and different ones can be used (by manual copying of files) or through a command line option.
        // Once loaded in at startup, they can be changed by command line options.
        // This was added to avoid having to change code because you want to use different files.
        // This was done by using Forms and textBoxes, but only for some of the configuration. Others were embedded in the code.
        private static void ReadConfig(string configFilename)
        {
            clearSettings = Configuration.GetSettings(configFilename);

            resourcesFolder = (string)clearSettings["ResourcesFolder"]; // e.g. 
            processingFolder = (string)clearSettings["ProcessingFolder"]; // e.g. 

            runConfigFile = (string)clearSettings["RunConfigFile"];
            runSettings = Configuration.GetSettings(runConfigFile);

            processingConfigFilename = (string)clearSettings["ProcessingConfigFile"];
            processingSettings = Configuration.GetSettings(processingConfigFilename);

            project = (string)runSettings["Project"]; // e.g. "NIV84-SIL-test"
            testament = (string)runSettings["Testament"]; // e.g. "OT" or "NT"

            // Set Processing Parameters
            runSpec = (string)processingSettings["RunSpec2"]; // e.g. 1:10;H:5
            epsilon = Double.Parse((string)processingSettings["Epsilon"]); // Must exceed this to be counted into model, e.g. "0.1"
            thotModel = (string)processingSettings["ThotModel"];
            thotHeuristic = (string)processingSettings["ThotHeuristic"];
            thotIterations = (string)processingSettings["ThotIterations"];
            smtContentWordsOnly = ((string)processingSettings["ContentWordsSMT"] == "true"); // e.g. "true" Only use content words for building models
            contentWordsOnly = ((string)processingSettings["ContentWordsOnly"] == "true"); // e.g. "true" Only align content words

            useAlignModel = ((string)processingSettings["UseAlignModel"] == "true"); // e.g. "true"
            badLinkMinCount = Int32.Parse((string)processingSettings["BadLinkMinCount"]); // e.g. "3", the minimal count required for treating a link as bad
            goodLinkMinCount = Int32.Parse((string)processingSettings["GoodLinkMinCount"]); // e.g. "3" the minimal count required for treating a link as bad


            // Set Translation Parameters
            translationConfigFilename = (string)clearSettings["TranslationConfigFile"];

            // Set file information in resourcesFolder
            sourceFoldername = (string)clearSettings["SourceFolder"];
            treeFoldername = (string)clearSettings["TreeFolder"];
            initialFilesFoldername = (string)clearSettings["InitialFilesFolder"];
            freqPhrasesFilename = (string)clearSettings["FreqPhrasesFile"];
            sourceFuncWordsFilename = (string)clearSettings["SourceFuncWordsFile"];
            puncsFilename = (string)clearSettings["PuncsFile"];
            glossFilename = (string)clearSettings["GlossFile"];
            versificationFilenameDefault = (string)clearSettings["Versification_File"];
            versificationTypeDefault = (string)clearSettings["Versification_Type"];

            //============================ Output/Input Files Used to Pass Data Between Functions ============================
            //
            // tokenFilename = (string)clearSettings["TokenFile"]; // e.g. "tokens.txt"
            // tokenLemmaFilename = (string)clearSettings["TokenLemmaFile"]; // e.g. "tokens.lower.txt", Not currently used

            // sourceTextFilenameM = (string)clearSettings["SourceTextFileM"]; // e.g. 
            // sourceIdFilenameM = (string)clearSettings["SourceIdFileM"]; // e.g. 
            // sourceLemmaFilenameM = (string)clearSettings["SourceLemmaFileM"]; // e.g. 

            // sourceTextFilename = (string)clearSettings["SourceTextFile"]; // e.g. "source.txt"
            sourceIdFilename = (string)clearSettings["SourceIdFile"]; // e.g. "source.id.txt"
            sourceLemmaFilename = (string)clearSettings["SourceLemmaFile"]; // e.g. source.id.lemma.txt"
            // targetTextFilename = (string)clearSettings["TargetTextFile"]; // e.g. "target.txt"
            targetIdFilename = (string)clearSettings["TargetIdFile"]; // e.g. "target.id.txt"
            targetLemmaFilename = (string)clearSettings["TargetLemmaFile"]; // e.g "target.id.lemma.txt"

            targetPuncFilename = (string)clearSettings["TargetPuncFile"]; // e.g. "target.punc.txt"
            targetPuncLowerFilename = (string)clearSettings["TargetPuncLowerFile"]; // "target.punc.lower.txt", Not currently used

            sourceLemmaFilenameCW = (string)clearSettings["SourceLemmaFileCW"]; // e.g "sourceFile.cw.txt", Should update variable to ...File
            sourceIdFilenameCW = (string)clearSettings["SourceIdFileCW"]; // e.g "sourceFile.id.cw.txt", Should update variable to ...File
            targetLemmaFilenameCW = (string)clearSettings["TargetLemmaFileCW"]; // e.g "targetFile.cw.txt", Should update variable to ...File
            targetIdFilenameCW = (string)clearSettings["TargetIdFileCW"]; // e.g "targetFile.id.cw.txt", Should update variable to ...File

            //============================ Output Files Only ============================
            // Files not part of the state, nor used as output/input to pass data between different functions
            // Output file. Has the alignment in .json format, which is more readable than XML format.
            jsonOutputFilename = (string)clearSettings["JsonOutputFile"]; // e.g "alignment.json", Should update variable to ...File
            jsonFilename = (string)clearSettings["JsonFile"]; // e.g "alignment.json", Should merge with jsonOutput

            // Output file. Has the alignment in .json format, which is more readable than XML format. Gateway language alignment. Manuscript to gateway, or gateway to target?
            t2gJsonFilename = (string)clearSettings["T2GJsonFile"]; // e.g. "gAlignment.json"

            //============================ Input Files Only ============================
            versesFilename = (string)clearSettings["VersesFile"]; // e.g. "Verses.txt"
            rawFilename = (string)clearSettings["RawFile"]; // e.g. "Verses.txt"
            checkedAlignmentsFilename = (string)clearSettings["CheckedAlignmentsFile"]; // e.g. "CheckedAlignments.json"
            m_g_jsonFilename = (string)clearSettings["M_G_JsonFile"]; // e.g "m_g_alignment.json", the CLEAR json where manuscript, gTranslation and gLinks are instantiated but translation and links are still empty
            gAlignmentFilename = (string)clearSettings["GAlignmentFile"]; // e.g. "gAlignment.json"
            auto_m_t_alignmentFilename = (string)clearSettings["Auto_M_T_AlignmentFile"]; // e.g. "auto_m_t_alignment.json"

            // Initialize state related filenames
            InitializeStateFilenames();
        }

        // 2020.10.19 CL: Need to separate out when we set the filenames and when we set the files (with path).
        // This is because the path may change when the project, testament, or filenames change as a command line option.
        public static void InitializeFiles()
        {
            // Set Translation Parameters
            translationFolder = Path.Combine(processingFolder, project);
            translationConfigFile = Path.Combine(translationFolder, translationConfigFilename);

            // If there is not a specific translaiton configuration file (which there should be), use the generic/default one.
            if (!File.Exists(translationConfigFile))
            {
                translationConfigFile = translationConfigFilename;
            }

            translationSettings = Configuration.GetSettings(translationConfigFile);

            // Set variables related to which language, translation, and testament to process.

            lang = (string)translationSettings["Language"]; // e.g. "English"
            translation = (string)translationSettings["Translation"]; // e.g. NIV84
            versificationFilename = (string)translationSettings["Versification_File"];
            versificationType = (string)translationSettings["Versification_Type"]; // e.g. "NRT", "S1"

            if (versificationType == "")
            {
                versificationType = versificationTypeDefault;
            }

            // Set variables related to which language, translation, and testament to process.
            if (versificationFilename == "")
            {
                // 2020.07.10 CL: Since we also specify the versification scheme for a specific language, this should be located in the WorkingDirectory folder.
                versificationFile = Path.Combine(resourcesFolder, versificationFilenameDefault); // e.g. "Versification.xml"
            }
            else
            {
                // 2020.10.04 CL: But if we find it too difficult to maintain a single versification file with various schemes, or have unique versification,
                // then we could put it the translation folder
                versificationFile = Path.Combine(translationFolder, versificationFilename); // e.g. "niv84.extracted_versification.xml"
            }

            // Set targetFolder to Language and NT or OT, e.g. "projectsFolder\\<translation>\\<testament>"
            targetFolder = Path.Combine(translationFolder, testament);

            // Set file information in resourcesFolder
            // sourceFolder = Path.Combine(resourcesFolder, sourceFoldername); // e.g. "Manuscript",folder with the original language files.
            treeFolder = Path.Combine(resourcesFolder, treeFoldername); // e.g. "Trees", folder with manuscript trees. Fixed. Doesn't change. Input to CLEAR, Andi's own XML format
            initialFilesFolder = Path.Combine(resourcesFolder, initialFilesFoldername); // e.g. "Initial Files"
            freqPhrasesFile = Path.Combine(resourcesFolder, freqPhrasesFilename); // e.g. "freqPhrases.tsv"
            sourceFuncWordsFile = Path.Combine(resourcesFolder, sourceFuncWordsFilename); // e.g. "sourceFuncwords.txt"
            puncsFile = Path.Combine(resourcesFolder, puncsFilename); // e.g. "puncs.txt"
            glossFile = Path.Combine(resourcesFolder, glossFilename); // e.g. "Gloss.tsv"

            //============================ Output/Input Files Used to Pass Data Between Functions ============================
            // tokFile = Path.Combine(targetFolder, tokenFilename);
            // tokLowerFile = Path.Combine(targetFolder, tokenLemmaFilename);

            // sourceTextFileM = Path.Combine(sourceFolder, sourceTextFilenameM);
            // sourceIdFileM = Path.Combine(sourceFolder, sourceIdFilenameM);
            // sourceLemmaFileM = Path.Combine(sourceFolder, sourceLemmaFilenameM);

            // sourceTextFile = Path.Combine(targetFolder, sourceTextFilename);
            sourceIdFile = Path.Combine(targetFolder, sourceIdFilename);
            sourceLemmaFile = Path.Combine(targetFolder, sourceLemmaFilename);
            // targetTextFile = Path.Combine(targetFolder, targetTextFilename);
            targetIdFile = Path.Combine(targetFolder, targetIdFilename);
            targetLemmaFile = Path.Combine(targetFolder, targetLemmaFilename);

            targetPuncFile = Path.Combine(targetFolder, targetPuncFilename);
            targetPuncLowerFile = Path.Combine(targetFolder, targetPuncLowerFilename);

            sourceLemmaFileCW = Path.Combine(targetFolder, sourceLemmaFilenameCW);
            sourceIdFileCW = Path.Combine(targetFolder, sourceIdFilenameCW);
            targetLemmaFileCW = Path.Combine(targetFolder, targetLemmaFilenameCW);
            targetIdFileCW = Path.Combine(targetFolder, targetIdFilenameCW);

            //============================ Output Files Only ============================
            jsonOutput = Path.Combine(targetFolder, jsonOutputFilename);
            jsonFile = Path.Combine(targetFolder, jsonFilename);
            t2gJsonFile = Path.Combine(targetFolder, t2gJsonFilename);

            //============================ Input Files Only ============================
            versesFile = Path.Combine(targetFolder, versesFilename);
            rawFile = Path.Combine(targetFolder, rawFilename);
            checkedAlignmentsFile = Path.Combine(targetFolder, checkedAlignmentsFilename);
            m_g_jsonFile = Path.Combine(targetFolder, m_g_jsonFilename);
            gAlignment = Path.Combine(targetFolder, gAlignmentFilename);
            auto_m_t_alignment = Path.Combine(targetFolder, auto_m_t_alignmentFilename);

            InitializeStateFiles();
        }
        // 2020.07.10 CL: These files define the state of CLEAR and should all be in the targetFolder, and may change during processing.
        private static void InitializeStateFilenames()
        {
            //============================ CLEAR State Filenames ============================
            // 
            // They are input and output files. You must have them though they can be empty.
            // Has probablities of a manuscript language translated into a target word. Story #1 needs to read in this file after creating it with #6.
            transModelFilename = (string)clearSettings["TransModelFile"]; // e.g. "transModel.tsv"

            // Input and Output file. If you already have manually checked alignment, it will give statistics. Must have but can be empty.
            manTransModelFilename = (string)clearSettings["ManTransModelFile"];  // e.g. "manTransModel.tsv"

            // Must Have. Input and Output file. Token based alignment data, so for a particular verse and word, what are the statistics for translation. Story #1 needs to read in this file after creating it with #6.
            alignModelFilename = (string)clearSettings["AlignModelFile"]; // e.g. "alignModel.tsv"

            // Input file. Must create this. Contains word the aligner should ignore. These are because they tend to not align well across languages, such as English aux verbs.
            stopWordFilename = (string)clearSettings["StopWordFile"]; // e.g. "stopWords.txt"

            // Output File. Contains links that were manually removed.
            badLinkFilename = (string)clearSettings["BadLinkFile"]; // e.g. "badLinks.tsv"

            // Output File. Contains the links that exist.
            goodLinkFilename = (string)clearSettings["GoodLinkFile"]; // e.g. "goodLinks.tsv"

            // If empty, it will do 1-to-1 alignment. Otherwise, it shows where groups of verses are mapped to other groups of verses. Can do many-to-1 and 1-to-many. Can do dynamic alignment.
            groupFilename = (string)clearSettings["GroupFile"]; // e.g. "groups.tsv"

            // Input and Output file. Must have. Translation memory, can be empty. 
            tmFilename = (string)clearSettings["TmFile"]; // e.g. "tm.tsv"

            // 2020.07.10 CL: Made targetFuncWordsFile a global variable to Form1
            targetFuncWordsFilename = (string)clearSettings["TargetFuncWordsFile"]; // e.g. "targetFuncWords.txt"

            // 2020.07.10 CL: Why have this specifically in the targetFolder? Shouldn't Strong's Number information be the same for all languages?
            strongFilename = (string)clearSettings["StrongFile"]; // e.g. "strongs.txt"

            // Input file. Must have it. If have done alignment, can bring in for consideration.
            oldJsonFilename = (string)clearSettings["OldJsonFile"]; // e.g. "oldAlignment.json"
        }

        // 2020.10.19 CL: Need to separate out when we set the filenames and when we set the files (with path).
        // This is because the path may change when the project, testament, or filenames change as a command line option.
        private static void InitializeStateFiles()
        {
            //============================ CLEAR State Files ============================
            transModelFile = Path.Combine(targetFolder, transModelFilename);
            manTransModelFile = Path.Combine(targetFolder, manTransModelFilename);
            alignModelFile = Path.Combine(targetFolder, alignModelFilename);
            groupFile = Path.Combine(targetFolder, groupFilename);
            stopWordFile = Path.Combine(targetFolder, stopWordFilename);
            badLinkFile = Path.Combine(targetFolder, badLinkFilename);
            goodLinkFile = Path.Combine(targetFolder, goodLinkFilename);
            tmFile = Path.Combine(targetFolder, tmFilename);
            targetFuncWordsFile = Path.Combine(targetFolder, targetFuncWordsFilename);
            strongFile = Path.Combine(targetFolder, strongFilename);
            oldJson = Path.Combine(targetFolder, oldJsonFilename);
        }

        public static void Initialize()
        {
            // Get ready to use the Clear3 API.

            clearService = Clear30Service.FindOrCreate();
            importExportService = clearService.ImportExportService;
            utility = clearService.Utility;

            // Initialize the resources that will not change during processing
            InitializeResources();

            // Initialize State of CLEAR
            InitializeState();
        }

        // 2020.07.10 CL: This method initializes the resources that are used by CLEAR but that will not change during processing and so only need to be initialized once.
        private static void InitializeResources()
        {
            // Initialize data structures that are only done once (i.e. not related to processing of verses).
            /*
            puncs = Data.ReadWordList(puncsFile); // input of punc marks
            glossTable = Data.ReadGlossTable(glossFile); // input of glosses
            sourceFuncWords = Data.ReadWordList(sourceFuncWordsFile);
            versificationList = Versification.ReadVersificationList(versificationFile, versificationType, "id"); // Read in the versification
            */
            // freqPhrases not used yet in Clear3
            // freqPhrases = Data.ReadFreqPhrases(freqPhrasesFile); // Used in Story2 and Story3

            simpleVersification =
                importExportService.ImportSimpleVersificationFromLegacy(
                    versificationFile,
                    versificationType);

            // Import auxiliary assumptions from files: punctuation,
            // stop words, function words, manual translation model,
            // good and bad links, old alignment, glossary table,
            // and Strongs data.

            (List<string> puncsTemp,
             List<string> stopWords,
             List<string> sourceFunctionWordsTemp,
             List<string> targetFunctionWords,
             TranslationModel manTransModel,
             Dictionary<string, int> goodLinks,
             Dictionary<string, int> badLinks,
             Dictionary<string, Gloss> glossTableTemp,
             GroupTranslationsTable groups,
             Dictionary<string, Dictionary<string, string>> oldLinks,
             Dictionary<string, Dictionary<string, int>> strongs)
             =
             ImportAuxAssumptionsSubTask.Run(
                 puncsFile,
                 stopWordFile,
                 sourceFuncWordsFile,
                 targetFuncWordsFile,
                 manTransModelFile,
                 goodLinkFile,
                 badLinkFile,
                 glossFile,
                 groupFile,
                 oldJson,
                 strongFile);

            puncs = puncsTemp;
            glossTable = glossTableTemp;
            sourceFunctionWords = sourceFunctionWordsTemp;

            // Get the standard tree service.

            treeService = GetStandardTreeServiceSubtask.Run(
                resourcesFolder);

        }

        // 2020.07.10 CL: It seems that Andi wanted to have the possibility that you can start CLEAR again and it would continue from where it left off.
        // However, since I added a new button that will start fresh with a new analysis, I want to be able to initialize the state with some files initially empty.
        // So need a method to call.
        // 2020.07.10 CL: There seem to be some of these that do not change because of processing through CLEAR. They may change based upon analysis by another program.
        private static void InitializeState()
        {
            // preAlignment Not used yet in Clear3
            // preAlignment = Data.BuildPreAlignmentTable(alignModel); // Has key as sourceID and value as targetID

            // tm Not used yet in Clear3
            // tm = AutoAligner.ReadTM(tmFile); // tm is a Hashtable with the Key the source Strongs number, and the Value is a list of translations. 

            translationModel = importExportService.ImportTranslationModel(transModelFile);
            alignmentModel = importExportService.ImportAlignmentModel(alignModelFile);

            (List<string> puncs,
             List<string> stopWordsTemp,
             List<string> sourceFunctionWords,
             List<string> targetFunctionWordsTemp,
             TranslationModel manTransModelTemp,
             Dictionary<string, int> goodLinksTemp,
             Dictionary<string, int> badLinksTemp,
             Dictionary<string, Gloss> glossTable,
             GroupTranslationsTable groupsTemp,
             Dictionary<string, Dictionary<string, string>> oldLinksTemp,
             Dictionary<string, Dictionary<string, int>> strongsTemp)
             =
             ImportAuxAssumptionsSubTask.Run(
                 puncsFile,
                 stopWordFile,
                 sourceFuncWordsFile,
                 targetFuncWordsFile,
                 manTransModelFile,
                 goodLinkFile,
                 badLinkFile,
                 glossFile,
                 groupFile,
                 oldJson,
                 strongFile);

            badLinks = badLinksTemp;
            goodLinks = goodLinksTemp;
            targetFunctionWords = targetFunctionWordsTemp;
            strongs = strongsTemp;
            oldLinks = oldLinksTemp;
            stopWords = stopWordsTemp;
            groups = groupsTemp;
            manTransModel = manTransModelTemp;

        }

        public static string Do_Button1()
        {
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

            // bool useAlignModel = true;
            // int maxPaths = 1000000;
            // int goodLinkMinCount = 3;
            // int badLinkMinCount = 3;
            // bool contentWordsOnly = true;

            IAutoAlignAssumptions assumptions =
                clearService.AutoAlignmentService.MakeStandardAssumptions(
                    translationModel,
                    manTransModel,
                    alignmentModel,
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
                    maxPaths: 100000);

            // Apply a tree-based auto-alignment to each of the zone
            // alignment problems, producing an alignment datum in the
            // persistent format.

            Console.WriteLine("Auto Aligning");

            ShowTime();

            LegacyPersistentAlignment alignment =
                AutoAlignFromModelsNoGroupsSubTask.Run(
                    zoneAlignmentProblems,
                    treeService,
                    glossTable,
                    assumptions);

            // Export the persistent-format datum to a file.

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Formatting.Indented);
            File.WriteAllText(jsonOutput, json);

            ShowTime();

            return ("Verse aligned.  Output in " + jsonOutput);
        }

        public static string Do_Button2()
        {
            Console.WriteLine("Updating Incrementally. Not implemented yet in Clear3.");
            return "Story2 not implemented.";

        }

        public static string Do_Button3()
        {
            Console.WriteLine("Updating Globally. Not implemented yet in Clear3.");
            return "Story3 not implemented.";
        }

        public static string Do_Button4()
        {
            Console.WriteLine("Aligning Gateway-to-Translation via Manuscript-to-Gateway. Not implemented yet in Clear3.");
            return "Story4 not implemented.";
        }

        public static string Do_Button5()
        {
            Console.WriteLine("Aligning Manuscript-to-Translation via Manuscript-to-Gateway. Not implemented yet in Clear3.");
            return "Story5 not implemented.";
        }

        public static string Do_Button6()
        {
            Console.WriteLine("Building Models");

            if (runSpec.StartsWith("Machine;"))
            {
                var machineRunSpec = runSpec.Substring(runSpec.IndexOf(';') + 1);
                string[] parts = machineRunSpec.Split(':');
                if (parts[0] == "") runSpec += thotModel;
                if (parts.Length < 2) runSpec += ":" + thotHeuristic;
                if (parts.Length < 3) runSpec += ":" + thotIterations;
            }

            ShowTime();

            // Train a statistical translation model using the parallel
            // corpora with content words only, producing an estimated
            // translation model and estimated alignment.

            if (contentWordsOnly)
            {
                (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(parallelCorporaCW, runSpec, epsilon);
            }
            else
            {
                (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(parallelCorpora, runSpec, epsilon);
            }

            ShowTime();

            // Within the SMTService.DefaultSMT, it writes and reads from the file, so any double differences is already done.
            // No need to read them in again.

            // transModel = importExportService.ImportTranslationModel(transModelFile);
            // alignmentModel = importExportService.ImportAlignmentModel(alignModelFile);

            Persistence.ExportTranslationModel(translationModel, transModelFile);
            Persistence.ExportAlignmentModel(alignmentModel, alignModelFile);

            ShowTime();

            // 2020.06.29 CL: We updated alignModel so also need to update preAlignment.
            // preAlignment = Data.BuildPreAlignmentTable(alignModel);

            return ("Models built: " + transModelFile + "; " + alignModelFile);
        }

        public static string Do_Button7()
        {
            Console.WriteLine("Tokenizing Verses");

            ShowTime();

            targetVerseCorpus =
                importExportService.ImportTargetVerseCorpusFromLegacy(
                    versesFile,
                    clearService.DefaultSegmenter,
                    puncs,
                    lang);

            ShowTime();

            // return (versesFile + " has been tokenized to " + targetPuncFile + " and " + targetPuncLowerFile + ".");
            return (versesFile + " has been tokenized and targetVerseCorpus has been created.");
        }


        public static string Do_Button8()
        {
            Console.WriteLine("Creating Parallel Corpora");
            /*
            GroupVerses.CreateParallelFiles(sourceFileM, sourceIdFileM, sourceIdLemmaFileM, targetPuncFile, sourceFile, sourceIdFile, sourceIdLemmaFile, targetFile, targetIdFile, versificationList);

            // Use the versification with the target verses to line up
            // translated zones with sourced zones.

            Console.WriteLine("Creating Parallel Corpora");
            */
            ShowTime();

            parallelCorpora = utility.CreateParallelCorpora(
                targetVerseCorpus,
                treeService,
                simpleVersification);

            Persistence.ExportParallelCorpora(parallelCorpora, sourceLemmaFile, sourceIdFile, targetLemmaFile, targetIdFile);

            // Remove functions words from the parallel corpora, leaving
            // only the content words for the SMT step to follow.

            ShowTime();

            parallelCorporaCW =
               utility.FilterFunctionWordsFromParallelCorpora(
                   parallelCorpora,
                   sourceFunctionWords,
                   targetFunctionWords);

            Persistence.ExportParallelCorpora(parallelCorporaCW, sourceLemmaFileCW, sourceIdFileCW, targetLemmaFileCW, targetIdFileCW);

            ShowTime();

            return ("Parallel files have been created.");
        }

        public static string Do_Button9()
        {
            Console.WriteLine("Doing ProcessAll command");

            Do_Button10(); // Copy Initial files
            Do_Button12(); // Initialize State and Process from Tokenize through Auto Align

            // Copy alignment.json to CheckedAlignment.json to pretend we did manual alignment
            // string checkedAlignmentsFile = Path.Combine(targetFolder, textBox4); // "CheckedAlignments.json"
            File.Delete(checkedAlignmentsFile);
            File.Copy(jsonOutput, checkedAlignmentsFile);

            Do_Button3(); // Global update

            return "Done Processing.";
        }

        public static string Do_Button10()
        {
            Console.WriteLine("Copying Initial Files");

            string copiedFiles = "Copied Initial State Files:\n";

            copiedFiles += CopyInitialStateFile(transModelFile);
            copiedFiles += CopyInitialStateFile(alignModelFile);
            copiedFiles += CopyInitialStateFile(stopWordFile);
            copiedFiles += CopyInitialStateFile(badLinkFile);
            copiedFiles += CopyInitialStateFile(goodLinkFile);
            copiedFiles += CopyInitialStateFile(groupFile);
            copiedFiles += CopyInitialStateFile(strongFile);
            copiedFiles += CopyInitialStateFile(oldJson);
            copiedFiles += CopyInitialStateFile(t2gJsonFile);
            copiedFiles += CopyInitialStateFile(checkedAlignmentsFile);
            copiedFiles += CopyInitialStateFile(manTransModelFile);
            copiedFiles += CopyInitialStateFile(tmFile);

            // return copiedFiles;
            return ("Copied Initial Files.");
        }


        private static string CopyInitialStateFile(string targetFile)
        {
            string filename = Path.GetFileName(targetFile);
            string sourceFile = Path.Combine(initialFilesFolder, filename);

            File.Delete(targetFile);
            File.Copy(sourceFile, targetFile);

            return sourceFile + "\n";
        }

        public static string Do_Button11()
        {
            Console.WriteLine("Iniializing Clear's State");

            InitializeState();

            return "Initialized State.";
        }

        public static string Do_Button12()
        {
            Console.WriteLine("Doing DoStart command");

            Do_Button11(); // Initialize State
            Do_Button7(); // Tokenize
            Do_Button8(); // Parallelize
            Do_Button6(); // Create statistical models
            Do_Button1(); // Auto align

            return "Done Processing.";
        }

        public static string Do_Button13()
        {
            Console.WriteLine("Doing FreshStart command");

            Do_Button10(); // Copy Initial files
            Do_Button11(); // Initialize State
            Do_Button7(); // Tokenize
            Do_Button8(); // Parallelize
            Do_Button6(); // Create statistical models
            Do_Button1(); // Auto align

            return ("Done Processing.");
        }

        private static void ShowTime()
        {
            DateTime dt = DateTime.Now;
            Console.WriteLine(dt.ToString("G"));
        }

        private static IClear30ServiceAPI clearService;
        private static IImportExportService importExportService;
        private static IUtility utility;

        private static string clearConfigFilename;
        private static Hashtable clearSettings; // Input from .config file
        private static string processingConfigFilename;
        private static Hashtable processingSettings; // Input from .config file
        private static string runConfigFile;
        private static Hashtable runSettings; // Input from .config file
        private static string translationConfigFilename;
        private static string translationFolder;
        private static string translationConfigFile;

        private static Hashtable translationSettings;

        private static string resourcesFolder; // 2020.07.11 CL: The folder where all the CLEAR Engine resources are kept.
        private static string processingFolder; // 2020.07.11 CL: The folder where all the CLEAR Engine translation projects are kept.
        private static string sourceFoldername; // the folder where all the manuscript files are kept.  They are static and do not change over time.
        // private static string sourceFolder; // the folder where all the manuscript files are kept.  They are static and do not change over time.
        private static string targetFolder; // the folder where the target language data is kept.  Each different translaton has a different folder.
        private static string treeFoldername; // the folder where syntatic trees are kept.
        private static string treeFolder; // the folder where syntatic trees are kept.
        private static string initialFilesFoldername; // 2020.07.16 CL: Added this to define where the initial files are located.
        private static string initialFilesFolder; // 2020.07.16 CL: Added this to define where the initial files are located.

        private static string project; // 2020.09.25 CL: The name of the folder inside the projectsFolder. We may have multiple translations for the same language. This is used to identify the folder used.
        private static string lang; // language of target language
        private static string translation; // 2020.09.25 CL: This is now the name of the translation, sometimes including a numbers to indicate the year of the copyright, e.g. NIV84
        private static string testament; // 2020.07.11 CL: Which testament we are working on. I think at some point, we should not need to distinguish between them.

        private static string runSpec; // 1:10;H:5
        private static double epsilon; // Must exceed this to be counted into model, 0.1
        private static string thotModel; // For SIL auto aligner interations
        private static string thotHeuristic; // For SIL auto aligner interations
        private static string thotIterations; // For SIL auto aligner interations
        private static bool smtContentWordsOnly; // Use only content words for creating the statistical models: transModel and alignModel
        private static bool useAlignModel; // whether to use the alignment model
        private static bool contentWordsOnly; // whether to align content words only

        private static string transModelFilename; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts
        private static string transModelFile; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts
        private static TranslationModel translationModel; // translation model

        private static string manTransModelFilename; // the file that contains the translation model created from manually checked alignments
        private static string manTransModelFile; // the file that contains the translation model created from manually checked alignments
        private static TranslationModel manTransModel; // the translation model created from manually checked alignments

        private static string alignModelFilename; // the file that contains the alignment model;
        private static string alignModelFile; // the file that contains the alignment model;
        // private static AlignmentModel alignModel; // alignment model
        private static AlignmentModel alignmentModel; // alignment model
        // private static AlignmentModel preAlignment; // alignment table

        private static string glossFilename; // 2020.07.11 CL: The file where the source glosses are kept.
        private static string glossFile; // 2020.07.11 CL: The file where the source glosses are kept.
        private static Dictionary<string, Gloss> glossTable; // Word glosses

        private static string puncsFilename; // 2020.07.11 CL: The file where the source punctuations kept.
        private static string puncsFile; // 2020.07.11 CL: The file where the source punctuations kept.
        private static List<string> puncs; // list of punctuation marks

        private static string versificationFilenameDefault; // default versification
        private static string versificationTypeDefault; // default versification type
        private static string versificationFilename; // versification file
        private static string versificationFile; // versification file
        private static string versificationType; // versification type
        // private static ArrayList versificationList; // list of source-target verse pairs
        private static SimpleVersification simpleVersification;

        private static string jsonOutputFilename; // output of aligner in JSON
        private static string jsonOutput; // output of aligner in JSON
        private static string jsonFilename; // output of aligner in JSON
        private static string jsonFile; // output of aligner in JSON

        private static string t2gJsonFilename; // alignment between gateway translation and target translation
        private static string t2gJsonFile; // alignment between gateway translation and target translation
        private static string gAlignment;
        private static string gAlignmentFilename;
        private static string auto_m_t_alignmentFilename;
        private static string auto_m_t_alignment;

        private static string groupFilename; // the file that contains the one-to-many, many-to-one, and many-to-many mappings
        private static string groupFile; // the file that contains the one-to-many, many-to-one, and many-to-many mappings
        private static GroupTranslationsTable groups; // one-to-many, many-to-one, and many-to-many mappings

        private static string stopWordFilename; // contains the list of target words not to be linked
        private static string stopWordFile; // contains the list of target words not to be linked
        private static List<string> stopWords; // list of target words not to be linked

        private static int badLinkMinCount; // the minimal count required for treating a link as bad
        private static string badLinkFilename; // contains the list of pairs of words that should not be linked
        private static string badLinkFile; // contains the list of pairs of words that should not be linked
        private static Dictionary<string, int> badLinks; // list of pairs of words that should not be linked

        private static int goodLinkMinCount; // the minimal count required for treating a link as good
        private static string goodLinkFilename; // contains the list of pairs of words that should be linked
        private static string goodLinkFile; // contains the list of pairs of words that should be linked
        private static Dictionary<string, int> goodLinks; // list of pairs of words that should be linked

        private static string tmFilename; // translation memory file
        private static string tmFile; // translation memory file
        // private static Hashtable tm; // translation memory

        private static string freqPhrasesFilename; // the file that contains frequent phrases
        private static string freqPhrasesFile; // the file that contains frequent phrases
        // private static Hashtable freqPhrases; // table of frequent phrases

        private static string sourceFuncWordsFilename;
        private static string sourceFuncWordsFile;
        // private static List<string> sourceFuncWords; // function words
        private static List<string> sourceFunctionWords; // function words

        private static string targetFuncWordsFilename; // 2020.07.10 CL: Added this to make it global to this form since targetFuncWords is global.
        private static string targetFuncWordsFile; // 2020.07.10 CL: Added this to make it global to this form since targetFuncWords is global.
        // private static List<string> targetFuncWords;
        private static List<string> targetFunctionWords;

        private static string strongFilename;
        private static string strongFile;
        private static Dictionary<string, Dictionary<string, int>> strongs;

        private static string oldJsonFilename;
        private static string oldJson;
        private static Dictionary<string, Dictionary<string, string>> oldLinks;

        private static TargetVerseCorpus targetVerseCorpus;
        private static ITreeService treeService;
        private static ParallelCorpora parallelCorpora;
        private static ParallelCorpora parallelCorporaCW;

        // private static string tokenFilename;
        // private static string tokFile;
        // private static string tokenLemmaFilename;
        // private static string tokLowerFile;
        // private static string sourceTextFilenameM;
        // private static string sourceTextFileM;
        // private static string sourceIdFilenameM;
        // private static string sourceIdFileM;
        // private static string sourceLemmaFilenameM;
        // private static string sourceLemmaFileM;

        // private static string sourceTextFilename;
        // private static string sourceTextFile;
        private static string sourceIdFilename;
        private static string sourceIdFile;
        private static string sourceLemmaFilename;
        private static string sourceLemmaFile;
        // private static string targetTextFilename;
        // private static string targetTextFile;
        private static string targetIdFilename;
        private static string targetIdFile;
        private static string targetLemmaFilename;
        private static string targetLemmaFile;

        private static string targetPuncFilename;
        private static string targetPuncFile;
        private static string targetPuncLowerFilename;
        private static string targetPuncLowerFile;

        private static string sourceLemmaFilenameCW;
        private static string sourceLemmaFileCW;
        private static string sourceIdFilenameCW;
        private static string sourceIdFileCW;
        private static string targetLemmaFilenameCW;
        private static string targetLemmaFileCW;
        private static string targetIdFilenameCW;
        private static string targetIdFileCW;

        private static string versesFilename; // Input file with verses on separate lines previxed with verseID.
        private static string versesFile; // Input file with verses on separate lines previxed with verseID.
        private static string rawFilename; // Input file with verses on separate lines previxed with verseID.
        private static string rawFile; // Input file with verses on separate lines previxed with verseID.
        private static string checkedAlignmentsFilename; // input to global update
        private static string checkedAlignmentsFile; // input to global update
        private static string m_g_jsonFilename;
        private static string m_g_jsonFile;

    }
}