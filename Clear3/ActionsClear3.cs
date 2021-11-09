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

        public static void SetRunSpec(string arg)
        {
            runSpec = arg;
        }

        public static void SetEpsilon(string arg)
        {
            strEpsilon = arg;
        }

        public static void SetSmtModel(string arg)
        {
            smtModel = arg;
        }

        public static void SetSmtHeuristic(string arg)
        {
            smtHeuristic = arg;
        }

        public static void SetSmtIterations(string arg)
        {
            smtIterations = arg;
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
        // They can be initialized from the configuration file, and different ones can be used (by manual copying of files) or through a command line option.
        // Once loaded in at startup, they can be changed by command line options.
        // This was added to avoid having to change code because you want to use different files.
        // This was done by using Forms and textBoxes, but only for some of the configuration. Others were embedded in the code.
        private static void ReadConfig(string configFilename)
        {
            clearSettings = Configuration.GetSettings(configFilename);

            resourcesFolder = clearSettings["ResourcesFolder"]; // e.g. 
            processingFolder = clearSettings["ProcessingFolder"]; // e.g. 

            runConfigFilename = clearSettings["Run_Configuration_Filename"];
            runSettings = Configuration.GetSettings(runConfigFilename);

            // Run and some processing settings can be overridden by command line parameters
            project = (string)runSettings["Project"]; // e.g. "NIV84-SIL-test"
            testament = (string)runSettings["Testament"]; // e.g. "OT" or "NT"

            // Set file information in resourcesFolder

            // treeFoldername = clearSettings["TreeFolder"];
            sourceFuncWordsFilename = clearSettings["Source_Function_Words_Filename"];
            puncsFilename = clearSettings["Punctuations_Filename"];
            glossFilename = clearSettings["Gloss_Filename"];

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

            //============================ Output Files Only ============================
            // Files not part of the state, nor used as output/input to pass data between different functions
            // Output file. Has the alignment in .json format, which is more readable than XML format.
            jsonOutputFilename = clearSettings["Json_Output_Filename"]; // e.g "alignments.json", Should update variable to ...File
            jsonLemmasOutputFilename = clearSettings["Json_Lemmas_Output_Filename"]; // e.g "alignment.json", Should update variable to ...File
            jsonFilename = clearSettings["Json_Filename"]; // e.g "alignment.json", Should merge with jsonOutput

            //============================ Input Files Only ============================
            versesFilename = clearSettings["Verses_Filename"]; // e.g. "Verses.txt"

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
            // sourceFolder = Path.Combine(resourcesFolder, sourceFoldername); // e.g. "Manuscript",folder with the original language files.
            // treeFolder = Path.Combine(resourcesFolder, treeFoldername); // e.g. "Trees", folder with manuscript trees. Fixed. Doesn't change. Input to CLEAR, Andi's own XML format
            sourceFuncWordsFile = Path.Combine(resourcesFolder, sourceFuncWordsFilename); // e.g. "sourceFuncwords.txt"
            puncsFile = Path.Combine(resourcesFolder, puncsFilename); // e.g. "puncs.txt"
            glossFile = Path.Combine(resourcesFolder, glossFilename); // e.g. "Gloss.tsv"

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

            jsonOutput = Path.Combine(targetFolder, translationTestamentPrefix + jsonOutputFilename);
            jsonLemmasOutput = Path.Combine(targetFolder, translationTestamentPrefix + jsonLemmasOutputFilename);
            jsonFile = Path.Combine(targetFolder, translationTestamentPrefix + jsonFilename);

            //============================ Input Files Only ============================
            versesFile = Path.Combine(targetFolder, translationTestamentPrefix + versesFilename);

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
            if (runSpec == null) runSpec = processingSettings["RunSpec"]; // e.g. 1:10;H:5, Machine;FastAlign, Machine;FastAlign:Intersection:7
            if (strEpsilon == null) strEpsilon = processingSettings["Epsilon"]; // Must exceed this to be counted into model, e.g. "0.1"
            if (smtModel == null) smtModel = processingSettings["SmtModel"];
            if (smtHeuristic == null) smtHeuristic = processingSettings["SmtHeuristic"];
            if (smtIterations == null) smtIterations = processingSettings["SmtIterations"];
            if (strContentWordsOnlySMT == null) strContentWordsOnlySMT = processingSettings["ContentWordsOnlySMT"]; // e.g. "true" Only use content words for building models
            if (strContentWordsOnlyTC == null) strContentWordsOnlyTC = processingSettings["ContentWordsOnlyTC"]; // e.g. "true" Only use content words for building models
            if (strContentWordsOnly == null) strContentWordsOnly = processingSettings["ContentWordsOnly"]; // e.g. "true" Only align content words

            if (strUseAlignModel == null) strUseAlignModel = processingSettings["UseAlignModel"]; // e.g. "true"
            if (strUseLemmaCatModel == null) strUseLemmaCatModel = processingSettings["UseLemmaCatModel"]; // e.g. "true"
            if (strUseNoPuncModel == null) strUseNoPuncModel = processingSettings["UseNoPuncModel"]; // e.g. "
            if (strUseNormalizedTransModelProbabilities == null) strUseNormalizedTransModelProbabilities = processingSettings["UseNoNormalizedTransModelProbabilities"]; // e.g. "true"
            if (strUseNormalizedAlignModelProbabilities == null) strUseNormalizedAlignModelProbabilities = processingSettings["UseNoNormalizedAlignModelProbabilities"]; // e.g. "true"

            if (strReuseTokenFiles == null) strReuseTokenFiles = processingSettings["ReuseTokenFiles"]; // e.g. "true"
            if (strReuseLemmaFiles == null) strReuseLemmaFiles = processingSettings["ReuseLemmatizedFiles"]; // e.g. "true"
            if (strReuseParallelCorporaFiles == null) strReuseParallelCorporaFiles = processingSettings["ReuseParallelCorporaFiles"]; // e.g. "true"
            if (strReuseSmtModelFiles == null) strReuseSmtModelFiles = processingSettings["ReuseSmtModelFiles"]; // e.g. "true"

            if (strBadLinkMinCount == null) strBadLinkMinCount = processingSettings["BadLinkMinCount"]; // e.g. "3", the minimal count required for treating a link as bad
            if (strGoodLinkMinCount == null) strGoodLinkMinCount = processingSettings["GoodLinkMinCount"]; // e.g. "3" the minimal count required for treating a link as bad

            // Convert strings parameters to values

            epsilon = double.Parse(strEpsilon); // Must exceed this to be counted into model, e.g. "0.1"
            contentWordsOnlySMT = (strContentWordsOnlySMT == "true"); // e.g. "true" Only use content words for building models
            contentWordsOnlyTC = (strContentWordsOnlyTC == "true"); // e.g. "true" Only use content words for finding terminal candidates
            contentWordsOnly = (strContentWordsOnly == "true"); // e.g. "true" Only align content words

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

            string autoType = "_auto.json";
            jsonOutput = jsonOutput.Replace(".json", autoType); // Want to distinguish these from gold standard alignments
            jsonLemmasOutput = jsonLemmasOutput.Replace(".json", autoType); // Want to distinguish these from gold standard alignments

            string alignmentType = "_all.json";
            if (contentWordsOnly)
            {
                alignmentType = "_content.json";
            }
            jsonOutput = jsonOutput.Replace(".json", alignmentType);
            jsonLemmasOutput = jsonLemmasOutput.Replace(".json", alignmentType);
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
        }


        // Was Do_Button10()
        public static string DeleteStateFiles()
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
            File.Delete(manTransModelFile);

            return ("Deleted State Files.");
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
        public static string InitializeState()
        {
            Console.WriteLine("Initializing Clear's State");

            // 2020.07.10 CL: It seems that Andi wanted to have the possibility that you can start CLEAR again and it would continue from where it left off.
            // However, since I added a new button that will start fresh with a new analysis, I want to be able to initialize the state with some files initially empty.
            // So need a method to call.
            // 2020.07.10 CL: There seem to be some of these that do not change because of processing through CLEAR. They may change based upon analysis by another program.
            // 2021.03.03 CL: Changed some of functions that read in data so if it doesn't exist, it will just return an empty data structure or null.

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

            return ("Initialized State.");
        }

        //
        // Currently it still uses Tim's ImportTargetVerseCorpusFromLegacy, which assumes a one-to-one relationship between surface text and lemma.
        // We are now allowing a surface word to have zero or more lemmas (i.e. there is no longer a one-to-one relationship between text and lemma).
        // We will need to change the data structure to do this all at the same time.
        // But since we actually want to separate tokenization and lemmatization into two different steps, we will just use the same function to segment the text.
        // The exporting routine is modified not to write the lemma file since that will be done in the lemmatization step.
        public static string TokenizeVerses()
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

            return "Tokenizing Verses: Done.";
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
        public static string CreateParallelCorpus()
        {
            Console.WriteLine("Creating Parallel Corpora");

            ShowTime();

            string returnMessage = string.Empty;

            // Create basic files
            if (reuseParallelCorporaFiles &&
                File.Exists(sourceLemmaFile) && File.Exists(sourceIdFile) && File.Exists(sourceTextFile) && File.Exists(sourceLemmaCatFile) &&
                File.Exists(targetLemmaFile) && File.Exists(targetIdFile) && File.Exists(targetTextFile))
            {
                Console.WriteLine("  Reusing basic parallel corpus files.");

                if (parallelCorpora == null) parallelCorpora = Persistence.ImportParallelCorpus(sourceTextFile, sourceLemmaFile, sourceIdFile, targetTextFile, targetLemmaFile, targetIdFile);
            }
            else
            {
                Console.WriteLine("  Creating basic parallel corpus files.");

                if (targetVerseCorpus == null) targetVerseCorpus = Persistence.ImportTargetVerseCorpus(tokenTextFile, tokenLemmaFile, tokenTextIdFile);
                parallelCorpora = utility.CreateParallelCorpora(targetVerseCorpus, treeService, simpleVersification);
                Persistence.ExportParallelCorpora(parallelCorpora, sourceTextFile, sourceLemmaFile, sourceIdFile, sourceLemmaCatFile, targetTextFile, targetLemmaFile, targetIdFile);
            }

            // Create Content Words Only Files
            if (contentWordsOnlySMT || contentWordsOnlyTC)
            {
                (string sourceLemmaFileCW, string sourceIdFileCW, string sourceTextFileCW, string sourceLemmaCatFileCW,
                    string targetLemmaFileCW, string targetIdFileCW, string targetTextFileCW) = InitializeCreateParallelCorporaFiles(false, true);

                if (reuseParallelCorporaFiles &&
                    File.Exists(sourceLemmaFileCW) && File.Exists(sourceIdFileCW) && File.Exists(sourceTextFileCW) && File.Exists(sourceLemmaCatFileCW) &&
                    File.Exists(targetLemmaFileCW) && File.Exists(targetIdFileCW) && File.Exists(targetTextFileCW))
                {
                    Console.WriteLine("  Reusing content words only parallel corpus files.");   
                }
                else
                {
                    Console.WriteLine("  Creating content words only parallel corpus files.");

                    parallelCorporaCW = utility.FilterWordsFromParallelCorpora(parallelCorpora, sourceFunctionWords, targetFunctionWords);
                    Persistence.ExportParallelCorpora(parallelCorporaCW, sourceTextFileCW, sourceLemmaFileCW, sourceIdFileCW, sourceLemmaCatFileCW, targetTextFileCW, targetLemmaFileCW, targetIdFileCW);
                }

                if (useNoPuncModel)
                {
                    (string sourceLemmaNoPuncFileCW, string sourceIdNoPuncFileCW, string sourceTextNoPuncFileCW, string sourceLemmaCatNoPuncFileCW,
                        string targetLemmaNoPuncFileCW, string targetIdNoPuncFileCW, string targetTextNoPuncFileCW) = InitializeCreateParallelCorporaFiles(true, true);

                    // Create Content Words Only and No Punctuation Files
                    if (reuseParallelCorporaFiles &&
                        File.Exists(sourceLemmaNoPuncFileCW) && File.Exists(sourceIdNoPuncFileCW) && File.Exists(sourceTextNoPuncFileCW) && File.Exists(sourceLemmaCatNoPuncFileCW) &&
                        File.Exists(targetLemmaNoPuncFileCW) && File.Exists(targetIdNoPuncFileCW) && File.Exists(targetTextNoPuncFileCW))
                    {
                        Console.WriteLine("  Reusing content words only and no punctuation parallel corpus files files.");

                        parallelCorporaNoPuncCW = Persistence.ImportParallelCorpus(sourceTextNoPuncFileCW, sourceLemmaNoPuncFileCW, sourceIdNoPuncFileCW, targetTextNoPuncFileCW, targetLemmaNoPuncFileCW, targetIdNoPuncFileCW);
                    }
                    else
                    {
                        Console.WriteLine("  Creating content words only and no punctuation parallel corpus files.");

                        if (parallelCorporaCW == null) parallelCorporaCW = Persistence.ImportParallelCorpus(sourceTextFileCW, sourceLemmaFileCW, sourceIdFileCW, targetTextFileCW, targetLemmaFileCW, targetIdFileCW);
                        parallelCorporaNoPuncCW = utility.FilterWordsFromParallelCorpora(parallelCorporaCW, puncs, puncs);
                        Persistence.ExportParallelCorpora(parallelCorporaNoPuncCW, sourceTextNoPuncFileCW, sourceLemmaNoPuncFileCW, sourceIdNoPuncFileCW, sourceLemmaCatNoPuncFileCW, targetTextNoPuncFileCW, targetLemmaNoPuncFileCW, targetIdNoPuncFileCW);
                    }
                }
            }

            if (useNoPuncModel)
            {
                (string sourceLemmaNoPuncFile, string sourceIdNoPuncFile, string sourceTextNoPuncFile, string sourceLemmaCatNoPuncFile,
                    string targetLemmaNoPuncFile, string targetIdNoPuncFile, string targetTextNoPuncFile) = InitializeCreateParallelCorporaFiles(true, false);

                // Create No Punctuation Files
                if (reuseParallelCorporaFiles &&
                    File.Exists(sourceLemmaNoPuncFile) && File.Exists(sourceIdNoPuncFile) && File.Exists(sourceTextNoPuncFile) && File.Exists(sourceLemmaCatNoPuncFile) &&
                    File.Exists(targetLemmaNoPuncFile) && File.Exists(targetIdNoPuncFile) && File.Exists(targetTextNoPuncFile))
                {
                    Console.WriteLine("  Reusing no punctuation parallel corpus files.");
                }
                else
                {
                    Console.WriteLine("  Creating no punctuation parallel corpus files.");

                    parallelCorporaNoPunc = utility.FilterWordsFromParallelCorpora(parallelCorpora, puncs, puncs);
                    Persistence.ExportParallelCorpora(parallelCorporaNoPunc, sourceTextNoPuncFile, sourceLemmaNoPuncFile, sourceIdNoPuncFile, sourceLemmaCatNoPuncFile, targetTextNoPuncFile, targetLemmaNoPuncFile, targetIdNoPuncFile);
                }
            }

            ShowTime();

            return "Creating Parallel Corpora: Done.";
        }

        //
        public static string BuildModels()
        {
            Console.WriteLine("Building Models");

            string returnMessage = string.Empty;

            // Create a new runSpec if thotModel is specified, otherwise, use exisiting runSpec set by command line or processing.config.

            if (smtModel == "HMM")
            {
                runSpec = "HMM;1:10;H:5";
            }
            else if (smtModel != "")
            {
                runSpec = string.Format("{0};{1};{2}", smtModel, smtHeuristic, smtIterations);
            }

            // Train a statistical translation model using the parallel corpora producing an estimated translation model and estimated alignment.
            // There are three possible scenarios for how to use parallel corpus with all words or content only words.
            // Within the SMTService.DefaultSMT, it writes and reads from the file, so any double differences is already done.
            // No need to read them in again.
            if (contentWordsOnlySMT)
            {
                (string smtTransModelFileCW, string smtAlignModelFileCW) = InitializeSmtModelFiles(true);

                if (reuseSmtModelFiles &&
                    File.Exists(smtTransModelFileCW) && File.Exists(smtAlignModelFileCW))
                {
                    Console.WriteLine("  Reusing (Content Word Only) SMT Model Files.");
                    returnMessage = "Building Models: Done.";

                    ShowTime();

                    translationModel = Persistence.ImportTranslationModel(smtTransModelFileCW);
                    alignmentModel = Persistence.ImportAlignmentModel(smtAlignModelFileCW);

                    ShowTime();
                }
                else
                {
                    Console.WriteLine("  Building Models (Content Words Only).");
                    returnMessage = string.Format("Built Models: {0}  {1}", smtTransModelFileCW, smtAlignModelFileCW);

                    ShowTime();

                    ParallelCorpora smtParallelCorporaCW = InitializeParallelCorpora(true);
                    (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorporaCW, runSpec, epsilon);

                    ShowTime();

                    Persistence.ExportTranslationModel(translationModel, smtTransModelFileCW);
                    Persistence.ExportAlignmentModel(alignmentModel, smtAlignModelFileCW);

                    ShowTime();
                }

                translationModelRest = translationModel;
                alignmentModelPre = alignmentModel;

                return returnMessage;
            }
            else if (contentWordsOnlyTC)
            {
                (string smtTransModelFileCW, string smtAlignModelFileCW) = InitializeSmtModelFiles(true);

                if (reuseSmtModelFiles &&
                    File.Exists(smtTransModelFileCW) && File.Exists(smtAlignModelFileCW))
                {
                    Console.WriteLine("  Reusing (Content Word Only) SMT Model Files for Finding Terminal Candidates.");

                    ShowTime();

                    translationModel = Persistence.ImportTranslationModel(smtTransModelFileCW);
                    alignmentModel = Persistence.ImportAlignmentModel(smtAlignModelFileCW);

                    ShowTime();
                }
                else
                {
                    Console.WriteLine("  Building Models (Content Words Only) for Finding Terminal Candidates.");
                    returnMessage += string.Format(" {0} {1}", smtTransModelFileCW, smtAlignModelFileCW);

                    ShowTime();

                    ParallelCorpora smtParallelCorporaCW = InitializeParallelCorpora(true);
                    (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorporaCW, runSpec, epsilon);

                    ShowTime();

                    Persistence.ExportTranslationModel(translationModel, smtTransModelFileCW);
                    Persistence.ExportAlignmentModel(alignmentModel, smtAlignModelFileCW);

                    ShowTime();
                }

                (string smtTransModelFile, string smtAlignModelFile) = InitializeSmtModelFiles(false);

                if (reuseSmtModelFiles &&
                    File.Exists(smtTransModelFile) && File.Exists(smtAlignModelFile))
                {
                    Console.WriteLine("  Reusing (All Words) SMT Model Files for Aligning the Rest.");

                    ShowTime();

                    translationModelRest = Persistence.ImportTranslationModel(smtTransModelFile);
                    alignmentModelPre = Persistence.ImportAlignmentModel(smtAlignModelFile);

                    ShowTime();
                }
                else
                {
                    Console.WriteLine("  Building Models (All Words) for Aligning the Rest.");
                    returnMessage += string.Format(" {0} {1}", smtTransModelFile, smtAlignModelFile);

                    ShowTime();

                    ParallelCorpora smtParallelCorpora = InitializeParallelCorpora(false);
                    (translationModelRest, alignmentModelPre) = clearService.SMTService.DefaultSMT(smtParallelCorpora, runSpec, epsilon);

                    ShowTime();

                    Persistence.ExportTranslationModel(translationModelRest, smtTransModelFile);
                    Persistence.ExportAlignmentModel(alignmentModelPre, smtAlignModelFile);

                    ShowTime();
                }

                if (returnMessage == string.Empty)
                {
                    returnMessage = "Building Models: Done.";
                }
                else
                {
                    returnMessage = "Built Models:" + returnMessage;
                }

                return returnMessage;
            }
            else
            {
                (string smtTransModelFile, string smtAlignModelFile) = InitializeSmtModelFiles(false);

                if (reuseSmtModelFiles &&
                    File.Exists(smtTransModelFile) && File.Exists(smtAlignModelFile))
                {
                    Console.WriteLine("  Reusing (All Words) SMT Model Files.");
                    returnMessage = "Building Models: Done.";

                    ShowTime();

                    translationModel = Persistence.ImportTranslationModel(smtTransModelFile);
                    alignmentModel = Persistence.ImportAlignmentModel(smtAlignModelFile);

                    ShowTime();
                }
                else
                {
                    Console.WriteLine("  Building Models (All Words).");
                    returnMessage += string.Format("Built Models: {0} {1}", smtTransModelFile, smtAlignModelFile);

                    ParallelCorpora smtParallelCorpora = InitializeParallelCorpora(false);
                    (translationModel, alignmentModel) = clearService.SMTService.DefaultSMT(smtParallelCorpora, runSpec, epsilon);

                    ShowTime();

                    Persistence.ExportTranslationModel(translationModel, smtTransModelFile);
                    Persistence.ExportAlignmentModel(alignmentModel, smtAlignModelFile);

                    ShowTime();
                }
                    
                translationModelRest = translationModel;
                alignmentModelPre = alignmentModel;

                return returnMessage;
            }
        }

        // returns the two sets (all words and content words only) parallel corpora based upon different settings for building models
        private static ParallelCorpora InitializeParallelCorpora(bool useContentWordsOnly)
        {
            (string smtSourceTextFile, string smtSourceLemmaFile, string smtSourceIdFile, string smtTargetTextFile, string smtTargetLemmaFile, string smtTargetIdFile) = InitializeParallelCorporaFilesForSMT(useContentWordsOnly);

            ParallelCorpora smtParallelCorpora = Persistence.ImportParallelCorpus(smtSourceTextFile, smtSourceLemmaFile, smtSourceIdFile, smtTargetTextFile, smtTargetLemmaFile, smtTargetIdFile);

            return smtParallelCorpora;
        }

        // returns the two sets (all words and content words only) files based upon different settings for building models
        private static (string, string, string, string, string, string) InitializeParallelCorporaFilesForSMT(bool useContentWordsOnly)
        {
            string suffix = CreateCorporaSuffix(useLemmaCatModel, useNoPuncModel, useContentWordsOnly);
            string filteredSuffix = CreateCorporaFilteredSuffix(useNoPuncModel, useContentWordsOnly);

            string smtSourceTextFile = sourceTextFile.Replace(".source", suffix + ".source");
            string smtSourceLemmaFile = sourceLemmaFile.Replace(".source", suffix + ".source");
            string smtSourceIdFile = sourceIdFile.Replace(".source", filteredSuffix + ".source");

            string smtTargetTextFile = targetTextFile.Replace(".target", filteredSuffix + ".target");
            string smtTargetLemmaFile = targetLemmaFile.Replace(".target", filteredSuffix + ".target");
            string smtTargetIdFile = targetIdFile.Replace(".target", filteredSuffix + ".target");

            return (smtSourceTextFile, smtSourceLemmaFile, smtSourceIdFile, smtTargetTextFile, smtTargetLemmaFile, smtTargetIdFile);
        }

        // returns the two sets (all words and content words only) files based upon different settings for building models
        // produce sourceLemma, sourceId, sourceText, sourceLemmaCat, targetLemma, targetId, targetText files
        private static (string, string, string, string, string, string, string) InitializeCreateParallelCorporaFiles(bool useNoPunc, bool useContentWordsOnly)
        {
            string filteredSuffix = CreateCorporaFilteredSuffix(useNoPunc, useContentWordsOnly);

            // Default parallel corpus files

            string corporaSourceLemmaFile = sourceLemmaFile.Replace(".source", filteredSuffix + ".source");
            string corporaSourceIdFile = sourceIdFile.Replace(".source", filteredSuffix + ".source");
            string corporaSourceTextFile = sourceTextFile.Replace(".source", filteredSuffix + ".source");
            string corporaSourceLemmaCatFile = sourceLemmaCatFile.Replace(".source", filteredSuffix + ".source");

            string corporaTargetLemmaFile = targetLemmaFile.Replace(".target", filteredSuffix + ".target");
            string corporaTargetIdFile = targetIdFile.Replace(".target", filteredSuffix + ".target");
            string corporaTargetTextFile = targetTextFile.Replace(".target", filteredSuffix + ".target");

            return (
                corporaSourceLemmaFile, corporaSourceIdFile, corporaSourceTextFile, corporaSourceLemmaCatFile,
                corporaTargetLemmaFile, corporaTargetIdFile, corporaTargetTextFile
                );
        }

        //
        private static (string, string) InitializeSmtModelFiles(bool useContentWordsOnly)
        {
            string suffix = CreateCorporaSuffix(useLemmaCatModel, useNoPuncModel, useContentWordsOnly);
            string modelSuffix = string.Format(".{0}.lemma{1}", smtModel.ToLower(), suffix);

            string smtTransModelFile = transModelFile.Replace(".tsv", modelSuffix + ".tsv");
            string smtAlignModelFile = alignModelFile.Replace(".tsv", modelSuffix + ".tsv");

            return (smtTransModelFile, smtAlignModelFile);
        }

        private static string CreateCorporaSuffix(bool useLemmaCat, bool useNoPunc, bool useContentWordsOnly)
        {
            string suffix = string.Empty;

            if (useLemmaCat) suffix += ".cat";
            if (useNoPunc) suffix += ".nopunc";
            if (useContentWordsOnly) suffix += ".cw";

            return suffix;
        }

        private static string CreateCorporaFilteredSuffix(bool useNoPunc, bool useContentWordsOnly)
        {
            string suffix = string.Empty;

            if (useNoPunc) suffix += ".nopunc";
            if (useContentWordsOnly) suffix += ".cw";

            return suffix;
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
            Console.WriteLine("ProcessAll command not implemented");

            /*
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
            */

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

        // Variable not in Clear2

        private static IClear30ServiceAPI clearService;
        private static IImportExportService importExportService;
        private static IUtility utility;

        private static TargetVerseCorpus targetVerseCorpus;
        private static ITreeService treeService;
        private static ParallelCorpora parallelCorpora;
        private static ParallelCorpora parallelCorporaCW;
        private static ParallelCorpora parallelCorporaNoPunc;
        private static ParallelCorpora parallelCorporaNoPuncCW;

        // Variables that are different in Clear2

        private static TranslationModel translationModel; // translation model
        private static TranslationModel translationModelRest; // translation model for all words
        private static TranslationModel manTransModel; // the translation model created from manually checked alignments
        private static AlignmentModel alignmentModel; // alignment model
        private static AlignmentModel alignmentModelPre; // alignment model
        private static List<string> puncs; // list of punctuation marks
        private static SimpleVersification simpleVersification;
        private static GroupTranslationsTable groups; // one-to-many, many-to-one, and many-to-many mappings
        private static List<string> stopWords; // list of target words not to be linked
        private static List<string> sourceFunctionWords; // function words
        private static List<string> targetFunctionWords;
        private static Dictionary<string, Dictionary<string, int>> strongs;
        private static Dictionary<string, Dictionary<string, string>> oldLinks;

        // Variables that are the same as in Clear2

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

        private static string runSpec; // 1:10;H:5
        private static double epsilon; // Must exceed this to be counted into model, 0.1
        private static string smtModel; // For SIL auto aligner interations
        private static string smtHeuristic; // For SIL auto aligner interations
        private static string smtIterations; // For SIL auto aligner interations

        private static bool useAlignModel; // whether to use the alignment model
        private static bool contentWordsOnly; // whether to align content words
        private static bool contentWordsOnlySMT; // Use only content words for creating the statistical models: transModel and alignModel
        private static bool contentWordsOnlyTC; // Use only content words for creating the statistical translation model: transModel
        private static bool useLemmaCatModel; // whether to use the lemma_cat in creating the SMT models
        private static bool useNoPuncModel; // whether to use the target corpora without punctuations in creating the SMT models
        private static bool useNormalizedTransModelProbabilities; // whether to use normalized probilities for the translation model from the SMT
        private static bool useNormalizedAlignModelProbabilities; // whether to use normalized probilities for the alignment model from the SMT

        private static bool reuseTokenFiles; // If token files already exist, use them rather than creating them over again.
        private static bool reuseLemmaFiles; // If token files already exist, use them rather than creating them over again.
        private static bool reuseParallelCorporaFiles; // If parallel corpora files already exist, use them rather than creating them over again.
        private static bool reuseSmtModelFiles; // If SMT model files already exist, use them rather than creating them over again.

        private static string strEpsilon;
        private static string strUseAlignModel;
        private static string strContentWordsOnly;
        private static string strContentWordsOnlySMT;
        private static string strContentWordsOnlyTC;
        private static string strUseLemmaCatModel;
        private static string strUseNoPuncModel;
        private static string strUseNormalizedTransModelProbabilities;
        private static string strUseNormalizedAlignModelProbabilities;

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
        private static string jsonOutput; // output of aligner in JSON
        private static string jsonLemmasOutputFilename; // output of aligner in JSON with target sub-lemmas and surface text
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