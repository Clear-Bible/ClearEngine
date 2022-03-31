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

using Lemmatizer;

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

        public static void SetSmtImplementation(string arg)
        {
            smtImplementation = arg;
        }

        public static void SetSmtModel(string arg)
        {
            smtModel = arg;
        }

        public static void SetSmtIterations(string arg)
        {
            smtIterations = arg;
        }

        public static void SetSmtEpsilon(string arg)
        {
            smtEpsilon = arg;
        }

        public static void SetSmtHeuristic(string arg)
        {
            smtHeuristic = arg;
        }

        public static void SetContentWordsOnlySMT(string arg)
        {
            strContentWordsOnlySMT = arg;
        }

        public static void SetContentWordsOnlyTC(string arg)
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

        public static void SetUseNormalizedTransModelProbabilities(string arg)
        {
            strUseNormalizedTransModelProbabilities = arg;
        }

        public static void SetUseNormalizedAlignModelProbabilities(string arg)
        {
            strUseNormalizedAlignModelProbabilities = arg;
        }

        public static void SetUseMorphology(string arg)
        {
            strUseMorphology = arg;
        }

        public static void SetReuseTokenFiles(string arg)
        {
            strReuseTokenFiles = arg;
        }

        public static void SetReuseLemmFiles(string arg)
        {
            strReuseLemmaFiles = arg;
        }

        public static void SetReuseParallelCorporaFiles(string arg)
        {
            strReuseParallelCorporaFiles = arg;
        }

        public static void SetReuseSmtModelFiles(string arg)
        {
            strReuseSmtModelFiles = arg;
        }

        public static void SetLowerCaseMethod(string arg)
        {
            lowerCaseMethod = arg;
        }

        public static void InitializeConfig()
        {
            Console.WriteLine();
            Console.WriteLine("Running ClearEngine3");

            ReadConfig(clearConfigFilename);

            InitializeClear30Service();
        }

        // CL: 2020.08.21 Modified to use a .config (XML) file to store all of the filenames and booleans rather than embedding them in the code.
        private static void ReadConfig(string configFilename)
        {
            clearSettings = Configuration.GetSettings(configFilename);

            resourcesFolder = clearSettings["Resources_Foldername"]; // e.g. 
            processingFolder = clearSettings["Processing_Foldername"]; // e.g. 

            runConfigFilename = clearSettings["Run_Configuration_Filename"];
            runSettings = Configuration.GetSettings(runConfigFilename);

            // Run and some processing settings can be overridden by command line parameters
            project = (string)runSettings["Project"]; // e.g. "NIV84-SIL-test"
            testament = (string)runSettings["Testament"]; // e.g. "OT" or "NT"

            // Set file information in resourcesFolder

            // treeFoldername = clearSettings["Tree_Folder"]; // Hardcoded in DownloadResource() in ResourceService.cs. Probably should allow it to be in a config file.
            sourceFuncWordsFilename = clearSettings["Source_Function_Words_Filename"];
            puncsFilename = clearSettings["Punctuations_Filename"];
            glossFilename = clearSettings["Gloss_Filename"];

            // sourceFoldername = clearSettings["Source_Foldername"]; // Source text files are created from trees on the fly.
            // freqPhrasesFilename = clearSettings["Frequent_Phrases_Filename"]; // Not used in ClearEngine3.

            //============================ Output/Input Files Used to Pass Data Between Functions ============================
            //
            tokenTextFilename = clearSettings["Token_Text_Filename"];
            tokenTextIdFilename = clearSettings["Token_Text_Id_Filename"];
            tokenLemmaFilename = clearSettings["Token_Lemma_Filename"];
            tokenLemmaIdFilename = clearSettings["Token_Lemma_Id_Filename"];

            sourceTextFilename = clearSettings["Source_Text_Filename"];
            sourceLemmaFilename = clearSettings["Source_Lemma_Filename"];
            sourceIdFilename = clearSettings["Source_Id_Filename"];
            sourceLemmaCatFilename = clearSettings["Source_Lemma_Cat_Filename"];

            targetTextFilename = clearSettings["Target_Text_Filename"];
            targetTextIdFilename = clearSettings["Target_Text_Id_Filename"];
            targetLemmaFilename = clearSettings["Target_Lemma_Filename"];
            targetLemmaIdFilename = clearSettings["Target_Lemma_Id_Filename"];

            // sourceTextFilenameM = clearSettings["Source_Text_Filename_M"]; // Created on the fly from trees.
            // sourceLemmaFilenameM = clearSettings["Source_Lemma_Filename_M"]; // Created on the fly from trees.
            // sourceIdFilenameM = clearSettings["Source_Id_Filename_M"]; // Created on the fly from trees.
            // sourceLemmaCatFilenameM = clearSettings["Source_Lemma_Cat_Filename_M"]; // Created on the fly from trees.

            //============================ Output Files Only ============================
            // Files not part of the state, nor used as output/input to pass data between different functions
            // Output file. Has the alignment in .json format, which is more readable than XML format.
            jsonOutputFilename = clearSettings["Json_Output_Filename"]; // e.g "alignments.json", Should update variable to ...File
            jsonLemmasOutputFilename = clearSettings["Json_Lemmas_Output_Filename"]; // e.g "alignment.json", Should update variable to ...File
            jsonFilename = clearSettings["Json_Filename"]; // e.g "alignment.json", Should merge with jsonOutput

            // Output file. Has the alignment in .json format, which is more readable than XML format. Gateway language alignment. Manuscript to gateway, or gateway to target?
            // t2gJsonFilename = clearSettings["T2G_Json_Filename"]; // e.g. "gAlignment.json", Not used in ClearEngine3

            //============================ Input Files Only ============================
            versesFilename = clearSettings["Verses_Filename"]; // e.g. "Verses.txt"

            // checkedAlignmentsFilename = clearSettings["Checked_Alignments_Filename"]; // e.g. "CheckedAlignments.json"
            // m_g_jsonFilename = clearSettings["M_G_Json_Filename"]; // e.g "m_g_alignment.json", the CLEAR json where manuscript, gTranslation and gLinks are instantiated but translation and links are still empty
            // gAlignmentFilename = clearSettings["Gateway_Alignment_Filename"]; // e.g. "gAlignment.json"
            // auto_m_t_alignmentFilename = clearSettings["Auto_M_T_Alignment_Filename"]; // e.g. "auto_m_t_alignment.json"

            //============================ Input Files Only from Lexicon ============================
            lemmaDataFilename = clearSettings["Word_To_Lemmas_Filename"];

            // Initialize state related filenames
            InitializeStateFilenames();

            // Initialize resource related file paths
            InitializeResourceFiles();
        }

        private static void InitializeResourceFiles()
        {
            // Set file information in resourcesFolder

            // treeFolder = Path.Combine(resourcesFolder, treeFoldername); // e.g. "Trees", folder with manuscript trees. Fixed. Doesn't change. Input to CLEAR, Andi's own XML format
            sourceFuncWordsFile = Path.Combine(resourcesFolder, sourceFuncWordsFilename); // e.g. "sourceFuncwords.txt"
            puncsFile = Path.Combine(resourcesFolder, puncsFilename); // e.g. "puncs.txt"
            glossFile = Path.Combine(resourcesFolder, glossFilename); // e.g. "Gloss.tsv"

            // sourceFolder = Path.Combine(resourcesFolder, sourceFoldername); // e.g. "Manuscript",folder with the original language files.
            // freqPhrasesFile = Path.Combine(resourcesFolder, freqPhrasesFilename); // e.g. "freqPhrases.tsv"
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

            // Import auxiliary assumptions from files: punctuation,
            // stop words, function words, manual translation model,
            // good and bad links, old alignment, glossary table,
            // and Strongs data.

            (HashSet<string> puncsTemp,
             HashSet<string> stopWords,
             HashSet<string> sourceFunctionWordsTemp,
             HashSet<string> targetFunctionWords,
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
                 oldJsonFile,
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
            // Set Project Configuration Parameters
            projectFolder = Path.Combine(processingFolder, project);
            projectSettings = Configuration.GetSettings(clearSettings, "Project_Configuration_Filename", projectFolder);
            translationSettings = Configuration.GetSettings(projectSettings, "Translation_Configuration_Filename", projectFolder);

            // Set variables related to which language, translation, and testament to process.

            lang = translationSettings["Language"]; // e.g. "English"
            targetCSharpCulture = translationSettings["CSharpCulture"]; // e.g. en-US
            translation = translationSettings["Translation"]; // e.g. NIV84

            // 2021.03.29 CL: Decided to prefix some of the state filenames with "<translation>.<testament>." since this makes it easier to identify
            // which translation and testament they are for.
            // This will also allow the option to put all of the files into one folder rather than an OT and NT subfolders and choose
            // which ones are shared by OT and NT and which ones are not.

            translationTestamentPrefix = string.Format("{0}.{1}.", translation.ToLower(), testament.ToLower());

            // Since we create translationSettings as a merging of the particular translation.config file in the project folder and the one in the Clear folder,
            // We can probably just assume the default values for file and type are in the one in the Clear folder rather than having a separate attribute for the default.

            versificationFilename = translationSettings["Versification_File"];
            versificationType = translationSettings["Versification_Type"]; // e.g. "NRT", "S1"

            if (versificationType == "")
            {
                versificationType = translationSettings["Versification_Default_Type"]; ;
            }

            if (versificationFilename == "")
            {
                versificationFile = Path.Combine(resourcesFolder, translationSettings["Versification_Default_File"]); // e.g. "Versification.xml"
            }
            else
            {
                versificationFile = Path.Combine(projectFolder, versificationFilename); // e.g. "niv84.extracted_versification.xml"
            }

            // versificationList = Versification.ReadVersificationList(versificationFile, versificationType, "id"); // Read in the versification

            simpleVersification = importExportService.ImportSimpleVersificationFromLegacy(
                                    versificationFile,
                                    versificationType);

            // Set targetFolder to Language and NT or OT, e.g. "processingFolder\\<translation>\\<testament>"
            // 2021.07.21 CL: Changed to not assume folder names are "OT" and "NT", but the folder names are in the Project.config file.
            testamentFoldername = projectSettings["New_Testament_Foldername"];
            if (testament == "OT")
            {
                testamentFoldername = projectSettings["Old_Testament_Foldername"];
            }
            targetFolder = Path.Combine(projectFolder, testamentFoldername);

            //============================ Output/Input Files Used to Pass Data Between Functions ============================
            tokensFoldername = clearSettings["Tokens_Foldername"];
            tokensFolder = Path.Combine(targetFolder, tokensFoldername);
            if (!Directory.Exists(tokensFolder))
            {
                Directory.CreateDirectory(tokensFolder);
            }

            tokenTextFile = Path.Combine(tokensFolder, translationTestamentPrefix + tokenTextFilename);
            tokenTextIdFile = Path.Combine(tokensFolder, translationTestamentPrefix + tokenTextIdFilename);
            tokenLemmaFile = Path.Combine(tokensFolder, translationTestamentPrefix + tokenLemmaFilename);
            tokenLemmaIdFile = Path.Combine(tokensFolder, translationTestamentPrefix + tokenLemmaIdFilename);

            // Parallel Corpus Files

            corporaFoldername = clearSettings["Corpora_Foldername"];
            corporaFolder = Path.Combine(targetFolder, corporaFoldername);
            if (!Directory.Exists(corporaFolder))
            {
                Directory.CreateDirectory(corporaFolder);
            }

            sourceTextFile = Path.Combine(corporaFolder, translationTestamentPrefix + sourceTextFilename);
            sourceLemmaFile = Path.Combine(corporaFolder, translationTestamentPrefix + sourceLemmaFilename);
            sourceIdFile = Path.Combine(corporaFolder, translationTestamentPrefix + sourceIdFilename);
            sourceLemmaCatFile = Path.Combine(corporaFolder, translationTestamentPrefix + sourceLemmaCatFilename);

            targetTextFile = Path.Combine(corporaFolder, translationTestamentPrefix + targetTextFilename);
            targetTextIdFile = Path.Combine(corporaFolder, translationTestamentPrefix + targetTextIdFilename);
            targetLemmaFile = Path.Combine(corporaFolder, translationTestamentPrefix + targetLemmaFilename);
            targetLemmaIdFile = Path.Combine(corporaFolder, translationTestamentPrefix + targetLemmaIdFilename);

            //============================ Output Files Only ============================

            jsonOutputBase = Path.Combine(targetFolder, translationTestamentPrefix + jsonOutputFilename);
            jsonLemmasOutputBase = Path.Combine(targetFolder, translationTestamentPrefix + jsonLemmasOutputFilename);
            jsonFile = Path.Combine(targetFolder, translationTestamentPrefix + jsonFilename);
            // t2gJsonFile = Path.Combine(targetFolder, translationTestamentPrefix + t2gJsonFilename);

            //============================ Input Files Only ============================
            versesFile = Path.Combine(targetFolder, translationTestamentPrefix + versesFilename);
            // checkedAlignmentsFile = Path.Combine(targetFolder, translationTestamentPrefix + checkedAlignmentsFilename);
            // m_g_jsonFile = Path.Combine(targetFolder, translationTestamentPrefix + m_g_jsonFilename);
            // gAlignmentFile = Path.Combine(targetFolder, translationTestamentPrefix + gAlignmentFilename);
            // auto_m_t_alignmentFile = Path.Combine(targetFolder, translationTestamentPrefix + auto_m_t_alignmentFilename);

            //============================ Input Files Only from Lexicon ============================
            lexiconFoldername = clearSettings["Lexicon_Foldername"];
            lexiconFolder = Path.Combine(targetFolder, lexiconFoldername);
            lemmaDataFile = Path.Combine(lexiconFolder, translationTestamentPrefix + lemmaDataFilename);

            InitializeStateFiles();
        }

        public static void InitializeProcessingSettings()
        {
            // If there is a specific processing configuration file, then use it, otherwise use the generic/default one.
            processingSettings = Configuration.GetSettings(clearSettings, "Processing_Configuration_Filename", projectFolder);

            if (lowerCaseMethod == null) lowerCaseMethod = processingSettings["LowerCaseForLemma"];

            // Set Processing Parameters
            if (smtImplementation == null) smtImplementation = processingSettings["SmtImplementation"];
            if (smtModel == null) smtModel = processingSettings["SmtModel"];
            if (smtIterations == null) smtIterations = processingSettings["SmtIterations"];
            if (smtEpsilon == null) smtEpsilon = processingSettings["SmtEpsilon"]; // Must exceed this to be counted into model, e.g. "0.1"
            if (smtHeuristic == null) smtHeuristic = processingSettings["SmtHeuristic"];
            
            if (strContentWordsOnlySMT == null) strContentWordsOnlySMT = processingSettings["ContentWordsOnlySMT"]; // e.g. "true" Only use content words for building models
            if (strContentWordsOnlyTC == null) strContentWordsOnlyTC = processingSettings["ContentWordsOnlyTC"]; // e.g. "true" Only use content words for building models
            if (strContentWordsOnly == null) strContentWordsOnly = processingSettings["ContentWordsOnly"]; // e.g. "true" Only align content words

            if (strUseAlignModel == null) strUseAlignModel = processingSettings["UseAlignModel"]; // e.g. "true"
            if (strUseLemmaCatModel == null) strUseLemmaCatModel = processingSettings["UseLemmaCatModel"]; // e.g. "true"
            if (strUseNoPuncModel == null) strUseNoPuncModel = processingSettings["UseNoPuncModel"]; // e.g. "
            if (strUseNormalizedTransModelProbabilities == null) strUseNormalizedTransModelProbabilities = processingSettings["UseNormalizedTransModelProbabilities"]; // e.g. "true"
            if (strUseNormalizedAlignModelProbabilities == null) strUseNormalizedAlignModelProbabilities = processingSettings["UseNormalizedAlignModelProbabilities"]; // e.g. "true"

            if (strReuseTokenFiles == null) strReuseTokenFiles = processingSettings["ReuseTokenizedFiles"]; // e.g. "true"
            if (strReuseLemmaFiles == null) strReuseLemmaFiles = processingSettings["ReuseLemmatizedFiles"]; // e.g. "true"
            if (strReuseParallelCorporaFiles == null) strReuseParallelCorporaFiles = processingSettings["ReuseParallelCorporaFiles"]; // e.g. "true"
            if (strReuseSmtModelFiles == null) strReuseSmtModelFiles = processingSettings["ReuseSmtModelFiles"]; // e.g. "true"

            if (strBadLinkMinCount == null) strBadLinkMinCount = processingSettings["BadLinkMinCount"]; // e.g. "3", the minimal count required for treating a link as bad
            if (strGoodLinkMinCount == null) strGoodLinkMinCount = processingSettings["GoodLinkMinCount"]; // e.g. "3" the minimal count required for treating a link as bad

            // Convert strings parameters to values

            contentWordsOnlySMT = (strContentWordsOnlySMT == "true"); // e.g. "true" Only use content words for building models
            contentWordsOnlyTC = (strContentWordsOnlyTC == "true"); // e.g. "true" Only use content words for finding terminal candidates
            contentWordsOnly = (strContentWordsOnly == "true"); // e.g. "true" Only align content words

            useMorphology = (strUseMorphology == "true"); // e.g. "true"

            useAlignModel = (strUseAlignModel == "true"); // e.g. "true"
            useLemmaCatModel = (strUseLemmaCatModel == "true"); // e.g. "true"
            useNoPuncModel = (strUseNoPuncModel == "true"); // e.g. "true"
            useNormalizedTransModelProbabilities = (strUseNormalizedTransModelProbabilities == "true"); // e.g. "true"
            useNormalizedAlignModelProbabilities = (strUseNormalizedAlignModelProbabilities == "true"); // e.g. "true"

            reuseTokenFiles = (strReuseTokenFiles == "true"); // e.g. "true"
            reuseLemmaFiles = (strReuseLemmaFiles == "true"); // e.g. "true"
            reuseParallelCorporaFiles = (strReuseParallelCorporaFiles == "true"); // e.g. "true"
            reuseSmtModelFiles = (strReuseSmtModelFiles == "true"); // e.g. "true"

            badLinkMinCount = int.Parse(strBadLinkMinCount); // e.g. "3", the minimal count required for treating a link as bad
            goodLinkMinCount = int.Parse(strGoodLinkMinCount); // e.g. "3" the minimal count required for treating a link as bad

            runSpec = string.Format("{0}-{1}-{2}-{3}-{4}", smtImplementation, smtModel, smtIterations, smtEpsilon, smtHeuristic);

            string autoType = "_auto.json";
            jsonOutputBase = jsonOutputBase.Replace(".json", autoType); // Want to distinguish these from gold standard alignments
            jsonLemmasOutputBase = jsonLemmasOutputBase.Replace(".json", autoType); // Want to distinguish these from gold standard alignments

            string alignmentType = "_all.json";
            if (contentWordsOnly)
            {
                alignmentType = "_content.json";
            }
            jsonOutputBase = jsonOutputBase.Replace(".json", alignmentType);
            jsonLemmasOutputBase = jsonLemmasOutputBase.Replace(".json", alignmentType);


            jsonOutput = jsonOutputBase.Replace(".alignments", string.Format(".alignments_{0}", runSpec));
            jsonLemmasOutput = jsonLemmasOutputBase.Replace(".lemmas", string.Format(".lemmas_{0}", runSpec));
        }

        // 2020.07.10 CL: These files define the state of CLEAR and should all be in the targetFolder, and may change during processing.
        private static void InitializeStateFilenames()
        {
            //============================ CLEAR State Filenames ============================
            // 
            // They are input and output files. You must have them though they can be empty.
            // Has probablities of a manuscript language translated into a target word. Story #1 needs to read in this file after creating it with #6.
            transModelFilename = clearSettings["Translation_Model_Filename"]; // e.g. "transModel.tsv"

            // Input and Output file. If you already have manually checked alignment, it will give statistics. Must have but can be empty.
            manTransModelFilename = clearSettings["Manual_Translation_Model_Filename"];  // e.g. "manTransModel.tsv"

            // Must Have. Input and Output file. Token based alignment data, so for a particular verse and word, what are the statistics for translation. Story #1 needs to read in this file after creating it with #6.
            alignModelFilename = clearSettings["Alignment_Model_Filename"]; // e.g. "alignModel.tsv"

            // Input file. Must create this. Contains word the aligner should ignore. These are because they tend to not align well across languages, such as English aux verbs.
            stopWordFilename = clearSettings["Stop_Word_Filename"]; // e.g. "stopWords.txt"

            // Output File. Contains links that were manually removed.
            badLinkFilename = clearSettings["Bad_Links_Filename"]; // e.g. "badLinks.tsv"

            // Output File. Contains the links that exist.
            goodLinkFilename = clearSettings["Good_Links_Filename"]; // e.g. "goodLinks.tsv"

            // If empty, it will do 1-to-1 alignment. Otherwise, it shows where groups of verses are mapped to other groups of verses. Can do many-to-1 and 1-to-many. Can do dynamic alignment.
            groupFilename = clearSettings["Group_Filename"]; // e.g. "groups.tsv"

            // 2020.07.10 CL: Made targetFuncWordsFile a global variable to Form1
            targetFuncWordsFilename = clearSettings["Target_Function_Words_Filename"]; // e.g. "targetFuncWords.txt"

            // 2020.07.10 CL: Why have this specifically in the targetFolder? Shouldn't Strong's Number information be the same for all languages?
            strongFilename = clearSettings["Strongs_Filename"]; // e.g. "strongs.txt"

            // Input file. Must have it. If have done alignment, can bring in for consideration.
            oldJsonFilename = clearSettings["Old_Json_Filename"]; // e.g. "oldAlignment.json"

            // Input and Output file. Must have. Translation memory, can be empty. 
            // tmFilename = clearSettings["Translation_Memory_Filename"]; // e.g. "tm.tsv"
        }

        // 2020.10.19 CL: Need to separate out when we set the filenames and when we set the files (with path).
        // This is because the path may change when the project, testament, or filenames change as a command line option.
        private static void InitializeStateFiles()
        {
            //============================ CLEAR State Files ============================
            // SMT Model Files

            modelsFoldername = clearSettings["Models_Foldername"];
            modelsFolder = Path.Combine(targetFolder, modelsFoldername);
            if (!Directory.Exists(modelsFolder))
            {
                Directory.CreateDirectory(modelsFolder);
            }

            transModelFile = Path.Combine(modelsFolder, translationTestamentPrefix + transModelFilename);
            alignModelFile = Path.Combine(modelsFolder, translationTestamentPrefix + alignModelFilename);

            manTransModelFile = Path.Combine(targetFolder, translationTestamentPrefix + manTransModelFilename);
            groupFile = Path.Combine(targetFolder, translationTestamentPrefix + groupFilename);
            stopWordFile = Path.Combine(targetFolder, translationTestamentPrefix + stopWordFilename);
            badLinkFile = Path.Combine(targetFolder, translationTestamentPrefix + badLinkFilename);
            goodLinkFile = Path.Combine(targetFolder, translationTestamentPrefix + goodLinkFilename);
            targetFuncWordsFile = Path.Combine(targetFolder, translationTestamentPrefix + targetFuncWordsFilename);
            strongFile = Path.Combine(targetFolder, translationTestamentPrefix + strongFilename);
            oldJsonFile = Path.Combine(targetFolder, translationTestamentPrefix + oldJsonFilename);

            // tmFile = Path.Combine(targetFolder, translationTestamentPrefix + tmFilename);
        }


        // Was Do_Button10()
        public static (bool, string) DeleteStateFiles()
        {
            Console.WriteLine("Deleting State Files");

            DeleteFilesInFolder(tokensFolder, "*.txt");
            DeleteFilesInFolder(corporaFolder, "*.txt");
            DeleteFilesInFolder(modelsFolder, "*.tsv");
            DeleteFilesInFolder(modelsFolder, "*.txt"); // For IBM Pharaoh files

            File.Delete(stopWordFile);
            File.Delete(badLinkFile);
            File.Delete(goodLinkFile);
            File.Delete(groupFile);
            File.Delete(strongFile);
            File.Delete(oldJsonFile);
            // File.Delete(t2gJsonFile);
            // File.Delete(checkedAlignmentsFile);
            File.Delete(manTransModelFile);
            // File.Delete(tmFile);

            return (true, "Deleted State Files.");
        }

        static void DeleteFilesInFolder(string folder, string fileType)
        {
            var files = Directory.GetFiles(folder, fileType);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        //
        //
        //
        public static (bool, string) InitializeState()
        {
            Console.WriteLine("Initializing Clear's State");

            // 2020.07.10 CL: It seems that Andi wanted to have the possibility that you can start CLEAR again and it would continue from where it left off.
            // However, since I added a new button that will start fresh with a new analysis, I want to be able to initialize the state with some files initially empty.
            // So need a method to call.
            // 2020.07.10 CL: There seem to be some of these that do not change because of processing through CLEAR. They may change based upon analysis by another program.
            // 2021.03.03 CL: Changed some of functions that read in data so if it doesn't exist, it will just return an empty data structure or null.

            (HashSet<string> puncs,
             HashSet<string> stopWordsTemp,
             HashSet<string> sourceFunctionWords,
             HashSet<string> targetFunctionWordsTemp,
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
                 oldJsonFile,
                 strongFile);

            stopWords = stopWordsTemp;
            targetFunctionWords = targetFunctionWordsTemp;
            manTransModel = manTransModelTemp;
            goodLinks = goodLinksTemp;
            badLinks = badLinksTemp;
            groups = groupsTemp;
            oldLinks = oldLinksTemp;
            strongs = strongsTemp;

            return (true, "Initialized State.");
        }

        //
        // Currently it still uses Tim's ImportTargetVerseCorpusFromLegacy, which assumes a one-to-one relationship between surface text and lemma.
        // We are now allowing a surface word to have zero or more lemmas (i.e. there is no longer a one-to-one relationship between text and lemma).
        // We will need to change the data structure to do this all at the same time.
        // But since we actually want to separate tokenization and lemmatization into two different steps, we will just use the same function to segment the text.
        // The exporting routine is modified not to write the lemma file since that will be done in the lemmatization step.
        public static (bool, string) TokenizeVerses()
        {
            Console.WriteLine("Tokenizing Verses");

            if (reuseTokenFiles && File.Exists(tokenTextFile) && File.Exists(tokenLemmaFile) && File.Exists(tokenTextFile))
            {
                Console.WriteLine("  Reusing token files.");
            }
            else
            {
                Console.WriteLine("  Creating token files.");

                ShowTime();

                targetVerseCorpus =
                    importExportService.ImportTargetVerseCorpusFromLegacy(
                        versesFile,
                        clearService.DefaultSegmenter,
                        puncs,
                        lang,
                        targetCSharpCulture);

                // NOTE: OneToMany - should not create or write tokenLemmaFile at this time. Should just export the tokenTextFile and tokenIdFile
                // targetVerseCorpus is read in when building the basic parallel corpora
                Persistence.ExportTargetVerseCorpus(targetVerseCorpus, tokenTextFile, tokenLemmaFile, tokenTextIdFile);

                ShowTime();
            }

            return (true, "Tokenizing Verses: Done.");
        }

        //
        //
        //
        public static (bool, string) LemmatizeVerses()
        {
            Console.WriteLine("Lemmatizing Verses.");

            if (reuseLemmaFiles && File.Exists(tokenTextIdFile) && File.Exists(tokenLemmaFile) && File.Exists(tokenLemmaIdFile))
            {
                Console.WriteLine("  Reusing lemmatized files.");
            }
            else
            {
                Console.WriteLine("  Creating lemmatized files.");

                ShowTime();

                Lemmas.Lemmatize(tokenTextFile, tokenLemmaFile, tokenLemmaIdFile, lang, lowerCaseMethod, targetCSharpCulture, lemmaDataFile);

                ShowTime();
            }

            return (true, "Lemmatizing Verses: Done.");
        }

        //
        public static (bool, string) CreateParallelCorpus()
        {
            Console.WriteLine("Creating Parallel Corpora");

            string returnMessage = string.Empty;

            // Create basic files
            if (reuseParallelCorporaFiles &&
                File.Exists(sourceLemmaFile) && File.Exists(sourceIdFile) && File.Exists(sourceTextFile) && File.Exists(sourceLemmaCatFile) &&
                File.Exists(targetLemmaFile) && File.Exists(targetLemmaIdFile) && File.Exists(targetTextFile) && File.Exists(targetTextIdFile))
            {
                Console.WriteLine("  Reusing basic parallel corpus files.");

                // I don't think the below is needed
                if (parallelCorpora == null) parallelCorpora = Persistence.ImportParallelCorpus(sourceTextFile, sourceLemmaFile, sourceIdFile, targetTextFile, targetLemmaFile, targetLemmaIdFile);
            }
            else
            {
                Console.WriteLine("  Creating basic parallel corpus files.");

                ShowTime();             

                // Need to use CE3's method of creating the initial parallel corpus since it gets the source data from the trees
                if (targetVerseCorpus == null) targetVerseCorpus = Persistence.ImportTargetVerseCorpus(tokenTextFile, tokenLemmaFile, tokenTextIdFile);
                parallelCorpora = utility.CreateParallelCorpora(targetVerseCorpus, treeService, simpleVersification);
                // Export to files so we can use the same methods as CE2
                Persistence.ExportParallelCorpora(parallelCorpora, sourceTextFile, sourceLemmaFile, sourceIdFile, sourceLemmaCatFile, targetTextFile, targetLemmaFile, targetLemmaIdFile);

                ShowTime();
            }


            if (contentWordsOnlySMT || contentWordsOnlyTC)
            {
                // Create Content Words Only Files
                ParallelCorpus.CreateContentWordsOnlyCorpus(
                        reuseParallelCorporaFiles, sourceFunctionWords, targetFunctionWords,
                        sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile, targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);

                if (useNoPuncModel)
                {
                    // Create Content Words Only and No Punctuation Files
                    ParallelCorpus.CreateNoPuncCorpus(true, reuseParallelCorporaFiles, puncs,
                        sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile, targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);
                }
            }
            else if (useNoPuncModel)
            {
                // Create No Punctionation Files
                ParallelCorpus.CreateNoPuncCorpus(false, reuseParallelCorporaFiles, puncs,
                    sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile, targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);
            }

            return (true, "Creating Parallel Corpora: Done.");
        }

        //
        public static (bool, string) BuildModels()
        {

            Console.WriteLine("Building Models");

            // Train a statistical translation model using the parallel corpora producing an estimated translation model and estimated alignment.
            // There are three possible scenarios for how to use parallel corpus with all words or content only words.
            // In CE2 it converts alignmentModelRest to alignmentModelPre.
            // But in CE3, this conversion is done when creating new assumptions internally so only need to
            // make an assignment, no conversion needed.
            //
            AlignmentModel alignmentModelRest;

            if (contentWordsOnlySMT || contentWordsOnlyTC)
            {
                // Build content only words for TC alignment
                (translationModel, alignmentModel) = BuildModelTools.BuildOrReuseModels(reuseSmtModelFiles, true, useNoPuncModel, useLemmaCatModel, useNormalizedTransModelProbabilities, useNormalizedAlignModelProbabilities,
                                                sourceTextFile, sourceLemmaFile, sourceIdFile, targetTextFile, targetLemmaFile, targetLemmaIdFile,
                                                runSpec, transModelFile, alignModelFile, clearService);

                // SMT take priority over TC. Probably should not use two booleans but a enum.
                if (contentWordsOnlySMT)
                {
                    // use same model as TC for aligning the rest
                    translationModelRest = translationModel;
                    alignmentModelRest = alignmentModel;
                    alignmentModelPre = alignmentModelRest;
                }
            }

            // Build all words models for aligning the rest and for TC

            if (!contentWordsOnlySMT)
            {
                // Build all words for aligning the rest
                (translationModelRest, alignmentModelRest) = BuildModelTools.BuildOrReuseModels(reuseSmtModelFiles, false, useNoPuncModel, useLemmaCatModel, useNormalizedTransModelProbabilities, useNormalizedAlignModelProbabilities,
                                                        sourceTextFile, sourceLemmaFile, sourceIdFile, targetTextFile, targetLemmaFile, targetLemmaIdFile,
                                                        runSpec, transModelFile, alignModelFile, clearService);

                alignmentModelPre = alignmentModelRest;

                if (!contentWordsOnlyTC)
                {
                    translationModel = translationModelRest;
                    alignmentModel = alignmentModelRest;
                }
            }

            return (true, "Building Models: Done");
        }


        //
        //
        //
        public static (bool, string) AutoAlign()
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

            LegacyLemmaPersistentAlignment alignment =
                AutoAlignFromModelsNoGroupsSubTask.RunLemma(
                    zoneAlignmentProblems,
                    treeService,
                    glossTable,
                    assumptions);

            // Export the persistent-format datum to a file.

            string json = JsonConvert.SerializeObject(
                alignment.Lines,
                Formatting.Indented);
            File.WriteAllText(jsonLemmasOutput, json);

            ShowTime();

            Data.ConvertLemmaToWordAlignments(jsonLemmasOutput, jsonOutput);

            ShowTime();

            return (true, "Verse aligned.  Output in " + jsonOutput);
        }

        //
        //
        //
        public static (bool, string) IncrementalUpdate()
        {
            Console.WriteLine("Updating Incrementally. Not implemented yet in Clear3.");
            return (true, "IncrementalUpdate is not implemented.");

        }

        //
        //
        //
        public static (bool, string) GlobalUpdate()
        {
            Console.WriteLine("Updating Globally. Not implemented yet in Clear3. Not implemented yet in Clear3");
            return (true, "GlobalUpdate is not implemented.");
        }

        //
        //
        //
        public static (bool, string) AlignG2TviaM2G()
        {
            Console.WriteLine("Aligning Gateway-to-Translation via Manuscript-to-Gateway. Not implemented yet in Clear3.");
            return (true, "AlignG2TviaM2G is not implemented.");
        }

        //
        //
        //
        public static (bool, string) AlignM2TviaM2G()
        {
            Console.WriteLine("Aligning Manuscript-to-Translation via Manuscript-to-Gateway. Not implemented yet in Clear3.");
            return (true, "AlignM2TviaM2G is not implemented.");
        }

        //
        //
        //
        public static (bool, string) ProcessAll()
        {
            Console.WriteLine("Doing ProcessAll command");

            (var succeeded, var message) = DoCommandList(freshStartCommands);

            if (succeeded)
            {
                // Copy alignment.json to CheckedAlignment.json to pretend we did manual alignment
                // File.Delete(checkedAlignmentsFile);
                // File.Copy(jsonOutput, checkedAlignmentsFile);

                (succeeded, message) = GlobalUpdate(); // Global update
            }

            if (!succeeded) Console.WriteLine(message);

            return (true, "Done Processing.");
        }

        //
        //
        //
        public static (bool, string) FreshStart()
        {
            Console.WriteLine("Doing FreshStart command");

            (var succeeded, var message) = DoCommandList(freshStartCommands);

            if (!succeeded) Console.WriteLine(message);

            return (true, "Done Processing.");
        }

        //
        //
        //
        public static (bool, string) DoStart()
        {
            Console.WriteLine("Doing DoStart command");

            (var succeeded, var message) = DoCommandList(startCommands);

            if (!succeeded) Console.WriteLine(message);

            return (true, "Done Processing.");
        }

        private static void ShowTime()
        {
            DateTime dt = DateTime.Now;
            Console.WriteLine(dt.ToString("G"));
        }

        private static (bool, string) DoCommandList(List<Commands> commands)
        {
            bool succeeded = true;
            string message = string.Empty;

            foreach (var command in commands)
            {
                (succeeded, message) = command();
                if (!succeeded) break;
            }

            return (succeeded, message);
        }

        delegate (bool, string) Commands();

        private static List<Commands> basicCommands = new List<Commands>()
            {
                { DeleteStateFiles },
                { InitializeState },
                { TokenizeVerses },
                { LemmatizeVerses },
                { CreateParallelCorpus },
                { BuildModels },
                { AutoAlign },
                { IncrementalUpdate },
                { GlobalUpdate },
                { AlignG2TviaM2G },
                { AlignM2TviaM2G },
            };

        private static List<Commands> freshStartCommands = new List<Commands>()
            {
                { DeleteStateFiles },
                { InitializeState },
                { TokenizeVerses },
                { LemmatizeVerses },
                { CreateParallelCorpus },
                { BuildModels },
                { AutoAlign },
            };

        private static List<Commands> startCommands = new List<Commands>()
            {
                { InitializeState },
                { TokenizeVerses },
                { LemmatizeVerses },
                { CreateParallelCorpus },
                { BuildModels },
                { AutoAlign },
            };

        // Variable not in Clear2

        private static IClear30ServiceAPI clearService;
        private static IImportExportService importExportService;
        private static IUtility utility;

        private static TargetVerseCorpus targetVerseCorpus;
        private static ITreeService treeService;
        private static ParallelCorpora parallelCorpora;
        // private static ParallelCorpora parallelCorporaCW;
        // private static ParallelCorpora parallelCorporaNoPunc;
        // private static ParallelCorpora parallelCorporaNoPuncCW;

        // Variables that are different in Clear2

        private static TranslationModel translationModel; // translation model
        private static TranslationModel translationModelRest; // translation model for all words
        private static TranslationModel manTransModel; // the translation model created from manually checked alignments
        private static AlignmentModel alignmentModel; // alignment model
        private static AlignmentModel alignmentModelPre; // alignment model
        private static SimpleVersification simpleVersification;
        private static GroupTranslationsTable groups; // one-to-many, many-to-one, and many-to-many mappings

        private static Dictionary<string, Dictionary<string, int>> strongs;
        private static Dictionary<string, Dictionary<string, string>> oldLinks;

        // Variables that are the same as in Clear2

        private static HashSet<string> puncs; // list of punctuation marks
        private static HashSet<string> stopWords; // list of target words not to be linked
        private static HashSet<string> sourceFunctionWords; // function words
        private static HashSet<string> targetFunctionWords;

        private static string python = "python.exe"; // The path to the Python program. Default is that it is in the Clear folder with this name.
        private static string clearConfigFilename = "Clear.config"; // Default top configuration file
        private static Dictionary<string, string> clearSettings; // Input from .config file
        private static string runConfigFilename;
        private static Dictionary<string, string> runSettings; // Input from .config file
        private static string projectFolder;
        private static Dictionary<string, string> projectSettings;
        private static Dictionary<string, string> translationSettings;
        private static Dictionary<string, string> processingSettings; // Input from .config file

        private static string resourcesFolder; // 2020.07.11 CL: The folder where all the CLEAR Engine resources are kept.
        private static string processingFolder; // 2020.07.11 CL: The folder where all the CLEAR Engine translation projects are kept.
        private static string targetFolder; // the folder where the target language data is kept.  Each different translaton has a different folder.

        private static string project; // 2020.09.25 CL: The name of the folder inside the projectsFolder. We may have multiple translations for the same language. This is used to identify the folder used.
        private static string lang; // language of target language
        private static string targetCSharpCulture; // Used in C# routines that consider language and culture, such as .ToLower()
        private static string translation; // 2020.09.25 CL: This is now the name of the translation, sometimes including a numbers to indicate the year of the copyright, e.g. NIV84
        private static string testament; // 2020.07.11 CL: Which testament we are working on. I think at some point, we should not need to distinguish between them
        private static string testamentFoldername; // 2021.07.21 CL: Added the ability to use a name for the testament folder other than "OT" and "NT".

        private static string lowerCaseMethod;

        private static string lemmaDataFilename;
        private static string lemmaDataFile;

        private static string runSpec; // <implementation>-<model>-<iterations>-<epsilon>-<heuristic>
        private static string smtImplementation; // The implementation of the SMT model we want to use
        private static string smtModel; // The SMT model we want to use
        private static string smtIterations; // The number of interations we want to use
        private static string smtEpsilon; // Must exceed this (threshold) to be included in the translation model, 0.1
        private static string smtHeuristic; // The heuristic used for alignments

        private static bool useAlignModel; // whether to use the alignment model
        private static bool contentWordsOnly; // whether to align content words
        private static bool contentWordsOnlySMT; // Use only content words for creating the statistical models: transModel and alignModel
        private static bool contentWordsOnlyTC; // Use only content words for creating the statistical translation model: transModel
        private static bool useLemmaCatModel; // whether to use the lemma_cat in creating the SMT models
        private static bool useNoPuncModel; // whether to use the target corpora without punctuations in creating the SMT models
        private static bool useNormalizedTransModelProbabilities; // whether to use normalized probilities for the translation model from the SMT
        private static bool useNormalizedAlignModelProbabilities; // whether to use normalized probilities for the alignment model from the
                                                                  //
        private static bool useMorphology; // whether to use morphology when created lemmatized tokens corpora

        private static bool reuseTokenFiles; // If token files already exist, use them rather than creating them over again.
        private static bool reuseLemmaFiles; // If token files already exist, use them rather than creating them over again.
        private static bool reuseParallelCorporaFiles; // If parallel corpora files already exist, use them rather than creating them over again.
        private static bool reuseSmtModelFiles; // If SMT model files already exist, use them rather than creating them over again.

        private static string strUseAlignModel;
        private static string strContentWordsOnly;
        private static string strContentWordsOnlySMT;
        private static string strContentWordsOnlyTC;
        private static string strUseLemmaCatModel;
        private static string strUseNoPuncModel;
        private static string strUseNormalizedTransModelProbabilities;
        private static string strUseNormalizedAlignModelProbabilities;

        private static string strUseMorphology;

        private static string strReuseTokenFiles;
        private static string strReuseLemmaFiles;
        private static string strReuseParallelCorporaFiles;
        private static string strReuseSmtModelFiles;

        private static string translationTestamentPrefix;

        private static string tokensFoldername;
        private static string tokensFolder;
        private static string corporaFoldername;
        private static string corporaFolder;
        private static string modelsFoldername;
        private static string modelsFolder;
        private static string lexiconFoldername;
        private static string lexiconFolder;

        private static string transModelFilename; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts
        private static string transModelFile; // the file that contains the translation model; to be loaded into the transModel Hashtable when the system starts

        private static string manTransModelFilename; // the file that contains the translation model created from manually checked alignments
        private static string manTransModelFile; // the file that contains the translation model created from manually checked alignments

        private static string alignModelFilename; // the file that contains the alignment model;
        private static string alignModelFile; // the file that contains the alignment model;

        private static string glossFilename; // 2020.07.11 CL: The file where the source glosses are kept.
        private static string glossFile; // 2020.07.11 CL: The file where the source glosses are kept.
        private static Dictionary<string, Gloss> glossTable; // Word glosses

        private static string puncsFilename; // 2020.07.11 CL: The file where the source punctuations kept.
        private static string puncsFile; // 2020.07.11 CL: The file where the source punctuations kept.

        private static string versificationFilename; // versification file
        private static string versificationFile; // versification file
        private static string versificationType; // versification type

        private static string jsonOutputFilename; // output of aligner in JSON
        private static string jsonOutputBase; // base file output of aligner in JSON
        private static string jsonOutput; // output of aligner in JSON
        private static string jsonLemmasOutputFilename; // output of aligner in JSON with target sub-lemmas and surface text
        private static string jsonLemmasOutputBase; // base file output of aligner in JSON with target sub-lemmas and surface text
        private static string jsonLemmasOutput; // output of aligner in JSON with target sub-lemmas and surface text
        private static string jsonFilename; // output of aligner in JSON
        private static string jsonFile; // output of aligner in JSON

        private static string groupFilename; // the file that contains the one-to-many, many-to-one, and many-to-many mappings
        private static string groupFile; // the file that contains the one-to-many, many-to-one, and many-to-many mappings

        private static string stopWordFilename; // contains the list of target words not to be linked
        private static string stopWordFile; // contains the list of target words not to be linked

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

        private static string sourceFuncWordsFilename;
        private static string sourceFuncWordsFile;

        private static string targetFuncWordsFilename; // 2020.07.10 CL: Added this to make it global to this form since targetFuncWords is global.
        private static string targetFuncWordsFile; // 2020.07.10 CL: Added this to make it global to this form since targetFuncWords is global.

        private static string strongFilename;
        private static string strongFile;

        private static string oldJsonFilename;
        private static string oldJsonFile;

        private static string tokenTextFilename;
        private static string tokenTextFile;
        private static string tokenTextIdFilename;
        private static string tokenTextIdFile;
        private static string tokenLemmaFilename;
        private static string tokenLemmaFile;
        private static string tokenLemmaIdFilename;
        private static string tokenLemmaIdFile;

        private static string sourceTextFilename;
        private static string sourceTextFile;
        private static string sourceIdFilename;
        private static string sourceIdFile;
        private static string sourceLemmaFilename;
        private static string sourceLemmaFile;
        private static string sourceLemmaCatFilename;
        private static string sourceLemmaCatFile;

        private static string targetTextFilename;
        private static string targetTextFile;
        private static string targetTextIdFilename;
        private static string targetTextIdFile;
        private static string targetLemmaFilename;
        private static string targetLemmaFile;
        private static string targetLemmaIdFilename;
        private static string targetLemmaIdFile;

        private static string versesFilename; // Input file with verses on separate lines previxed with verseID.
        private static string versesFile; // Input file with verses on separate lines previxed with verseID.
    }
}