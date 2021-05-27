using System;
using System.Collections.Generic;

namespace Clear3
{
    public enum Command
    {
        DetleteStateFiles, // Delete existing state files (for a fresh start)
        InitializeState, // Initialize the State
        TokenizeVerses, // Tokenize a verse text file
        CreateParallelFiles, // Create Parallel Files
        BuildModels, // Build Translation Model with these files and specs
        AutoAlign,
        IncrementalUpdate,
        GlobalUpdate, // Rebuild the model with these 3 files:
        AlignToGateway, // Align to the gateway translation，using (1) target translation, (2) alignment between manuscript and gateway
        ManuscriptToTarget, // Create manuscript-to-target alignment through gateway-to-target alignment
        ProcessAll, // Process from Copy the initial files (empty) to Create manTransModel and TM
        FreshStart, // Process from Copy the initial files (empty) to AutoAlign
        DoStart, // Process from Initialize State to AutoAlign
        Menu,
        Help,
    }
    public enum Options
    {
        SetProject, 
        SetTestament,
        SetContentWordsOnly,
        SetUseAlignModel,
        SetRunSpec,
        SetEpsilon,
        SetThotModel,
        SetThotHeuristic,
        SetThotIterations,
        SetSmtContentWordsOnly,
        SetTcContentWordsOnly,
        SetUseLemmaCatModel,
        SetUseNoPuncModel,
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ProcessHelp();
            }
            else
            {
                LoadTables();
                if (CommandLineIsGood(args))
                {
                    ActionsClear3.InitializeConfig();
                    ProcessArgs(args);
                }
            }            
        }

        static bool CommandLineIsGood(string[] args)
        {
            bool cmdLineError = false;

            if ((args.Length != 1) || !singleCommands.ContainsKey(args[0].ToLower()))
            {
                for (int i = 0; (!cmdLineError && (i < args.Length)); i++)
                {
                    string arg = args[i];

                    if ((arg.Length > 2) && (arg.Substring(0, 2) == "--"))
                    {
                        string[] parts = arg.Split('=');
                        if (!optionCommands.ContainsKey(parts[0].ToLower()))
                        {
                            Console.WriteLine(string.Format("Invalid Option: {0}", parts[0]));
                            cmdLineError = true;
                        }
                        else if ((parts.Length != 2) || (parts[1] == ""))
                        {
                            Console.WriteLine(string.Format("Error: Option {0} is missing its parameter", parts[0]));
                            cmdLineError = true;
                        }
                        else if (!GoodParameter(parts[0], parts[1]))
                        {
                            cmdLineError = true;
                        }
                    }
                    else if ((arg.Length > 1) && (arg.Substring(0, 1) == "-"))
                    {
                        i++;
                        if (!optionCommands.ContainsKey(arg))
                        {
                            Console.WriteLine(string.Format("Invalid Option: {0}", arg));
                            cmdLineError = true;
                        }
                        else if ((i == args.Length) || (args[i].Substring(0, 1) == "-"))
                        {
                            Console.WriteLine(string.Format("Error: Option {0} is missing its parameter.", arg));
                            cmdLineError = true;
                        }
                        else if (!GoodParameter(arg, args[i]))
                        {   
                            cmdLineError = true;
                        }
                    }
                    else
                    {
                        if (!runCommands.ContainsKey(arg.ToLower()))
                        {
                            Console.WriteLine(string.Format("Invalid Command: {0}", arg));
                            cmdLineError = true;
                        }
                    }
                }
            }

            return !cmdLineError;
        }

        static bool GoodParameter(string optionStr, string param)
        {
            bool good = false;
            Options option = optionCommands[optionStr];

            switch (option)
            {
                case Options.SetProject:
                    good = true;
                    break;
                case Options.SetTestament:
                    if ((param == "OT") || (param == "NT") || (param == "OT-NT"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} should be OT or NT", optionStr, param));
                    }
                    break;
                case Options.SetRunSpec:
                    if (param.StartsWith("HMM;") || param.StartsWith("IBM1;") || param.StartsWith("IBM2;") || param.StartsWith("FastAlign;"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} should start with 'HMM;', 'IBM1;', 'IBM2;', or 'FastAlign;'", optionStr, param));
                    }
                    break;
                case Options.SetEpsilon:
                    if (double.TryParse(param, out double epsilon))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} should be a number", optionStr, param));
                    }
                    break;
                case Options.SetThotModel:
                    if ((param == "IBM1") || (param == "IBM2") || (param == "HMM") || (param == "FastAlign"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} is an unsupported model", optionStr, param));
                    }
                    break;
                case Options.SetThotHeuristic:
                    if ((param == "Intersection") || (param == "Union") || (param == "Grow") || (param == "GrowDiag") || (param == "GrowDiagFinal") || (param == "GrowDiagFinalAnd") || (param == "Och"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} is an unsupported hueristic", optionStr, param));
                    }
                    break;
                case Options.SetThotIterations:
                    if (int.TryParse(param, out int iterations))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} should be an integer", optionStr, param));
                    }
                    break;
                case Options.SetContentWordsOnly:
                case Options.SetUseAlignModel:
                case Options.SetSmtContentWordsOnly:
                case Options.SetTcContentWordsOnly:
                case Options.SetUseLemmaCatModel:
                case Options.SetUseNoPuncModel:
                    if ((param == "true") || (param == "false"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} should be true or false", optionStr, param));
                    }
                    break;
                default: // This should never happen unless I forgot to revise the mainCommands
                    Console.WriteLine(string.Format("Error: Option {0} has no case statement.", optionStr));
                    break;
            }

            return good;
        }


        static void ProcessArgs(string[] args)
        {

            bool wasSingleCommand = false;

            if (args.Length == 1)
            {
                string singleCommand = args[0].ToLower();

                if (singleCommands.ContainsKey(singleCommand))
                {
                    wasSingleCommand = true;
                    Command command = singleCommands[singleCommand];

                    switch (command)
                    {
                        case Command.Menu:
                            ProcessMenu();
                            break;
                        case Command.Help:
                            ProcessHelp();
                            break;
                        default: // This should never happen unless I forgot to revise the mainCommands
                            Console.WriteLine(string.Format("Error: Command {0} has no case statement.", args[0]));
                            break;
                    }                   
                }
            }

            if (!wasSingleCommand)
            {
                // Since we check the command line already, no need to do it here. We should assume the command line is good.
                bool cmdLineError = false;

                for (int i = 0; (!cmdLineError && (i < args.Length)); i++)
                {
                    string arg = args[i];

                    if ((arg.Length > 2) && (arg.Substring(0, 2) == "--"))
                    {
                        string[] parts = arg.Split("=".ToCharArray());
                        string option = parts[0];

                        if ((parts.Length == 2) && (parts[1] != ""))
                        {
                            string param = parts[1];

                            if (optionCommands.ContainsKey(option))
                            {
                                if (!ProcessOption(option, param))
                                {
                                    Console.WriteLine(string.Format("Error: Option {0} has no case statement.", option));
                                    cmdLineError = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine(string.Format("Invalid Option: {0}", option));
                                cmdLineError = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Error: Option {0} is missing its parameter", option));
                            cmdLineError = true;
                        }

                    }
                    else if ((arg.Length > 1) && (arg.Substring(0, 1) == "-"))
                    {
                        if (optionCommands.ContainsKey(arg))
                        {
                            i++;
                            if ((i < args.Length) && (args[i].Substring(0,1) != "-"))
                            {
                                string param = args[i];
                                if (!ProcessOption(arg, param))
                                {
                                    cmdLineError = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine(string.Format("Error: Option {0} is missing its parameter.", arg));
                                cmdLineError = true;
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Invalid Option: {0}", arg));
                            cmdLineError = true;
                        }
                    }
                    else
                    {
                        string runCommand = arg.ToLower();

                        if (runCommands.ContainsKey(runCommand))
                        {
                            if (!initialized)
                            {
                                ActionsClear3.InitializeClear30Service();
                                ActionsClear3.InitializeTargetFiles();
                                ActionsClear3.InitializeProcessingSettings();
                            }

                            Command command = runCommands[runCommand];

                            switch (command)
                            {
                                case Command.DetleteStateFiles:
                                    Console.WriteLine(ActionsClear3.DeleteStateFiles());
                                    break;
                                case Command.InitializeState:
                                    Console.WriteLine(ActionsClear3.InitializeState());
                                    break;
                                case Command.TokenizeVerses:
                                    Console.WriteLine(ActionsClear3.TokenizeVerses());
                                    break;
                                case Command.CreateParallelFiles:
                                    Console.WriteLine(ActionsClear3.CreateParallelCorpus());
                                    break;
                                case Command.BuildModels:
                                    Console.WriteLine(ActionsClear3.BuildModels());
                                    break;
                                case Command.AutoAlign:
                                    Console.WriteLine(ActionsClear3.AutoAlign());
                                    break;
                                case Command.IncrementalUpdate:
                                    Console.WriteLine(ActionsClear3.IncrementalUpdate());
                                    break;
                                case Command.GlobalUpdate:
                                    Console.WriteLine(ActionsClear3.GlobalUpdate());
                                    break;
                                case Command.AlignToGateway:
                                    Console.WriteLine(ActionsClear3.AlignG2T());
                                    break;
                                case Command.ManuscriptToTarget:
                                    Console.WriteLine(ActionsClear3.AlignM2T());
                                    break;
                                case Command.ProcessAll:
                                    Console.WriteLine(ActionsClear3.ProcessAll());
                                    break;
                                case Command.FreshStart:
                                    Console.WriteLine(ActionsClear3.FreshStart());
                                    break;
                                case Command.DoStart:
                                    Console.WriteLine(ActionsClear3.DoStart());
                                    break;
                                default: // This should never happen unless I forgot to revise the mainCommands
                                    Console.WriteLine(string.Format("Error: Command {0} has no case statement.", arg));
                                    cmdLineError = true;
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Invalid Command: {0}", arg));
                            cmdLineError = true;
                        }
                    }
                }
            }
        }


        private static bool ProcessOption(string optionStr, string param)
        {
            Options option = optionCommands[optionStr];
            bool hasCase = true;

            switch (option)
            {
                case Options.SetProject:
                    ActionsClear3.SetProject(param);
                    Console.WriteLine("project set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetTestament:
                    ActionsClear3.SetTestament(param);
                    Console.WriteLine("testament set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetContentWordsOnly:
                    ActionsClear3.SetContentWordsOnly(param);
                    Console.WriteLine("contentWordsOnly set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetUseAlignModel:
                    ActionsClear3.SetUseAlignModel(param);
                    Console.WriteLine("useAlignModel set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetRunSpec:
                    ActionsClear3.SetRunSpec(param);
                    Console.WriteLine("runSpec set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetEpsilon:
                    ActionsClear3.SetEpsilon(param);
                    Console.WriteLine("epsilon set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetThotModel:
                    ActionsClear3.SetThotModel(param);
                    Console.WriteLine("thotModel set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetThotHeuristic:
                    ActionsClear3.SetThotHeuristic(param);
                    Console.WriteLine("thotHeuristic set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetThotIterations:
                    ActionsClear3.SetThotIterations(param);
                    Console.WriteLine("thotIterations set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetSmtContentWordsOnly:
                    ActionsClear3.SetSmtContentWordsOnly(param);
                    Console.WriteLine("contentWordsOnlySMT set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetTcContentWordsOnly:
                    ActionsClear3.SetTcContentWordsOnly(param);
                    Console.WriteLine("contentWordsOnlyTC set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetUseLemmaCatModel:
                    ActionsClear3.SetUseLemmaCatModel(param);
                    Console.WriteLine("useLemmaCatModel set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetUseNoPuncModel:
                    ActionsClear3.SetUseNoPuncModel(param);
                    Console.WriteLine("useNoPuncModel set to {0}", param);
                    initialized = false;
                    break;
                default: // This should never happen unless I forgot to revise the mainCommands
                    Console.WriteLine(string.Format("Error: Option {0} has no case statement.", optionStr));
                    hasCase = false;
                    break;
            }

            return hasCase;
        }


        private static void ProcessMenu()
        {
            var menuChoices = new Dictionary<string, string>()
            {
                {"1", "DeleteStateFiles"},
                {"2", "InitializeState"},
                {"3", "TokenizeVerses"}, 
                {"4", "CreateParallelFiles"}, 
                {"5", "BuildModels"}, 
                {"6", "AutoAlign"}, 
                {"7", "IncrementalUpdate"}, 
                {"8", "GlobalUpdate"}, 
                {"9", "AlignToGateway"},
                {"10", "ManuscriptToTarget"},
                {"11", "ProcessAll"},
                {"12", "FreshStart"},
                {"13", "DoStart"},
                {"h", "help"},
            };

            string choice = string.Empty;
            string[] args = { "" };

            while (choice != "x")
            {
                DisplayMenu();
                choice = Console.ReadLine();

                if (menuChoices.ContainsKey(choice))
                {
                    args[0] = menuChoices[choice];
                    ProcessArgs(args);
                }
                else
                {
                    if (choice != "x")
                    {
                        Console.WriteLine(string.Format("Invalid input: {0}", choice));
                    }
                }                
            }
        }


        private static void DisplayMenu()
        {
            Console.WriteLine();
            Console.WriteLine("MENU:");
            Console.WriteLine("\t1.  DeleteStateFiles");
            Console.WriteLine("\t2.  InitializeState");
            Console.WriteLine("\t3.  TokenizeVerses");
            Console.WriteLine("\t4.  CreateParallelFiles");
            Console.WriteLine("\t5.  BuildModels");
            Console.WriteLine("\t6.  AutoAlign");
            Console.WriteLine("\t7.  IncrementalUpdate");
            Console.WriteLine("\t8.  GlobalUpdate");
            Console.WriteLine("\t9.  AlignToGateway");
            Console.WriteLine("\t10. ManuscriptToTarget");
            Console.WriteLine("\t11. ProcessAll");
            Console.WriteLine("\t12. FreshStart");
            Console.WriteLine("\t13. DoStart");
            Console.WriteLine("\th.  Help");
            Console.WriteLine("\tx.  Exit");
            Console.WriteLine();
            Console.Write("Type Your Choice (1-12, h, x): ");
        }


        private static void ProcessHelp()
        {
            Console.WriteLine("Here are the following options:");
            Console.WriteLine("\t-p <project>, --project=<project>");
            Console.WriteLine("\t\tSets the project to be processed to <project>.");
            Console.WriteLine("\t-t <testament>, --testament=<testament>");
            Console.WriteLine("\t\tSets the testament to be processed to <testament>, which can only be OT or NT");
            Console.WriteLine("\t-c <bool>, --content-words-only=<bool>");
            Console.WriteLine("\t\tSets the contentWordsOnly boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-a <bool>, --use-align-model=<bool>");
            Console.WriteLine("\t\tSets the useAlignModel boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-e <double>, --epsilon=<double>");
            Console.WriteLine("\t\tSets epsilon (threshold) to <double>, e.g. 0.1");
            Console.WriteLine("\t-h <heuristic>, --heuristic=<heuristic>");
            Console.WriteLine("\t\tSets the Thot model heursitic to <heursitic>, e.g. Intersection");
            Console.WriteLine("\t-i <int>, --iterations=<int>");
            Console.WriteLine("\t\tSets number of iterations for Thot model to <int>, e.g. 7");
            Console.WriteLine("\t-m <model>, --model=<model>");
            Console.WriteLine("\t\tSets the Thot model to <model>, e.g. FastAlign");
            Console.WriteLine("\t-r <runspec>, --runspec=<runspec>");
            Console.WriteLine("\t\tSets the runSpec to <runspec>, e.g. 1:10;H:5 or Machine;FastAlign:Inter");
            Console.WriteLine("\t-smt <bool>, --smt-content-words-only=<bool>");
            Console.WriteLine("\t\tSets the contentWordsOnlySMT boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-tc <bool>, --tc-content-words-only=<bool>");
            Console.WriteLine("\t\tSets the contentWordsOnlyTC boolean to <bool>, which can only be true or false");
            Console.WriteLine("Here are the following commands:");
            Console.WriteLine("\tDeleteStateFiles");
            Console.WriteLine("\tInitializeState");
            Console.WriteLine("\tTokenizeVerses");
            Console.WriteLine("\tCreateParallelFiles");
            Console.WriteLine("\tBuildnModels");
            Console.WriteLine("\tAutoAlign");
            Console.WriteLine("\tIncrementalUpdate");
            Console.WriteLine("\tGlobalUpdate");
            Console.WriteLine("\tAlignToGateway");
            Console.WriteLine("\tManuscriptToTarget");
            Console.WriteLine("\tProcessAll");
            Console.WriteLine("\tFreshStart");
            Console.WriteLine("\tDoStart");
            Console.WriteLine("\tMenu");
            Console.WriteLine("\tHelp or ?");
            Console.Write("Press [Return] to continue.");
            Console.ReadLine();
        }


        private static void LoadTables()
        {
            singleCommands = new Dictionary<string, Command>()
            {
                {"menu", Command.Menu },
                {"help", Command.Help },
                {"?", Command.Help},
            };

            runCommands = new Dictionary<string, Command>()
            {
                {"deletestatefiles", Command.DetleteStateFiles },
                {"initializestate", Command.InitializeState },
                {"tokenizeverses", Command.TokenizeVerses },
                {"createparallelfiles", Command.CreateParallelFiles },
                {"buildmodels", Command.BuildModels },
                {"autoalign", Command.AutoAlign },
                {"incrementalupdate", Command.IncrementalUpdate },
                {"globalupdate", Command.GlobalUpdate },
                {"aligntogateway", Command.AlignToGateway },
                {"manuscripttotarget", Command.ManuscriptToTarget },
                {"processall", Command.ProcessAll },
                {"freshstart", Command.FreshStart },
                {"dostart", Command.DoStart },
            };

            optionCommands = new Dictionary<string, Options>()
            {
                {"-p", Options.SetProject }, {"--project", Options.SetProject },
                {"-t", Options.SetTestament }, {"--testament", Options.SetTestament },
                {"-c", Options.SetContentWordsOnly }, {"--content-words-only", Options.SetContentWordsOnly },
                {"-a", Options.SetUseAlignModel }, {"--use-align-model", Options.SetUseAlignModel },
                {"-r", Options.SetRunSpec }, {"--runspec", Options.SetRunSpec },
                {"-e", Options.SetEpsilon }, {"--epsilon", Options.SetEpsilon },
                {"-m", Options.SetThotModel }, {"--model", Options.SetThotModel },
                {"-h", Options.SetThotHeuristic }, {"--hueristic", Options.SetThotHeuristic },
                {"-i", Options.SetThotIterations }, {"--iterations", Options.SetThotIterations },
                {"-smt", Options.SetSmtContentWordsOnly }, {"--smt-content-words-only", Options.SetSmtContentWordsOnly },
                {"-tc", Options.SetTcContentWordsOnly }, {"--tc-content-words-only", Options.SetTcContentWordsOnly },
                {"-lc", Options.SetUseLemmaCatModel }, {"--use-lemma-cat-model", Options.SetUseLemmaCatModel },
                {"-np", Options.SetUseNoPuncModel }, {"--use-no-punc-model", Options.SetUseNoPuncModel },
            };
        }


        private static bool initialized = false;
        private static Dictionary<string, Command> singleCommands;
        private static Dictionary<string, Command> runCommands;
        private static Dictionary<string, Options> optionCommands;

    }
}
