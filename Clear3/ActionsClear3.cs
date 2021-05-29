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


namespace Clear3
{
    public class ActionsClear3
    {
        public static void SetProject(string arg)
        {
            project = arg;
        }

        public static void SetTestament(string arg)
        {
            testament = arg;
        }

        public static void SetContentWordsOnly(string arg)
        {
            strContentWordsOnly = arg;
        }

        public static void SetUseAlignModel(string arg)
        {
            strUseAlignModel = arg;
        }

        public static void SetRunSpec(string arg)
        {
            runSpec = arg;
        }

        public static void SetEpsilon(string arg)
        {
            strEpsilon = arg;
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
            strContentWordsOnlySMT = arg;
        }

        public static void SetTcContentWordsOnly(string arg)
        {
            strContentWordsOnlyTC = arg;
        }

        public static void SetUseLemmaCatModel(string arg)
        {
            strUseLemmaCatModel = arg;
        }

        public static void SetUseNoPuncModel(string arg)
        {
            strUseNoPuncModel = arg;
        }

        public static void InitializeConfig()
        {
            Console.WriteLine();
            Console.WriteLine("Running ClearEngine 3");

            clearConfigFilename = "CLEAR.config"; // default configuration file
            ReadConfig(clearConfigFilename);

            InitializeClear30Service();
        }

        // CL: 2020.08.21 Modified to use a .config (XML) file to store all of the filenames and booleans rather than embedding them in the code.
        // They can be initialized from the configuration file, and different ones can be used (by manual copying of files) or through a command line option.
        // Once loaded in at startup, they can be changed by command line options.
        // This was added to avoid having to change code because you want to use different files.
        // This was done by using Forms and textBoxes, but only for some of the configuration. Others were embedded in the code.
        private static void ReadConfig(string configFilename)
        {
            clearSettings = Configuration.GetSettings(configFilename);

            resourcesFolder = clearSettings["ResourcesFolder"]; // e.g. 
            processingFolder = clearSettings["ProcessingFolder"]; // e.g. 

            runConfigFile = clearSettings["RunConfigFile"];
            runSettings = Configuration.GetSettings(runConfigFile);

            processingConfigFilename = clearSettings["ProcessingConfigFile"];

            // Run and some processing settings can be overridden by command line parameters
            project = (string)runSettings["Project"]; // e.g. "NIV84-SIL-test"
            testament = (string)runSettings["Testament"]; // e.g. "OT" or "NT"

            // Set Translation Parameters
            translationConfigFilename = clearSettings["TranslationConfigFile"];

            // Set file information in resourcesFolder
            sourceFoldername = clearSettings["SourceFolder"];
            treeFoldername = clearSettings["TreeFolder"];
            initialFilesFoldername = clearSettings["InitialFilesFolder"];
            freqPhrasesFilename = clearSettings["FreqPhrasesFile"];
            sourceFuncWordsFilename = clearSettings["SourceFuncWordsFile"];
            puncsFilename = clearSettings["PuncsFile"];
            glossFilename = clearSettings["GlossFile"];
            versificationFilenameDefault = clearSettings["Versification_File"];
            versificationTypeDefault = clearSettings["Versification_Type"];

            //============================ Output/Input Files Used to Pass Data Between Functions ============================
            //
            tokenTextFilename = clearSettings["TokenTextFile"];
            tokenLemmaFilename = clearSettings["TokenLemmaFile"];
            tokenIdFilename = clearSettings["TokenIdFile"];

            // sourceTextFilenameM = clearSettings["SourceTextFileM"]; // e.g. 
            // sourceIdFilenameM = clearSettings["SourceIdFileM"]; // e.g. 
            // sourceLemmaFilenameM = clearSettings["SourceLemmaFileM"]; // e.g.
            // sourceLemmaCatFilenameM = clearSettings["SourceLemmaCatFileM"];

            sourceTextFilename = clearSettings["SourceTextFile"];
            sourceLemmaFilename = clearSettings["SourceLemmaFile"];
            sourceIdFilename = clearSettings["SourceIdFile"];
            sourceLemmaCatFilename = clearSettings["SourceLemmaCatFile"];

            sourceTextNoPuncFilename = clearSettings["SourceTextNoPuncFile"];
            sourceLemmaNoPuncFilename = clearSettings["SourceLemmaNoPuncFile"];
            sourceIdNoPuncFilename = clearSettings["SourceIdNoPuncFile"];
            sourceLemmaCatNoPuncFilename = clearSettings["SourceLemmaCatNoPuncFile"];

            targetTextFilename = clearSettings["TargetTextFile"];
            targetLemmaFilename = clearSettings["TargetLemmaFile"];
            targetIdFilename = clearSettings["TargetIdFile"];

            targetTextNoPuncFilename = clearSettings["TargetTextNoPuncFile"];
            targetLemmaNoPuncFilename = clearSettings["TargetLemmaNoPuncFile"];
            targetIdNoPuncFilename = clearSettings["TargetIdNoPuncFile"];

            // Not Currently Used
            // targetPuncFilename = clearSettings["TargetPuncFile"];
            // targetPuncLowerFilename = clearSettings["TargetPuncLowerFile"];

            // For Content Words Only Versions

            sourceTextFilenameCW = clearSettings["SourceTextFileCW"];
            sourceLemmaFilenameCW = clearSettings["SourceLemmaFileCW"];
            sourceIdFilenameCW = clearSettings["SourceIdFileCW"];
            sourceLemmaCatFilenameCW = clearSettings["SourceLemmaCatFileCW"];

            sourceTextNoPuncFilenameCW = clearSettings["SourceTextNoPuncFileCW"];
            sourceLemmaNoPuncFilenameCW = clearSettings["SourceLemmaNoPuncFileCW"];
            sourceIdNoPuncFilenameCW = clearSettings["SourceIdNoPuncFileCW"];
            sourceLemmaCatNoPuncFilenameCW = clearSettings["SourceLemmaCatNoPuncFileCW"];

            targetTextFilenameCW = clearSettings["TargetTextFileCW"];
            targetLemmaFilenameCW = clearSettings["TargetLemmaFileCW"];
            targetIdFilenameCW = clearSettings["TargetIdFileCW"];

            targetTextNoPuncFilenameCW = clearSettings["TargetTextNoPuncFileCW"];
            targetLemmaNoPuncFilenameCW = clearSettings["TargetLemmaNoPuncFileCW"];
            targetIdNoPuncFilenameCW = clearSettings["TargetIdNoPuncFileCW"];

            //============================ Output Files Only ============================
            // Files not part of the state, nor used as output/input to pass data between different functions
            // Output file. Has the alignment in .json format, which is more readable than XML format.
            jsonOutputFilename = clearSettings["JsonOutputFile"]; // e.g "alignment.json", Should update variable to ...File
            jsonFilename = clearSettings["JsonFile"]; // e.g "alignment.json", Should merge with jsonOutput

            // Output file. Has the alignment in .json format, which is more readable than XML format. Gateway language alignment. Manuscript to gateway, or gateway to target?
            t2gJsonFilename = clearSettings["T2GJsonFile"]; // e.g. "gAlignment.json"

            //============================ Input Files Only ============================
            versesFilename = clearSettings["VersesFile"]; // e.g. "Verses.txt"
            rawFilename = clearSettings["RawFile"]; // e.g. "Verses.txt"
            checkedAlignmentsFilename = clearSettings["CheckedAlignmentsFile"]; // e.g. "CheckedAlignments.json"
            m_g_jsonFilename = clearSettings["M_G_JsonFile"]; // e.g "m_g_alignment.json", the CLEAR json where manuscript, gTranslation and gLinks are instantiated but translation and links are still empty
            gAlignmentFilename = clearSettings["GAlignmentFile"]; // e.g. "gAlignment.json"
            auto_m_t_alignmentFilename = clearSettings["Auto_M_T_AlignmentFile"]; // e.g. "auto_m_t_alignment.json"

            // Initialize state related filenames
            InitializeStateFilenames();

            // Initialize resource related file paths
            InitializeResourceFiles();
        }

        private static void InitializeResourceFiles()
        {
            // Set file information in resourcesFolder
            // sourceFolder = Path.Combine(resourcesFolder, sourceFoldername); // e.g. "Manuscript",folder with the original language files.
            treeFolder = Path.Combine(resourcesFolder, treeFoldername); // e.g. "Trees", folder with manuscript trees. Fixed. Doesn't change. Input to CLEAR, Andi's own XML format
            initialFilesFolder = Path.Combine(resourcesFolder, initialFilesFoldername); // e.g. "Initial Files"
            freqPhrasesFile = Path.Combine(resourcesFolder, freqPhrasesFilename); // e.g. "freqPhrases.tsv"
            sourceFuncWordsFile = Path.Combine(resourcesFolder, sourceFuncWordsFilename); // e.g. "sourceFuncwords.txt"
            puncsFile = Path.Combine(resourcesFolder, puncsFilename); // e.g. "puncs.txt"
            glossFile = Path.Combine(resourcesFolder, glossFilename); // e.g. "Gloss.tsv"

            // sourceTextFileM = Path.Combine(sourceFolder, sourceTextFilenameM);
            // sourceLemmaFileM = Path.Combine(sourceFolder, sourceLemmaFilenameM);
            // sourceIdFileM = Path.Combine(sourceFolder, sourceIdFilenameM);
            // sourceLemmaCatFileM = Path.Combine(sourceFolder, sourceLemmaCatFilenameM);

            InitializeResources();
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

        private static void InitializeClear30Service()
        {
            // Get ready to use the Clear3 API.

            clearService = Clear30Service.FindOrCreate();
            importExportService = clearService.ImportExportService;
            utility = clearService.Utility;
        }

        // 2020.10.19 CL: Need to separate out when we set the filenames and when we set the files (with path).
        // This is because the path may change when the project, testament, or filenames change as a command line option.
        // Called by command line processor before doing actions (if it hasn't already been initialized).
        public static void InitializeTargetFiles()
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

            lang = translationSettings["Language"]; // e.g. "English"
            culture = translationSettings["CSharpCulture"]; // e.g. en-US
            translation = translationSettings["Translation"]; // e.g. NIV84

            // 2021.03.29 CL: Decided to prefix some of the state filenames with "<translation>.<testament>." since this makes it easier to identify
            // which translation and testament they are for.
            // This will also allow the option to put all of the files into one folder rather than an OT and NT subfolders and choose
            // which ones are shared by OT and NT and which ones are not.

            translationTestamentPrefix = string.Format("{0}.{1}.", translation.ToLower(), testament.ToLower());

            versificationFilename = translationSettings["Versification_File"];
            versificationType = translationSettings["Versification_Type"]; // e.g. "NRT", "S1"

            if (versificationType == "")
            {
                versificationType = versificationTypeDefault;
            }

            if (versificationFilename == "")
            {
                versificationFile = Path.Combine(resourcesFolder, versificationFilenameDefault); // e.g. "Versification.xml"
            }
            else
            {
                versificationFile = Path.Combine(translationFolder, versificationFilename); // e.g. "niv84.extracted_versification.xml"
            }
            // versificationList = Versification.ReadVersificationList(versificationFile, versificationType, "id"); // Read in the versification

            simpleVersification = importExportService.ImportSimpleVersificationFromLegacy(
                                    versificationFile,
                                    versificationType);

            // Set targetFolder to Language and NT or OT, e.g. "processingFolder\\<translation>\\<testament>"
            targetFolder = Path.Combine(translationFolder, testament);

            //============================ Output/Input Files Used to Pass Data Between Functions ============================
            tokenTextFile = Path.Combine(targetFolder, translationTestamentPrefix + tokenTextFilename);
            tokenLemmaFile = Path.Combine(targetFolder, translationTestamentPrefix + tokenLemmaFilename);
            tokenIdFile = Path.Combine(targetFolder, translationTestamentPrefix + tokenIdFilename);

            // Parallel Corpus Files

            string corpusFolder = Path.Combine(targetFolder, "Corpus");
            if (!Directory.Exists(corpusFolder))
            {
                Directory.CreateDirectory(corpusFolder);
            }

            sourceTextFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceTextFilename);
            sourceLemmaFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaFilename);
            sourceIdFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceIdFilename);
            sourceLemmaCatFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaCatFilename);

            sourceTextNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceTextNoPuncFilename);
            sourceLemmaNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaNoPuncFilename);
            sourceIdNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceIdNoPuncFilename);
            sourceLemmaCatNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaCatNoPuncFilename);

            targetTextFile = Path.Combine(corpusFolder, translationTestamentPrefix + targetTextFilename);
            targetLemmaFile = Path.Combine(corpusFolder, translationTestamentPrefix + targetLemmaFilename);
            targetIdFile = Path.Combine(corpusFolder, translationTestamentPrefix + targetIdFilename);

            targetTextNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + targetTextNoPuncFilename);
            targetLemmaNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + targetLemmaNoPuncFilename);
            targetIdNoPuncFile = Path.Combine(corpusFolder, translationTestamentPrefix + targetIdNoPuncFilename);

            // Not currently used
            // targetPuncFile = Path.Combine(targetFolder, translationTestamentPrefix + targetPuncFilename);
            // targetPuncLowerFile = Path.Combine(targetFolder, translationTestamentPrefix + targetPuncLowerFilename);

            sourceTextFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceTextFilenameCW);
            sourceLemmaFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaFilenameCW);
            sourceIdFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceIdFilenameCW);
            sourceLemmaCatFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaCatFilenameCW);

            sourceTextNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceTextNoPuncFilenameCW);
            sourceLemmaNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaNoPuncFilenameCW);
            sourceIdNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceIdNoPuncFilenameCW);
            sourceLemmaCatNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + sourceLemmaCatNoPuncFilenameCW);

            targetTextFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + targetTextFilenameCW);
            targetLemmaFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + targetLemmaFilenameCW);
            targetIdFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + targetIdFilenameCW);

            targetTextNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + targetTextNoPuncFilenameCW);
            targetLemmaNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + targetLemmaNoPuncFilenameCW);
            targetIdNoPuncFileCW = Path.Combine(corpusFolder, translationTestamentPrefix + targetIdNoPuncFilenameCW);

            //============================ Output Files Only ============================

            jsonOutput = Path.Combine(targetFolder, translationTestamentPrefix + jsonOutputFilename);
            jsonFile = Path.Combine(targetFolder, translationTestamentPrefix + jsonFilename);
            t2gJsonFile = Path.Combine(targetFolder, translationTestamentPrefix + t2gJsonFilename);

            //============================ Input Files Only ============================
            versesFile = Path.Combine(targetFolder, translationTestamentPrefix + versesFilename);
            rawFile = Path.Combine(targetFolder, translationTestamentPrefix + rawFilename);
            checkedAlignmentsFile = Path.Combine(targetFolder, translationTestamentPrefix + checkedAlignmentsFilename);
            m_g_jsonFile = Path.Combine(targetFolder, translationTestamentPrefix + m_g_jsonFilename);
            gAlignment = Path.Combine(targetFolder, translationTestamentPrefix + gAlignmentFilename);
            auto_m_t_alignment = Path.Combine(targetFolder, translationTestamentPrefix + auto_m_t_alignmentFilename);

            InitializeStateFiles();
        }

        public static void InitializeProcessingSettings()
        {
            // If there is a specific processing configuration file, then use it, otherwise use the generic/default one.
            processingConfigFile = Path.Combine(translationFolder, processingConfigFilename);

            if (!File.Exists(processingConfigFile))
            {
                processingConfigFile = processingConfigFilename;
            }
            processingSettings = Configuration.GetSettings(processingConfigFile);

            // Set Processing Parameters
            if (runSpec == null) runSpec = processingSettings["RunSpec"]; // e.g. 1:10;H:5, Machine;FastAlign, Machine;FastAlign:Intersection:7
            if (strEpsilon == null) strEpsilon = processingSettings["Epsilon"]; // Must exceed this to be counted into model, e.g. "0.1"
            if (thotModel == null) thotModel = processingSettings["ThotModel"];
            if (thotHeuristic == null) thotHeuristic = processingSettings["ThotHeuristic"];
            if (thotIterations == null) thotIterations = processingSettings["ThotIterations"];
            if (strContentWordsOnlySMT == null) strContentWordsOnlySMT = processingSettings["ContentWordsOnlySMT"]; // e.g. "true" Only use content words for building models
            if (strContentWordsOnlyTC == null) strContentWordsOnlyTC = processingSettings["ContentWordsOnlyTC"]; // e.g. "true" Only use content words for building models
            if (strContentWordsOnly == null) strContentWordsOnly = processingSettings["ContentWordsOnly"]; // e.g. "true" Only align content words

            if (strUseAlignModel == null) strUseAlignModel = processingSettings["UseAlignModel"]; // e.g. "true"
            if (strUseLemmaCatModel == null) strUseLemmaCatModel = processingSettings["UseLemmaCatModel"]; // e.g. "true"
            if (strUseNoPuncModel == null) strUseNoPuncModel = processingSettings["UseNoPuncModel"]; // e.g. "true"

            if (strBadLinkMinCount == null) strBadLinkMinCount = processingSettings["BadLinkMinCount"]; // e.g. "3", the minimal count required for treating a link as bad
            if (strGoodLinkMinCount == null) strGoodLinkMinCount = processingSettings["GoodLinkMinCount"]; // e.g. "3" the minimal count required for treating a link as bad

            // Convert strings parameters to values

            epsilon = Double.Parse(strEpsilon); // Must exceed this to be counted into model, e.g. "0.1"
            contentWordsOnlySMT = (strContentWordsOnlySMT == "true"); // e.g. "true" Only use content words for building models
            contentWordsOnlyTC = (strContentWordsOnlyTC == "true"); // e.g. "true" Only use content words for building models
            contentWordsOnly = (strContentWordsOnly == "true"); // e.g. "true" Only align content words

            useAlignModel = (strUseAlignModel == "true"); // e.g. "true"
            useLemmaCatModel = (strUseLemmaCatModel == "true"); // e.g. "true"
            useNoPuncModel = (strUseNoPuncModel == "true"); // e.g. "true"

            badLinkMinCount = Int32.Parse(strBadLinkMinCount); // e.g. "3", the minimal count required for treating a link as bad
            goodLinkMinCount = Int32.Parse(strGoodLinkMinCount); // e.g. "3" the minimal count required for treating a link as bad

            string alignmentType = "_all.json";
            if (contentWordsOnly)
            {
                alignmentType = "_content.json";
            }
            jsonOutput = jsonOutput.Replace(".json", alignmentType);
        }

        // 2020.07.10 CL: These files define the state of CLEAR and should all be in the targetFolder, and may change during processing.
        private static void InitializeStateFilenames()
        {
            //============================ CLEAR State Filenames ============================
            // 
            // They are input and output files. You must have them though they can be empty.
            // Has probablities of a manuscript language translated into a target word. Story #1 needs to read in this file after creating it with #6.
            transModelFilename = clearSettings["TransModelFile"]; // e.g. "transModel.tsv"
            transModelFilenameCW = clearSettings["TransModelFileCW"]; // e.g. "transModel.cw.tsv"

            // Input and Output file. If you already have manually checked alignment, it will give statistics. Must have but can be empty.
            manTransModelFilename = clearSettings["ManTransModelFile"];  // e.g. "manTransModel.tsv"

            // Must Have. Input and Output file. Token based alignment data, so for a particular verse and word, what are the statistics for translation. Story #1 needs to read in this file after creating it with #6.
            alignModelFilename = clearSettings["AlignModelFile"]; // e.g. "alignModel.tsv"
            alignModelFilenameCW = clearSettings["AlignModelFileCW"]; // e.g. "alignModel.cw.tsv"

            // Input file. Must create this. Contains word the aligner should ignore. These are because they tend to not align well across languages, such as English aux verbs.
            stopWordFilename = clearSettings["StopWordFile"]; // e.g. "stopWords.txt"

            // Output File. Contains links that were manually removed.
            badLinkFilename = clearSettings["BadLinkFile"]; // e.g. "badLinks.tsv"

            // Output File. Contains the links that exist.
            goodLinkFilename = clearSettings["GoodLinkFile"]; // e.g. "goodLinks.tsv"

            // If empty, it will do 1-to-1 alignment. Otherwise, it shows where groups of verses are mapped to other groups of verses. Can do many-to-1 and 1-to-many. Can do dynamic alignment.
            groupFilename = clearSettings["GroupFile"]; // e.g. "groups.tsv"

            // Input and Output file. Must have. Translation memory, can be empty. 
            tmFilename = clearSettings["TmFile"]; // e.g. "tm.tsv"

            // 2020.07.10 CL: Made targetFuncWordsFile a global variable to Form1
            targetFuncWordsFilename = clearSettings["TargetFuncWordsFile"]; // e.g. "targetFuncWords.txt"

            // 2020.07.10 CL: Why have this specifically in the targetFolder? Shouldn't Strong's Number information be the same for all languages?
            strongFilename = clearSettings["StrongFile"]; // e.g. "strongs.txt"

            // Input file. Must have it. If have done alignment, can bring in for consideration.
            oldJsonFilename = clearSettings["OldJsonFile"]; // e.g. "oldAlignment.json"
        }

        // 2020.10.19 CL: Need to separate out when we set the filenames and when we set the files (with path).
        // This is because the path may change when the project, testament, or filenames change as a command line option.
        private static void InitializeStateFiles()
        {
            //============================ CLEAR State Files ============================
            transModelFile = Path.Combine(targetFolder, translationTestamentPrefix + transModelFilename);
            transModelFileCW = Path.Combine(targetFolder, translationTestamentPrefix + transModelFilenameCW);
            manTransModelFile = Path.Combine(targetFolder, translationTestamentPrefix + manTransModelFilename);
            alignModelFile = Path.Combine(targetFolder, translationTestamentPrefix + alignModelFilename);
            alignModelFileCW = Path.Combine(targetFolder, translationTestamentPrefix + alignModelFilenameCW);
            groupFile = Path.Combine(targetFolder, translationTestamentPrefix + groupFilename);
            stopWordFile = Path.Combine(targetFolder, translationTestamentPrefix + stopWordFilename);
            badLinkFile = Path.Combine(targetFolder, translationTestamentPrefix + badLinkFilename);
            goodLinkFile = Path.Combine(targetFolder, translationTestamentPrefix + goodLinkFilename);
            tmFile = Path.Combine(targetFolder, translationTestamentPrefix + tmFilename);
            targetFuncWordsFile = Path.Combine(targetFolder, translationTestamentPrefix + targetFuncWordsFilename);
            strongFile = Path.Combine(targetFolder, translationTestamentPrefix + strongFilename);
            oldJson = Path.Combine(targetFolder, translationTestamentPrefix + oldJsonFilename);
        }


        // Was Do_Button10()
        public static string DeleteStateFiles()
        {
            Console.WriteLine("Deleting State Files");

            File.Delete(transModelFile);
            File.Delete(alignModelFile);
            File.Delete(transModelFileCW);
            File.Delete(alignModelFileCW);
            File.Delete(stopWordFile);
            File.Delete(badLinkFile);
            File.Delete(goodLinkFile);
            File.Delete(groupFile);
            File.Delete(strongFile);
            File.Delete(oldJson);
            File.Delete(t2gJsonFile);
            File.Delete(checkedAlignmentsFile);
            File.Delete(manTransModelFile);
            File.Delete(tmFile);

            return ("Deleted State Files.");
        }

        // Was Do_Button11()
        public static string InitializeState()
        {
            Console.WriteLine("Initializing Clear's State");

            // 2020.07.10 CL: It seems that Andi wanted to have the possibility that you can start CLEAR again and it would continue from where it left off.
            // However, since I added a new button that will start fresh with a new analysis, I want to be able to initialize the state with some files initially empty.
            // So need a method to call.
            // 2020.07.10 CL: There seem to be some of these that do not change because of processing through CLEAR. They may change based upon analysis by another program.
            // 2021.03.03 CL: Changed some of functions that read in data so if it doesn't exist, it will just return an empty data structure or null.

            if (contentWordsOnlySMT)
            {
                if (File.Exists(transModelFileCW)) translationModel = importExportService.ImportTranslationModel(transModelFileCW);
                if (File.Exists(alignModelFileCW)) alignmentModel = importExportService.ImportAlignmentModel(alignModelFileCW);
                
                translationModelRest = translationModel;
                // preAlignment = Data.BuildPreAlignmentTable(alignModel);
            }
            else if (contentWordsOnlyTC)
            {
                if (File.Exists(transModelFileCW)) translationModel = importExportService.ImportTranslationModel(transModelFileCW);
                if (File.Exists(alignModelFileCW)) alignmentModel = importExportService.ImportAlignmentModel(alignModelFileCW);

                if (File.Exists(transModelFile)) translationModelRest = importExportService.ImportTranslationModel(transModelFile);
                // var tmpAlignModel = BuildTransModels.ReadAlignModel(alignModelFile, mustExistFlag);
                // preAlignment = Data.BuildPreAlignmentTable(tmpAlignModel);
            }
            else
            {
                if (File.Exists(transModelFile)) translationModel = importExportService.ImportTranslationModel(transModelFile);
                if (File.Exists(alignModelFile)) alignmentModel = importExportService.ImportAlignmentModel(alignModelFile);

                translationModelRest = translationModel;
                // preAlignment = Data.BuildPreAlignmentTable(alignModel);
            }

            // preAlignment Not used yet in Clear3
            // preAlignment = Data.BuildPreAlignmentTable(alignModel); // Has key as sourceID and value as targetID

            // tm Not used yet in Clear3
            // tm = AutoAligner.ReadTM(tmFile); // tm is a Hashtable with the Key the source Strongs number, and the Value is a list of translations. 

            // translationModel = importExportService.ImportTranslationModel(transModelFile);
            // alignmentModel = importExportService.ImportAlignmentModel(alignModelFile);


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

            stopWords = stopWordsTemp;
            targetFunctionWords = targetFunctionWordsTemp;
            manTransModel = manTransModelTemp;
            goodLinks = goodLinksTemp;
            badLinks = badLinksTemp;
            groups = groupsTemp;
            oldLinks = oldLinksTemp;
            strongs = strongsTemp;

            return ("Initialized State.");
        }

        // Was Do_Button7()
        public static string TokenizeVerses()
        {
            Console.WriteLine("Tokenizing Verses");

            ShowTime();

            targetVerseCorpus =
                importExportService.ImportTargetVerseCorpusFromLegacy(
                    versesFile,
                    clearService.DefaultSegmenter,
                    puncs,
                    lang,
                    culture);

            ShowTime();

            Persistence.ExportTargetVerseCorpus(targetVerseCorpus, tokenTextFile, tokenLemmaFile, tokenIdFile);

            ShowTime();

            return (versesFile + " has been tokenized and targetVerseCorpus has been created.");
        }

        // Was Do_Button8()
        public static string CreateParallelCorpus()
        {
            Console.WriteLine("Creating Parallel Corpora");

            ShowTime();

            parallelCorpora = utility.CreateParallelCorpora(
                targetVerseCorpus,
                treeService,
                simpleVersification);

            Persistence.ExportParallelCorpora(parallelCorpora, sourceTextFile, sourceLemmaFile, sourceIdFile, sourceLemmaCatFile, targetTextFile, targetLemmaFile, targetIdFile);

            ShowTime();

            parallelCorporaCW = utility.FilterWordsFromParallelCorpora(
                parallelCorpora,
                sourceFunctionWords,
                targetFunctionWords);

            Persistence.ExportParallelCorpora(parallelCorporaCW, sourceTextFileCW, sourceLemmaFileCW, sourceIdFileCW, sourceLemmaCatFileCW, targetTextFileCW, targetLemmaFileCW, targetIdFileCW);

            ShowTime();

            parallelCorporaNoPunc = utility.FilterWordsFromParallelCorpora(
                 parallelCorpora,
                 puncs,
                 puncs);

            Persistence.ExportParallelCorpora(parallelCorpora, sourceTextNoPuncFile, sourceLemmaNoPuncFile, sourceIdNoPuncFile, sourceLemmaCatNoPuncFile, targetTextNoPuncFile, targetLemmaNoPuncFile, targetIdNoPuncFile);

            ShowTime();

            parallelCorporaNoPuncCW = utility.FilterWordsFromParallelCorpora(
                 parallelCorpora,
                 puncs,
                 puncs);

            Persistence.ExportParallelCorpora(parallelCorporaNoPuncCW, sourceTextNoPuncFileCW, sourceLemmaNoPuncFileCW, sourceIdNoPuncFileCW, sourceLemmaCatNoPuncFileCW, targetTextNoPuncFileCW, targetLemmaNoPuncFileCW, targetIdNoPuncFileCW);

            ShowTime();

            return ("Parallel files have been created.");
        }

        // Was Do_Button6()
        public static string BuildModels()
        {
            // Create a new runSpec if thotModel is specified, otherwise, use exisiting runSpec set by command line or processing.config.

            if (thotModel == "HMM")
            {
                runSpec = "HMM;1:10;H:5";
            }
            else if (thotModel != "")
            {
                runSpec = string.Format("{0};{1};{2}", thotModel, thotHeuristic, thotIterations);
            }

            (ParallelCorpora smtParallelCorpora, ParallelCorpora smtParallelCorporaCW) = InitializeParallelCorpora();

            Console.Write("Building Models");

            // Train a statistical translation model using the parallel corpora producing an estimated translation model and estimated alignment.
            // There are three possible scenarios for how to use parallel corpus with all words or content only words.
            // Within the SMTService.DefaultSMT, it writes and reads from the file, so any double differences is already done.
            // No need to read them in again.
            if (contentWordsOnlySMT)
            {
                Console.WriteLine(" with Content Words Only.");
                ShowTime();

                (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorporaCW, runSpec, epsilon);

                ShowTime();
                translationModelRest = translationModel;
                alignmentModelPre = alignmentModel;
                Persistence.ExportTranslationModel(translationModel, transModelFileCW);
                Persistence.ExportAlignmentModel(alignmentModel, alignModelFileCW);
                ShowTime();

                return ("Models built: " + transModelFileCW + "; " + alignModelFileCW);
            }
            else if (contentWordsOnlyTC)
            {
                Console.WriteLine(" with Content Words Only for Finding Terminal Candidates.");
                ShowTime();

                (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorporaCW, runSpec, epsilon);

                ShowTime();
                Persistence.ExportTranslationModel(translationModel, transModelFileCW);
                Persistence.ExportAlignmentModel(alignmentModel, alignModelFileCW);
                ShowTime();

                Console.WriteLine("Building Models (for All Words).");
                ShowTime();

                (var translationModelAllWords, var alignmentModelAllWords) = clearService.SMTService.DefaultSMT(smtParallelCorpora, runSpec, epsilon);

                ShowTime();
                translationModelRest = translationModelAllWords;
                alignmentModelPre = alignmentModelAllWords;
                Persistence.ExportTranslationModel(translationModelAllWords, transModelFile);
                Persistence.ExportAlignmentModel(alignmentModelAllWords, alignModelFile);
                ShowTime();

                return ("Models built: " + transModelFile + "; " + alignModelFile + "; " + transModelFileCW + "; " + alignModelFileCW);
            }
            else
            {
                Console.WriteLine(".");
                ShowTime();

                (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorpora, runSpec, epsilon);

                ShowTime();
                translationModelRest = translationModel;
                alignmentModelPre = alignmentModel;
                Persistence.ExportTranslationModel(translationModel, transModelFile);
                Persistence.ExportAlignmentModel(alignmentModel, alignModelFile);
                ShowTime();

                return ("Models built: " + transModelFile + "; " + alignModelFile);
            }
        }

        // returns the two sets (all words and content words only) parallel corpora based upon different settings for building models
        private static (ParallelCorpora, ParallelCorpora) InitializeParallelCorpora()
        {
            // Initialize the content words only and all words corpora
            ParallelCorpora smtParallelCorpora;
            ParallelCorpora smtParallelCorporaCW;

            Console.Write("Building Models: Initializing parallel corpora");

            // Update files for different scenarios
            if (useLemmaCatModel)
            {
                Console.Write(" with Source Lemma_Cat");

                if (useNoPuncModel)
                {
                    Console.WriteLine(" with No Source and No Target Punctuations.");
                    ShowTime();

                    smtParallelCorpora = Persistence.ImportParallelCorpus(sourceTextNoPuncFile, sourceLemmaCatNoPuncFile, sourceIdNoPuncFile, targetTextNoPuncFile, targetLemmaNoPuncFile, targetIdNoPuncFile);
                    smtParallelCorporaCW = Persistence.ImportParallelCorpus(sourceTextNoPuncFileCW, sourceLemmaCatNoPuncFileCW, sourceIdNoPuncFileCW, targetTextNoPuncFileCW, targetLemmaNoPuncFileCW, targetIdNoPuncFileCW);
                }
                else
                {
                    Console.WriteLine(".");
                    ShowTime();

                    smtParallelCorpora = Persistence.ImportParallelCorpus(sourceTextFile, sourceLemmaCatFile, sourceIdFile, targetTextFile, targetLemmaFile, targetIdFile);
                    smtParallelCorporaCW = Persistence.ImportParallelCorpus(sourceTextFileCW, sourceLemmaCatFileCW, sourceIdFileCW, targetTextFileCW, targetLemmaFileCW, targetIdFileCW);

                }
            }
            else if (useNoPuncModel)
            {
                Console.Write(" with No Source and No Target Punctuations.");
                ShowTime();

                if (parallelCorporaNoPunc == null) parallelCorporaNoPunc = Persistence.ImportParallelCorpus(sourceTextNoPuncFile, sourceLemmaNoPuncFile, sourceIdNoPuncFile, targetTextNoPuncFile, targetLemmaNoPuncFile, targetIdNoPuncFile);
                if (parallelCorporaNoPuncCW == null) parallelCorporaNoPuncCW = Persistence.ImportParallelCorpus(sourceTextNoPuncFileCW, sourceLemmaNoPuncFileCW, sourceIdNoPuncFileCW, targetTextNoPuncFileCW, targetLemmaNoPuncFileCW, targetIdNoPuncFileCW);

                smtParallelCorpora = parallelCorporaNoPunc;
                smtParallelCorporaCW = parallelCorporaNoPuncCW;
            }
            else
            {
                Console.WriteLine(".");
                ShowTime();

                if (parallelCorpora == null) parallelCorpora = Persistence.ImportParallelCorpus(sourceTextFile, sourceLemmaFile, sourceIdFile, targetTextFile, targetLemmaFile, targetIdFile);
                if (parallelCorporaCW == null) parallelCorporaCW = Persistence.ImportParallelCorpus(sourceTextFileCW, sourceLemmaFileCW, sourceIdFileCW, targetTextFileCW, targetLemmaFileCW, targetIdFileCW);

                smtParallelCorpora = parallelCorpora;
                smtParallelCorporaCW = parallelCorporaCW;
            }

            ShowTime();

            return (smtParallelCorpora, smtParallelCorporaCW);
        }

        // Was Do_Button1()
        public static string AutoAlign()
        {
            // Use the parallel corpora (with both the function words and
            // the content words included) to state the zone alignment
            // problems for the tree-based auto-aligner.
            //
            // CL: Fixed to have a ZoneAlignmentProblem be a TargetZone and SourceZone
            // Although ZonePair and ZoneAlignmentProblem are essentially the same,
            // Keeping them as different records helps distinguish between them.

            List<ZoneAlignmentProblem> zoneAlignmentProblems =
                parallelCorpora.List
                .Select(zonePair =>
                    new ZoneAlignmentProblem(
                        zonePair.TargetZone,
                        zonePair.SourceZone))
                        // zonePair.SourceZone.List.First().SourceID.VerseID,
                        // zonePair.SourceZone.List.Last().SourceID.VerseID))
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
                    translationModelRest,
                    translationModel,
                    useLemmaCatModel,
                    manTransModel,
                    alignmentModel,
                    alignmentModelPre,
                    useAlignModel,
                    puncs,
                    stopWords,
                    goodLinks, goodLinkMinCount,
                    badLinks, badLinkMinCount,
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

        // Was Do_Button2()
        public static string IncrementalUpdate()
        {
            Console.WriteLine("Updating Incrementally. Not implemented yet in Clear3.");
            return "Story2 not implemented.";

        }

        // Was Do_Button3()
        public static string GlobalUpdate()
        {
            Console.WriteLine("Updating Globally. Not implemented yet in Clear3.");
            return "Story3 not implemented.";
        }

        // Was Do_Button4()
        public static string AlignG2T()
        {
            Console.WriteLine("Aligning Gateway-to-Translation via Manuscript-to-Gateway. Not implemented yet in Clear3.");
            return "Story4 not implemented.";
        }

        // Was Do_Button5()
        public static string AlignM2T()
        {
            Console.WriteLine("Aligning Manuscript-to-Translation via Manuscript-to-Gateway. Not implemented yet in Clear3.");
            return "Story5 not implemented.";
        }

        // Was Do_Button9()
        public static string ProcessAll()
        {
            Console.WriteLine("Doing ProcessAll command");

            DeleteStateFiles(); // Delete State files
            InitializeState(); // Initialize State
            TokenizeVerses(); // Tokenize
            CreateParallelCorpus(); // Parallelize
            BuildModels(); // Create statistical models
            AutoAlign(); // Auto align

            // Copy alignment.json to CheckedAlignment.json to pretend we did manual alignment
            File.Delete(checkedAlignmentsFile);
            File.Copy(jsonOutput, checkedAlignmentsFile);

            GlobalUpdate(); // Global update

            return "Done Processing.";
        }

        // Was Do_Button13()
        public static string FreshStart()
        {
            Console.WriteLine("Doing FreshStart command");

            DeleteStateFiles(); // Delete State files
            InitializeState(); // Initialize State
            TokenizeVerses(); // Tokenize
            CreateParallelCorpus(); // Parallelize
            BuildModels(); // Create statistical models
            AutoAlign(); // Auto align

            return ("Done Processing.");
        }

        // Was D0_Button12()
        public static string DoStart()
        {
            Console.WriteLine("Doing DoStart command");

            InitializeState(); // Initialize State
            TokenizeVerses(); // Tokenize
            CreateParallelCorpus(); // Parallelize
            BuildModels(); // Create statistical models
            AutoAlign(); // Auto align

            return "Done Processing.";
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
        private static Dictionary<string, string> clearSettings; // Input from .config file
        private static string processingConfigFilename;
        private static string processingConfigFile;
        private static Dictionary<string, string> processingSettings; // Input from .config file
        private static string runConfigFile;
        private static Dictionary<string, string> runSettings; // Input from .config file
        private static string translationConfigFilename;
        private static string translationFolder;
        private static string translationConfigFile;

        private static Dictionary<string, string> translationSettings;

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
        private static string culture; // Used in C# routines that consider language and culture, such as .ToLower()
        private static string translation; // 2020.09.25 CL: This is now the name of the translation, sometimes including a numbers to indicate the year of the copyright, e.g. NIV84
        private static string testament; // 2020.07.11 CL: Which testament we are working on. I think at some point, we should not need to distinguish between them.

        private static string runSpec; // 1:10;H:5
        private static double epsilon; // Must exceed this to be counted into model, 0.1
        private static string thotModel; // For SIL auto aligner interations
        private static string thotHeuristic; // For SIL auto aligner interations
        private static string thotIterations; // For SIL auto aligner interations
        private static bool contentWordsOnlySMT; // Use only content words for creating the statistical models: transModel and alignModel
        private static bool contentWordsOnlyTC; // Use only content words for creating the statistical translation model: transModel
        private static bool useLemmaCatModel; // whether to use the lemma_cat in creating the SMT models
        private static bool useNoPuncModel; // whether to use the target corpora without punctuations in creating the SMT models
        private static bool useAlignModel; // whether to use the alignment model
        private static bool contentWordsOnly; // whether to align content words

        private static string strEpsilon;
        private static string strContentWordsOnlySMT;
        private static string strContentWordsOnlyTC;
        private static string strUseLemmaCatModel;
        private static string strUseNoPuncModel;
        private static string strUseAlignModel;
        private static string strContentWordsOnly;

        private static string translationTestamentPrefix;

        private static string transModelFilename; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts
        private static string transModelFile; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts
        private static string transModelFilenameCW; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts
        private static string transModelFileCW; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts

        private static TranslationModel translationModel; // translation model
        private static TranslationModel translationModelRest; // translation model for all words

        private static string manTransModelFilename; // the file that contains the translation model created from manually checked alignments
        private static string manTransModelFile; // the file that contains the translation model created from manually checked alignments
        private static TranslationModel manTransModel; // the translation model created from manually checked alignments

        private static string alignModelFilename; // the file that contains the alignment model;
        private static string alignModelFile; // the file that contains the alignment model;
        private static string alignModelFilenameCW; // the file that contains the alignment model;
        private static string alignModelFileCW; // the file that contains the alignment model;

        // private static AlignmentModel alignModel; // alignment model
        private static AlignmentModel alignmentModel; // alignment model
        private static AlignmentModel alignmentModelPre; // alignment model
        // private static AlignmentModel preAlignment; // alignment table

        private static string glossFilename; // 2020.07.11 CL: The file where the source glosses are kept.
        private static string glossFile; // 2020.07.11 CL: The file where the source glosses are kept.
        private static Dictionary<string, Gloss> glossTable; // Word glosses

        private static string puncsFilename; // 2020.07.11 CL: The file where the source punctuations kept.
        private static string puncsFile; // 2020.07.11 CL: The file where the source punctuations kept.
        private static List<string> puncs; // list of punctuation marks
        // private static HashSet<string> puncs; // list of punctuation marks

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
        // private static HashSet<string> stopWords; // list of target words not to be linked

        private static int badLinkMinCount; // the minimal count required for treating a link as bad
        private static string badLinkFilename; // contains the list of pairs of words that should not be linked
        private static string badLinkFile; // contains the list of pairs of words that should not be linked
        private static Dictionary<string, int> badLinks; // list of pairs of words that should not be linked

        private static int goodLinkMinCount; // the minimal count required for treating a link as good
        private static string goodLinkFilename; // contains the list of pairs of words that should be linked
        private static string goodLinkFile; // contains the list of pairs of words that should be linked
        private static Dictionary<string, int> goodLinks; // list of pairs of words that should be linked

        private static string strBadLinkMinCount;
        private static string strGoodLinkMinCount;

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
        // private static HashSet<string> sourceFunctionWords; // function words

        private static string targetFuncWordsFilename; // 2020.07.10 CL: Added this to make it global to this form since targetFuncWords is global.
        private static string targetFuncWordsFile; // 2020.07.10 CL: Added this to make it global to this form since targetFuncWords is global.
        // private static List<string> targetFuncWords;
        private static List<string> targetFunctionWords;
        // private static HashSet<string> targetFunctionWords;

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
        private static ParallelCorpora parallelCorporaNoPunc;
        private static ParallelCorpora parallelCorporaNoPuncCW;

        private static string tokenTextFilename;
        private static string tokenTextFile;
        private static string tokenLemmaFilename;
        private static string tokenLemmaFile;
        private static string tokenIdFilename;
        private static string tokenIdFile;

        // private static string sourceTextFilenameM;
        // private static string sourceTextFileM;
        // private static string sourceIdFilenameM;
        // private static string sourceIdFileM;
        // private static string sourceLemmaFilenameM;
        // private static string sourceLemmaFileM;
        // private static string sourceLemmaCatFilenameM;
        // private static string sourceLemmaCatFileM;

        private static string sourceTextFilename;
        private static string sourceTextFile;
        private static string sourceIdFilename;
        private static string sourceIdFile;
        private static string sourceLemmaFilename;
        private static string sourceLemmaFile;
        private static string sourceLemmaCatFilename;
        private static string sourceLemmaCatFile;

        private static string sourceTextNoPuncFilename;
        private static string sourceTextNoPuncFile;
        private static string sourceIdNoPuncFilename;
        private static string sourceIdNoPuncFile;
        private static string sourceLemmaNoPuncFilename;
        private static string sourceLemmaNoPuncFile;
        private static string sourceLemmaCatNoPuncFilename;
        private static string sourceLemmaCatNoPuncFile;

        private static string targetTextFilename;
        private static string targetTextFile;
        private static string targetIdFilename;
        private static string targetIdFile;
        private static string targetLemmaFilename;
        private static string targetLemmaFile;

        private static string targetTextNoPuncFilename;
        private static string targetTextNoPuncFile;
        private static string targetIdNoPuncFilename;
        private static string targetIdNoPuncFile;
        private static string targetLemmaNoPuncFilename;
        private static string targetLemmaNoPuncFile;

        // private static string targetPuncFilename;
        // private static string targetPuncFile;
        // private static string targetPuncLowerFilename;
        // private static string targetPuncLowerFile;

        private static string sourceTextFilenameCW;
        private static string sourceTextFileCW;
        private static string sourceIdFilenameCW;
        private static string sourceIdFileCW;
        private static string sourceLemmaFilenameCW;
        private static string sourceLemmaFileCW;
        private static string sourceLemmaCatFilenameCW;
        private static string sourceLemmaCatFileCW;

        private static string sourceTextNoPuncFilenameCW;
        private static string sourceTextNoPuncFileCW;
        private static string sourceIdNoPuncFilenameCW;
        private static string sourceIdNoPuncFileCW;
        private static string sourceLemmaNoPuncFilenameCW;
        private static string sourceLemmaNoPuncFileCW;
        private static string sourceLemmaCatNoPuncFilenameCW;
        private static string sourceLemmaCatNoPuncFileCW;

        private static string targetTextFilenameCW;
        private static string targetTextFileCW;
        private static string targetIdFilenameCW;
        private static string targetIdFileCW;
        private static string targetLemmaFilenameCW;
        private static string targetLemmaFileCW;

        private static string targetTextNoPuncFilenameCW;
        private static string targetTextNoPuncFileCW;
        private static string targetIdNoPuncFilenameCW;
        private static string targetIdNoPuncFileCW;
        private static string targetLemmaNoPuncFilenameCW;
        private static string targetLemmaNoPuncFileCW;

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