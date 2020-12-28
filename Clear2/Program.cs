using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


using TransModels;
using System.Globalization;

namespace Clear2
{
    public enum Command { 
        Story1, AutoAlign,
        Story2, IncrementalUpdate,
        Story3, RebuildModel, // Rebuild the model with these 3 files:
        Story4, AlignToGateway, // Align to the gateway translation，using (1) target translation, (2) alignment between manuscript and gateway
        Story5, ManuscriptToTarget, // Create manuscript-to-target alignment through gateway-to-target alignment
        Story6, BuildTranslationModel, // Build Translation Model with these files and specs
        Story7, TokenizeVerses, // Tokenize a verse text file
        Story8, CreateParallelFiles, // Create Parallel Files
        Story9, FreshStart, // Process from a Fresh Start
        Story10, CopyInitialFiles, // Copy the initial files (empty) to the target folder
        Story11, InitializeState, // Copy the initial files (empty) to the target folder
        Story12, Process, // Copy the initial files (empty) to the target folder
        Menu, Help,
    }
    public enum Options
    {
        SetProject, 
        SetTestament,
        SetContentWordsOnly,
    }

    static class program
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
                if (CheckCommandLine(args))
                {
                    Console.WriteLine();
                    Console.WriteLine("Running ClearEngine 2");
                    ActionsClear3.InitializeConfig();
                    ProcessArgs(args);
                }
            }            
        }

        static bool CheckCommandLine(string[] args)
        {
            bool cmdLineError = false;

            if ((args.Length != 1) || !singleCommands.ContainsKey(args[0].ToLower()))
            {
                for (int i = 0; (!cmdLineError && (i < args.Length)); i++)
                {
                    string arg = args[i];

                    if ((arg.Length > 2) && (arg.Substring(0, 2) == "--"))
                    {
                        string[] parts = arg.Split("=".ToCharArray());
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
                    if ((param == "OT") || (param == "NT"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter should be OT or NT", optionStr));
                    }
                    break;
                case Options.SetContentWordsOnly:
                    if ((param == "true") || (param == "false"))
                    {
                        good = true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Error: Option {0} parameter should be true or false", optionStr));
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
                                ActionsClear3.InitializeFiles();
                                ActionsClear3.Initialize();
                            }

                            Command command = runCommands[runCommand];

                            switch (command)
                            {
                                case Command.Story1:
                                case Command.AutoAlign:
                                    Console.WriteLine(ActionsClear3.Do_Button1());
                                    Console.WriteLine("Done doing Story1.");
                                    break;
                                case Command.Story2:
                                case Command.IncrementalUpdate:
                                    Console.WriteLine(ActionsClear3.Do_Button2());
                                    Console.WriteLine("Done doing Story2.");
                                    break;
                                case Command.Story3:
                                case Command.RebuildModel:
                                    Console.WriteLine(ActionsClear3.Do_Button3());
                                    Console.WriteLine("Done doing Story3.");
                                    break;
                                case Command.Story4:
                                case Command.AlignToGateway:
                                    Console.WriteLine(ActionsClear3.Do_Button4());
                                    Console.WriteLine("Done doing Story4.");
                                    break;
                                case Command.Story5:
                                case Command.ManuscriptToTarget:
                                    Console.WriteLine(ActionsClear3.Do_Button5());
                                    Console.WriteLine("Done doing Story5.");
                                    break;
                                case Command.Story6:
                                case Command.BuildTranslationModel:
                                    Console.WriteLine(ActionsClear3.Do_Button6());
                                    Console.WriteLine("Done doing Story6.");
                                    break;
                                case Command.Story7:
                                case Command.TokenizeVerses:
                                    Console.WriteLine(ActionsClear3.Do_Button7());
                                    Console.WriteLine("Done doing Story7.");
                                    break;
                                case Command.Story8:
                                case Command.CreateParallelFiles:
                                    Console.WriteLine(ActionsClear3.Do_Button8());
                                    Console.WriteLine("Done doing Story8.");
                                    break;
                                case Command.Story9:
                                case Command.FreshStart:
                                    Console.WriteLine(ActionsClear3.Do_Button9());
                                    Console.WriteLine("Done doing Story9.");
                                    break;
                                case Command.Story10:
                                case Command.CopyInitialFiles:
                                    Console.WriteLine(ActionsClear3.Do_Button10());
                                    Console.WriteLine("Done doing Story10.");
                                    break;
                                case Command.Story11:
                                case Command.InitializeState:
                                    Console.WriteLine(ActionsClear3.Do_Button11());
                                    Console.WriteLine("Done doing Story11.");
                                    break;
                                case Command.Story12:
                                case Command.Process:
                                    Console.WriteLine(ActionsClear3.Do_Button12());
                                    Console.WriteLine("Done doing Story12.");
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
                    Console.WriteLine("Project set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetTestament:
                    ActionsClear3.SetTestament(param);
                    Console.WriteLine("Testament set to {0}", param);
                    initialized = false;
                    break;
                case Options.SetContentWordsOnly:
                    ActionsClear3.SetContentWordsOnly(param);
                    Console.WriteLine("ContentWordsOnly set to {0}", param);
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
            var menuChoices = new SortedDictionary<string, string>()
            {
                {"1", "story1"},
                {"2", "story2"},
                {"3", "story3"}, 
                {"4", "story4"}, 
                {"5", "story5"}, 
                {"6", "story6"}, 
                {"7", "story7"}, 
                {"8", "story8"}, 
                {"9", "story9"},
                {"10", "story10"},
                {"11", "story10"},
                {"12", "story10"},
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
            Console.WriteLine("\t1. Story1 or AutoAlign");
            Console.WriteLine("\t2. Story2 or IncrementalUpdate");
            Console.WriteLine("\t3. Story3 or RebuildModel");
            Console.WriteLine("\t4. Story4 or AlignToGateway");
            Console.WriteLine("\t5. Story5 or ManuscriptToTarget");
            Console.WriteLine("\t6. Story6 or BuildTranslationModel");
            Console.WriteLine("\t7. Story7 or TokenizeVerses");
            Console.WriteLine("\t8. Story8 or CreateParallelFiles");
            Console.WriteLine("\t9. Story9 or FreshStart");
            Console.WriteLine("\t10.Story10 or CopyInitialFiles");
            Console.WriteLine("\t11. Story11 or InitializeState");
            Console.WriteLine("\t12. Story12 or Process");
            Console.WriteLine("\th. Help");
            Console.WriteLine("\tx. Exit");
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
            Console.WriteLine("\t\tSets the contentWordsOnly boolean to be processed to <bool>, which can only be true or false");
            Console.WriteLine("Here are the following commands:");
            Console.WriteLine("\tStory1 or AutoAlign");
            Console.WriteLine("\tStory2 or IncrementalUpdate");
            Console.WriteLine("\tStory3 or RebuildModel");
            Console.WriteLine("\tStory4 or AlignToGateway");
            Console.WriteLine("\tStory5 or ManuscriptToTarget");
            Console.WriteLine("\tStory6 or BuildTranslationModel");
            Console.WriteLine("\tStory7 or TokenizeVerses");
            Console.WriteLine("\tStory8 or CreateParallelFiles");
            Console.WriteLine("\tStory9 or FreshStart");
            Console.WriteLine("\tStory10 or CopyInitialFiles");
            Console.WriteLine("\tStory11 or InitializeState");
            Console.WriteLine("\tStory12 or Process");
            Console.WriteLine("\tMenu");
            Console.WriteLine("\tHelp or ?");
            Console.Write("Press [Return] to continue.");
            Console.ReadLine();
        }

        private static void LoadTables()
        {
            singleCommands = new SortedDictionary<string, Command>()
            {
                {"menu", Command.Menu },
                {"help", Command.Help }, {"?", Command.Help},
            };

            runCommands = new SortedDictionary<string, Command>()
            {
                {"story1", Command.Story1 }, {"autoalign", Command.AutoAlign },
                {"story2", Command.Story2 }, {"incrementalupdate", Command.IncrementalUpdate },
                {"story3", Command.Story3 }, {"rebuildmodel", Command.RebuildModel },
                {"story4", Command.Story4 }, {"aligntogateway", Command.AlignToGateway },
                {"story5", Command.Story5 }, {"manuscripttotarget", Command.ManuscriptToTarget },
                {"story6", Command.Story6 }, {"buildtranslationmodel", Command.BuildTranslationModel },
                {"story7", Command.Story7 }, {"tokenizeverses", Command.TokenizeVerses },
                {"story8", Command.Story8 }, {"createparallelfiles", Command.CreateParallelFiles },
                {"story9", Command.Story9 }, {"freshstart", Command.FreshStart },
                {"story10", Command.Story10 }, {"copyinitialfiles", Command.CopyInitialFiles },
                {"story11", Command.Story11 }, {"initializestate", Command.InitializeState },
                {"story12", Command.Story12 }, {"process", Command.Process },
            };

            optionCommands = new SortedDictionary<string, Options>()
            {
                {"-p", Options.SetProject }, {"--project", Options.SetProject },
                {"-t", Options.SetTestament }, {"--testament", Options.SetTestament },
                {"-c", Options.SetContentWordsOnly }, {"--content-words-only", Options.SetContentWordsOnly },
            };
        }


        private static bool initialized = false;
        private static SortedDictionary<string, Command> singleCommands;
        private static SortedDictionary<string, Command> runCommands;
        private static SortedDictionary<string, Options> optionCommands;

    }
}
