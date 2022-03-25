using System;
using System.IO;
using System.Collections.Generic;

namespace Clear3
{
    public class ParallelCorpus
    {
        //
        public static void CreateContentWordsOnlyCorpus(
            bool reuseParallelCorporaFiles,
            HashSet<string> sourceFunctionWords,
            HashSet<string> targetFunctionWords,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceLemmaCatFile,
            string sourceIdFile,
            string targetTextFile,
            string targetTextIdFile,
            string targetLemmaFile,
            string targetLemmaIdFile)
        {
            (string sourceLemmaFileCW, string sourceIdFileCW, string sourceTextFileCW, string sourceLemmaCatFileCW,
                string targetLemmaFileCW, string targetLemmaIdFileCW, string targetTextFileCW, string targetTextIdFileCW) =
                    BuildModelTools.InitializeCreateParallelCorporaFiles(
                        true, false,
                        sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile,
                        targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);

            if (reuseParallelCorporaFiles &&
                File.Exists(sourceLemmaFileCW) && File.Exists(sourceIdFileCW) && File.Exists(sourceTextFileCW) && File.Exists(sourceLemmaCatFileCW) &&
                File.Exists(targetLemmaFileCW) && File.Exists(targetLemmaIdFileCW) && File.Exists(targetTextFileCW) && File.Exists(targetTextIdFileCW))
            {
                Console.WriteLine("  Reusing content words only parallel corpus files.");
            }
            else
            {
                Console.WriteLine("  Creating content words only parallel corpus files.");

                ShowTime();

                Data.FilterOutWords(
                    sourceLemmaFile, sourceIdFile, sourceTextFile, sourceLemmaCatFile,
                    sourceLemmaFileCW, sourceIdFileCW, sourceTextFileCW, sourceLemmaCatFileCW,
                    sourceFunctionWords);
                Data.FilterOutWordsLemmaText(
                    targetLemmaFile, targetLemmaIdFile, targetTextFile, targetTextIdFile,
                    targetLemmaFileCW, targetLemmaIdFileCW, targetTextFileCW, targetTextIdFileCW,
                    targetFunctionWords);

                ShowTime();
            }
        }


        // 
        public static void CreateNoPuncCorpus(
            bool useContentWordsOnly,
            bool reuseParallelCorporaFiles,
            HashSet<string> puncs,
            string sourceTextFile,
            string sourceLemmaFile,
            string sourceLemmaCatFile,
            string sourceIdFile,
            string targetTextFile,
            string targetTextIdFile,
            string targetLemmaFile,
            string targetLemmaIdFile)
        {
            string sourceLemmaNoPuncFile;
            string sourceIdNoPuncFile;
            string sourceTextNoPuncFile;
            string sourceLemmaCatNoPuncFile;
            string targetLemmaNoPuncFile;
            string targetLemmaIdNoPuncFile;
            string targetTextNoPuncFile;
            string targetTextIdNoPuncFile;

            if (useContentWordsOnly)
            {
                // Change starting files to content words only files
                (sourceLemmaFile, sourceIdFile, sourceTextFile, sourceLemmaCatFile,
                    targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile) =
                    BuildModelTools.InitializeCreateParallelCorporaFiles(
                        true, false,
                        sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile,
                        targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);

                (sourceLemmaNoPuncFile, sourceIdNoPuncFile, sourceTextNoPuncFile, sourceLemmaCatNoPuncFile,
                 targetLemmaNoPuncFile, targetLemmaIdNoPuncFile, targetTextNoPuncFile, targetTextIdNoPuncFile) =
                    BuildModelTools.InitializeCreateParallelCorporaFiles(
                        true, true,
                        sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile,
                        targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);

            }
            else
            {
                (sourceLemmaNoPuncFile, sourceIdNoPuncFile, sourceTextNoPuncFile, sourceLemmaCatNoPuncFile,
                    targetLemmaNoPuncFile, targetLemmaIdNoPuncFile, targetTextNoPuncFile, targetTextIdNoPuncFile) =
                    BuildModelTools.InitializeCreateParallelCorporaFiles(
                        false, true,
                        sourceTextFile, sourceLemmaFile, sourceLemmaCatFile, sourceIdFile,
                        targetTextFile, targetTextIdFile, targetLemmaFile, targetLemmaIdFile);
            }

            // Create No Punctuation Files
            if (reuseParallelCorporaFiles &&
                File.Exists(sourceLemmaNoPuncFile) && File.Exists(sourceIdNoPuncFile) && File.Exists(sourceTextNoPuncFile) && File.Exists(sourceLemmaCatNoPuncFile) &&
                File.Exists(targetLemmaNoPuncFile) && File.Exists(targetLemmaIdNoPuncFile) && File.Exists(targetTextNoPuncFile) && File.Exists(targetTextIdNoPuncFile))
            {
                if (useContentWordsOnly)
                {
                    Console.WriteLine("  Reusing content words only and no punctuation parallel corpus files.");
                }
                else
                {
                    Console.WriteLine("  Reusing no punctuation parallel corpus files.");
                }
            }
            else
            {
                if (useContentWordsOnly)
                {
                    Console.WriteLine("  Creating content words only and no punctuation parallel corpus files.");
                }

                else
                {
                    Console.WriteLine("  Creating no punctuation parallel corpus files.");
                }

                ShowTime();

                Data.FilterOutWords(
                    sourceLemmaFile, sourceIdFile, sourceTextFile, sourceLemmaCatFile,
                    sourceLemmaNoPuncFile, sourceIdNoPuncFile, sourceTextNoPuncFile, sourceLemmaCatNoPuncFile,
                    puncs);
                Data.FilterOutWords(targetLemmaFile, targetLemmaIdFile, targetLemmaNoPuncFile, targetLemmaIdNoPuncFile, puncs);
                Data.FilterOutWords(targetTextFile, targetTextIdFile, targetTextNoPuncFile, targetTextIdNoPuncFile, puncs);

                ShowTime();
            }
        }

        private static void ShowTime()
        {
            DateTime dt = DateTime.Now;
            Console.WriteLine(dt.ToString("G"));
        }
    }
}
