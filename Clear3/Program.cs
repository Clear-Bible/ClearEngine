using System;
using System.Collections.Generic;

namespace Clear3
{
    public enum Command
    {
        DeleteStateFiles, // Delete existing state files (for a fresh start)
        InitializeState, // Initialize the State
        TokenizeVerses, // Tokenize a verse text file
        LemmatizeVerses, // Lemmatize a tokenized verse text file
        CreateParallelCorpus, // Create Parallel Files
        BuildModels, // Build Translation Model with these files and specs
        AutoAlign,
        IncrementalUpdate,
        GlobalUpdate, // Rebuild the model with these 3 files:
        AlignG2TviaM2G, // Align to the gateway translation，using (1) target translation, (2) alignment between manuscript and gateway
        AlignM2TviaM2G, // Create manuscript-to-target alignment through gateway-to-target alignment
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
        SetSmtModel,
        SetSmtHeuristic,
        SetSmtIterations,

        SetContentWordsOnlySMT,
        SetContentWordsOnlyTC,

        SetUseLemmaCatModel,
        SetUseNoPuncModel,
        SetUseNormalizedTransModelProbabilities,
        SetUseNormalizedAlignModelProbabilities,

        SetReuseTokenFiles,
        SetReuseLemmaFiles,
        SetReuseParallelCorporaFiles,
        SetReuseSmtModelFiles,

        SetLowerCaseMethod,
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
                        var optionStr = parts[0].ToLower();
                        if (!optionCommands.ContainsKey(optionStr))
                        {
                            Console.WriteLine(string.Format("Invalid Option: {0}", parts[0]));
                            cmdLineError = true;
                        }
                        else if (!optionActions.ContainsKey(optionCommands[optionStr]))
                        {
                            Console.WriteLine(string.Format("Error: Option {0} has no delegate", parts[0]));
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
                        var optionStr = arg.ToLower();
                        i++;
                        if (!optionCommands.ContainsKey(optionStr))
                        {
                            Console.WriteLine(string.Format("Invalid Option: {0}", arg));
                            cmdLineError = true;
                        }
                        else if (!optionActions.ContainsKey(optionCommands[optionStr]))
                        {
                            Console.WriteLine(string.Format("Error: Option {0} has no delegate", arg));
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
                        else if (!commandFunctions.ContainsKey(runCommands[arg.ToLower()]))
                        {
                            Console.WriteLine(string.Format("Error: Command {0} has no delegate", arg));
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
                case Options.SetLowerCaseMethod:
                    if ((param == "None") || (param == "ToLower") || (param == "ToLowerInvariant") || (param == "CultureInfo"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} is an unsupported lowercase selection", optionStr, param));
                    }
                    break;
                case Options.SetRunSpec:
                    if (param.StartsWith("HMM-") || param.StartsWith("IBM1-") || param.StartsWith("IBM2-") || param.StartsWith("IBM3-") || param.StartsWith("IBM4-") || param.StartsWith("FastAlign-"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} should start with 'HMM-', 'IBM1-', 'IBM2-', 'IBM3-', 'IBM4-', or 'FastAlign-'", optionStr, param));
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
                case Options.SetSmtModel:
                    if ((param == "IBM1") || (param == "IBM2") || (param == "IBM3") || (param == "IBM4") || (param == "HMM") || (param == "FastAlign"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} is an unsupported model", optionStr, param));
                    }
                    break;
                case Options.SetSmtHeuristic:
                    if ((param == "Intersection") || (param == "Union") || (param == "Grow") || (param == "GrowDiag") || (param == "GrowDiagFinal") || (param == "GrowDiagFinalAnd") || (param == "Och"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter {1} is an unsupported hueristic", optionStr, param));
                    }
                    break;
                case Options.SetSmtIterations:
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
                case Options.SetContentWordsOnlySMT:
                case Options.SetContentWordsOnlyTC:
                case Options.SetUseLemmaCatModel:
                case Options.SetUseNoPuncModel:
                case Options.SetUseNormalizedTransModelProbabilities:
                case Options.SetUseNormalizedAlignModelProbabilities:
                case Options.SetReuseTokenFiles:
                case Options.SetReuseLemmaFiles:
                case Options.SetReuseParallelCorporaFiles:
                case Options.SetReuseSmtModelFiles:
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

        // Since we check the comand line before processing the command line, I think we can get rid of checking for errors.
        // We should assume there are no errors in the command line.
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
                                cmdLineError = !ProcessOption(option, param);
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
                            if ((i < args.Length) && (args[i].Substring(0, 1) != "-"))
                            {
                                string param = args[i];
                                cmdLineError = !ProcessOption(arg, param);
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
                        cmdLineError = !ProcessCommand(arg);
                    }
                }
            }
        }

        // Assumes no problems with the command string since it has already been check in CommandLineIsGood()
        private static bool ProcessCommand(string commandStr)
        {
            if (!initialized)
            {
                ActionsClear3.InitializeTargetFiles();
                ActionsClear3.InitializeProcessingSettings();
            }

            Command command = runCommands[commandStr.ToLower()];

            (var succeeded, var message) = commandFunctions[command]();

            Console.WriteLine(message);

            return succeeded;
        }

        // Assumes no problems with the command string since it has already been check in CommandLineIsGood()
        private static bool ProcessOption(string optionStr, string param)
        {
            Options option = optionCommands[optionStr];

            optionActions[option](param);

            Console.WriteLine("  {0} set to {1}", optionNames[option], param);
            initialized = false;

            return true;
        }

        private static void ProcessMenu()
        {
            var menuChoices = new Dictionary<string, string>()
            {
                { "1", "DeleteStateFiles" },
                { "2", "InitializeState" },
                { "3", "TokenizeVerses" },
                { "4", "LemmatizeVerses" },
                { "5", "CreateParallelFiles" },
                { "6", "BuildModels" },
                { "7", "AutoAlign" },
                { "8", "IncrementalUpdate" },
                { "9", "GlobalUpdate" },
                { "10", "AlignToGateway" },
                { "11", "ManuscriptToTarget" },
                { "12", "ProcessAll" },
                { "13", "FreshStart" },
                { "14", "DoStart" },
                { "h", "help" },
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
            Console.WriteLine("\t4.  LemmatizeVerses");
            Console.WriteLine("\t5.  CreateParallelFiles");
            Console.WriteLine("\t6.  BuildModels");
            Console.WriteLine("\t7.  AutoAlign");
            Console.WriteLine("\t8.  IncrementalUpdate");
            Console.WriteLine("\t9.  GlobalUpdate");
            Console.WriteLine("\t10. AlignToGateway");
            Console.WriteLine("\t11. ManuscriptToTarget");
            Console.WriteLine("\t12. ProcessAll");
            Console.WriteLine("\t13. FreshStart");
            Console.WriteLine("\t14. DoStart");
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
            Console.WriteLine("\t\tSets the SMT model heursitic to <heursitic>, e.g. Intersection");
            Console.WriteLine("\t-i <int>, --iterations=<int>");
            Console.WriteLine("\t\tSets number of iterations for SMT model to <int>, e.g. 7");
            Console.WriteLine("\t-m <model>, --model=<model>");
            Console.WriteLine("\t\tSets the SMT model to <model>, which can only be IBM1, IBM2, IBM3, IBM4, HMM, FastAlign");
            Console.WriteLine("\t-r <runspec>, --runspec=<runspec>");
            Console.WriteLine("\t\tSets the runSpec to <runspec>, e.g. 1:10;H:5 or Machine;FastAlign:Inter");

            Console.WriteLine("\t-smt <bool>, --smt-content-words-only=<bool>");
            Console.WriteLine("\t\tSets the contentWordsOnlySMT boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-tc <bool>, --tc-content-words-only=<bool>");
            Console.WriteLine("\t\tSets the contentWordsOnlyTC boolean to <bool>, which can only be true or false");

            Console.WriteLine("\t-lc <bool>, --use-lemma-cat-model=<bool>");
            Console.WriteLine("\t\tSets the useLemmaCatModel boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-lcm <method>, --lowercase-method=<method>");
            Console.WriteLine("\t\tSets the lowerCaseToLemma variable to <method>, which can only be None, ToLower, ToLowerInvarient, or CultureInfo");
            Console.WriteLine("\t-np <bool>, --use-no-punc-model=<bool>");
            Console.WriteLine("\t\tSets the useNoPuncModel boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-ntp <bool>, --use-normalized-transmodel-probabilities=<bool>");
            Console.WriteLine("\t\tSets the useNormalizedTransModelProbabilities boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-nap <bool>, --use-normalized-alignmodel-probabilities=<bool>");
            Console.WriteLine("\t\tSets the useNormalizedAlignModelProbabilities boolean to <bool>, which can only be true or false");

            Console.WriteLine("\t-rt <bool>, --reuse-tokenized-files=<bool>");
            Console.WriteLine("\t\tSets the reuseTokenizedFiles boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-rl <bool>, --reuse-lemmatized-files=<bool>");
            Console.WriteLine("\t\tSets the reuseLemmatizedFiles boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-rc <bool>, --reuse-parallel-corpora-files=<bool>");
            Console.WriteLine("\t\tSets the reuseParallelCorporaFiles boolean to <bool>, which can only be true or false");
            Console.WriteLine("\t-rm <bool>, --reuse-smt-model-files=<bool>");
            Console.WriteLine("\t\tSets the reuseSmtModelFiles boolean to <bool>, which can only be true or false");

            Console.WriteLine("Here are the following commands:");
            Console.WriteLine("\tDeleteStateFiles");
            Console.WriteLine("\tInitializeState");
            Console.WriteLine("\tTokenizeVerses");
            Console.WriteLine("\tLemmatizeVerses");
            Console.WriteLine("\tCreateParallelFiles");
            Console.WriteLine("\tBuildnModels");
            Console.WriteLine("\tAutoAlign");
            Console.WriteLine("\tIncrementalUpdate");
            Console.WriteLine("\tGlobalUpdate");
            Console.WriteLine("\tAlignToGateway");
            Console.WriteLine("\tAlignViaGateway");
            Console.WriteLine("\tProcessAll");
            Console.WriteLine("\tFreshStart");
            Console.WriteLine("\tDoStart");
            Console.WriteLine("\tMenu");
            Console.WriteLine("\tHelp or ?");
            Console.Write("Press [Return] to continue.");
            Console.ReadLine();
        }

        private static bool initialized = false;

        private static Dictionary<string, Command> singleCommands = new Dictionary<string, Command>()
            {
                { "menu", Command.Menu },
                { "help", Command.Help },
                { "?", Command.Help },
            };

        private static Dictionary<string, Command> runCommands = new Dictionary<string, Command>()
            {
                { "deletestatefiles", Command.DeleteStateFiles },
                { "initializestate", Command.InitializeState },
                { "tokenizeverses", Command.TokenizeVerses },
                { "lemmatizeverses", Command.LemmatizeVerses },
                { "createparallelfiles", Command.CreateParallelCorpus },
                { "buildmodels", Command.BuildModels },
                { "autoalign", Command.AutoAlign },
                { "incrementalupdate", Command.IncrementalUpdate },
                { "globalupdate", Command.GlobalUpdate },
                { "aligntogateway", Command.AlignG2TviaM2G },
                { "alignviagateway", Command.AlignM2TviaM2G },
                { "processall", Command.ProcessAll },
                { "freshstart", Command.FreshStart },
                { "dostart", Command.DoStart },
            };

        private delegate (bool, string) Commands();

        private static Dictionary<Command, Commands> commandFunctions = new Dictionary<Command, Commands>()
            {
                { Command.DeleteStateFiles, ActionsClear3.DeleteStateFiles },
                { Command.InitializeState, ActionsClear3.InitializeState },
                { Command.TokenizeVerses, ActionsClear3.TokenizeVerses },
                { Command.LemmatizeVerses, ActionsClear3.LemmatizeVerses },
                { Command.CreateParallelCorpus, ActionsClear3.CreateParallelCorpus },
                { Command.BuildModels, ActionsClear3.BuildModels },
                { Command.AutoAlign, ActionsClear3.AutoAlign },
                { Command.IncrementalUpdate, ActionsClear3.IncrementalUpdate },
                { Command.GlobalUpdate, ActionsClear3.GlobalUpdate },
                { Command.AlignG2TviaM2G, ActionsClear3.AlignG2TviaM2G },
                { Command.AlignM2TviaM2G, ActionsClear3.AlignM2TviaM2G },
                { Command.ProcessAll, ActionsClear3.ProcessAll },
                { Command.FreshStart, ActionsClear3.FreshStart },
                { Command.DoStart, ActionsClear3.DoStart },
            };

        private static Dictionary<string, Options> optionCommands = new Dictionary<string, Options>()
            {
                { "-p", Options.SetProject }, { "--project", Options.SetProject },
                { "-t", Options.SetTestament }, { "--testament", Options.SetTestament },

                { "-c", Options.SetContentWordsOnly }, { "--content-words-only", Options.SetContentWordsOnly },
                { "-a", Options.SetUseAlignModel }, { "--use-align-model", Options.SetUseAlignModel },
                { "-r", Options.SetRunSpec }, { "--runspec", Options.SetRunSpec },
                { "-e", Options.SetEpsilon }, { "--epsilon", Options.SetEpsilon },
                { "-m", Options.SetSmtModel }, { "--model", Options.SetSmtModel },
                { "-h", Options.SetSmtHeuristic }, { "--hueristic", Options.SetSmtHeuristic },
                { "-i", Options.SetSmtIterations }, { "--iterations", Options.SetSmtIterations },

                { "-smt", Options.SetContentWordsOnlySMT }, { "--smt-content-words-only", Options.SetContentWordsOnlySMT },
                { "-tc", Options.SetContentWordsOnlyTC }, { "--tc-content-words-only", Options.SetContentWordsOnlyTC },

                { "-lc", Options.SetUseLemmaCatModel }, { "--use-lemma-cat-model", Options.SetUseLemmaCatModel },
                { "-lcm", Options.SetLowerCaseMethod }, { "--lowercase-method", Options.SetLowerCaseMethod },
                { "-np", Options.SetUseNoPuncModel }, { "--use-no-punc-model", Options.SetUseNoPuncModel },
                { "-ntp", Options.SetUseNormalizedTransModelProbabilities }, { "--use-normalized-transmodel-probabilities", Options.SetUseNormalizedTransModelProbabilities },
                { "-nap", Options.SetUseNormalizedAlignModelProbabilities }, { "--use-normalized-alignmodel-probabilities", Options.SetUseNormalizedAlignModelProbabilities },

                { "-rt", Options.SetReuseTokenFiles }, { "--reuse-tokenized-files", Options.SetReuseTokenFiles },
                { "-rl", Options.SetReuseLemmaFiles }, { "--reuse-lemmatized-files", Options.SetReuseLemmaFiles },
                { "-rc", Options.SetReuseParallelCorporaFiles }, { "--reuse-parallel-corpora-files", Options.SetReuseParallelCorporaFiles },
                { "-rm", Options.SetReuseSmtModelFiles }, { "--reuse-smt-model-files", Options.SetReuseSmtModelFiles },
            };

        private static Dictionary<Options, string> optionNames = new Dictionary<Options, string>()
            {
                { Options.SetProject, "project" },
                { Options.SetTestament, "testament" },

                { Options.SetContentWordsOnly, "contentWordsOnly" },
                { Options.SetUseAlignModel, "useAlignModel" },
                { Options.SetRunSpec, "runSpec" },
                { Options.SetEpsilon, "epsilon" },
                { Options.SetSmtModel, "smtModel" },
                { Options.SetSmtHeuristic, "smtHeuristic" },
                { Options.SetSmtIterations, "smtIterations" },

                { Options.SetContentWordsOnlySMT, "contentWordsOnlySMT" },
                { Options.SetContentWordsOnlyTC, "contentWordsOnlyTC" },

                { Options.SetUseLemmaCatModel, "useLemmaCatModel" },
                { Options.SetLowerCaseMethod, "lowerCaseMethod" },
                { Options.SetUseNoPuncModel, "useNoPuncModel" },
                { Options.SetUseNormalizedTransModelProbabilities, "useNormalizedTransModelProbabilities" },
                { Options.SetUseNormalizedAlignModelProbabilities, "useNormalizedAlignModelProbabilities" },

                { Options.SetReuseTokenFiles, "reuseTokenFiles" },
                { Options.SetReuseLemmaFiles, "reuseLemmaFiles" },
                { Options.SetReuseParallelCorporaFiles, "reuseParallelCorporaFiles" },
                { Options.SetReuseSmtModelFiles, "reuseSmtModelFiles" },
            };

        delegate void OptionActions(string param);

        private static Dictionary<Options, OptionActions> optionActions = new Dictionary<Options, OptionActions>()
            {
                { Options.SetProject, ActionsClear3.SetProject },
                { Options.SetTestament, ActionsClear3.SetTestament },

                { Options.SetContentWordsOnly, ActionsClear3.SetContentWordsOnly },
                { Options.SetUseAlignModel, ActionsClear3.SetUseAlignModel },
                { Options.SetRunSpec, ActionsClear3.SetRunSpec },
                { Options.SetEpsilon, ActionsClear3.SetEpsilon },
                { Options.SetSmtModel, ActionsClear3.SetSmtModel },
                { Options.SetSmtHeuristic, ActionsClear3.SetSmtHeuristic },
                { Options.SetSmtIterations, ActionsClear3.SetSmtIterations },

                { Options.SetContentWordsOnlySMT, ActionsClear3.SetContentWordsOnlySMT },
                { Options.SetContentWordsOnlyTC, ActionsClear3.SetContentWordsOnlyTC },

                { Options.SetUseLemmaCatModel, ActionsClear3.SetUseLemmaCatModel },
                { Options.SetLowerCaseMethod, ActionsClear3.SetLowerCaseMethod },
                { Options.SetUseNoPuncModel, ActionsClear3.SetUseNoPuncModel },
                { Options.SetUseNormalizedTransModelProbabilities, ActionsClear3.SetUseNormalizedTransModelProbabilities },
                { Options.SetUseNormalizedAlignModelProbabilities, ActionsClear3.SetUseNormalizedAlignModelProbabilities },

                { Options.SetReuseTokenFiles, ActionsClear3.SetReuseTokenFiles },
                { Options.SetReuseLemmaFiles, ActionsClear3.SetReuseLemmFiles },
                { Options.SetReuseParallelCorporaFiles, ActionsClear3.SetReuseParallelCorporaFiles },
                { Options.SetReuseSmtModelFiles, ActionsClear3.SetReuseSmtModelFiles },
            };

    }
}
