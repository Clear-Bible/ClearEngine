using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Service;

namespace ClearBible.Clear3.SubTasks
{
    public class ImportAuxAssumptionsSubTask
    {
        public record Result(
            List<string> Puncs,
            List<string> Stopwords,
            List<string> SourceFunctionWords,
            List<string> TargetFunctionWords,
            TranslationModel ManTransModel,
            Dictionary<string, int> GoodLinks,
            Dictionary<string, int> BadLinks,
            Dictionary<string, Gloss> GlossTable,
            GroupTranslationsTable Groups,
            Dictionary<string, Dictionary<string, string>> OldLinks,
            Dictionary<string, Dictionary<string, int>> Strongs);

        public static Result Run(
            string puncsPath,
            string stopWordsPath,
            string sourceFuncWordsPath,
            string targetFuncWordsPath,
            string manTransModelPath,
            string goodLinksPath,
            string badLinksPath,
            string glossTablePath,
            string groupsPath,
            string oldAlignmentPath,
            string strongsPath)
        {
            IImportExportService importExportService =
                Clear30Service.FindOrCreate().ImportExportService;

            // 2021.05.26 CL: Changed to make these files optional. If they don't exist, return empty datastructure.
            // It is possible to make this part of each importExportService function instead with a bool parameter (as in Clear2) which may be cleaner.
            // But for now, I don't want to change the Interface so I'll do it here.

            var puncs = new List<string>();
            var stopWords = new List<string>();
            var sourceFuncWords = new List<string>();
            var targetFuncWords = new List<string>();
            var manTransModel = new TranslationModel(new Dictionary<SourceLemma, Dictionary<TargetLemma, Score>>());
            var groups = new GroupTranslationsTable(new Dictionary<SourceLemmasAsText, HashSet<TargetGroup>>());
            var goodLinks = new Dictionary<string, int>();
            var badLinks = new Dictionary<string, int>();
            var glossTable = new Dictionary<string, Gloss>();
            var oldLinks = new Dictionary<string, Dictionary<string, string>>();
            var strongs = new Dictionary<string, Dictionary<string, int>>();

            if (File.Exists(puncsPath)) puncs = importExportService.GetWordList(puncsPath);
            if (File.Exists(stopWordsPath)) stopWords = importExportService.GetStopWords(stopWordsPath);
            if (File.Exists(sourceFuncWordsPath)) sourceFuncWords = importExportService.GetWordList(sourceFuncWordsPath);
            if (File.Exists(targetFuncWordsPath)) targetFuncWords = importExportService.GetWordList(targetFuncWordsPath);
            if (File.Exists(manTransModelPath))
            {
                var manTransModelOrig = importExportService.GetTranslationModel2(manTransModelPath);

                manTransModel =
                new TranslationModel(
                    manTransModelOrig.ToDictionary(
                        kvp => new SourceLemma(kvp.Key),
                        kvp => kvp.Value.ToDictionary(
                            kvp2 => new TargetLemma(kvp2.Key),
                            kvp2 => new Score(kvp2.Value.Prob))));
            }

            if (File.Exists(goodLinksPath)) goodLinks = importExportService.GetXLinks(goodLinksPath);
            if (File.Exists(badLinksPath)) badLinks = importExportService.GetXLinks(badLinksPath);
            if (File.Exists(glossTablePath)) glossTable = importExportService.BuildGlossTableFromFile(glossTablePath);
            if (File.Exists(groupsPath)) groups = importExportService.ImportGroupTranslationsTable(groupsPath);
            if (File.Exists(oldAlignmentPath)) oldLinks = importExportService.GetOldLinks(oldAlignmentPath, groups);
            if (File.Exists(strongsPath)) strongs = importExportService.BuildStrongTable(strongsPath);

            return new Result(
                puncs,
                stopWords,
                sourceFuncWords,
                targetFuncWords,
                manTransModel,
                goodLinks,
                badLinks,
                glossTable,
                groups,
                oldLinks,
                strongs);
        }
    }
}
