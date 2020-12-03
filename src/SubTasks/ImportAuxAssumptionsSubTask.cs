using System;
using System.Collections.Generic;
using System.Linq;

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


            List<string> puncs = importExportService.GetWordList(puncsPath);

            List<string> stopWords = importExportService.GetStopWords(stopWordsPath);

            List<string> sourceFuncWords = importExportService.GetWordList(sourceFuncWordsPath);
            List<string> targetFuncWords = importExportService.GetWordList(targetFuncWordsPath);

            Dictionary<string, Dictionary<string, Stats>> manTransModelOrig =
                importExportService.GetTranslationModel2(manTransModelPath);
           
            TranslationModel manTransModel =
                new TranslationModel(
                    manTransModelOrig.ToDictionary(
                        kvp => new Lemma(kvp.Key),
                        kvp => kvp.Value.ToDictionary(
                            kvp2 => new TargetText(kvp2.Key),
                            kvp2 => new Score(kvp2.Value.Prob))));

            Dictionary<string, int> goodLinks = importExportService.GetXLinks(goodLinksPath);
            Dictionary<string, int> badLinks = importExportService.GetXLinks(badLinksPath);
           
            Dictionary<string, Gloss> glossTable =
                importExportService.BuildGlossTableFromFile(glossTablePath);

            GroupTranslationsTable groups =
                importExportService.ImportGroupTranslationsTable(groupsPath);

            Dictionary<string, Dictionary<string, string>> oldLinks =
                importExportService.GetOldLinks(oldAlignmentPath, groups);

            Dictionary<string, Dictionary<string, int>> strongs =
                importExportService.BuildStrongTable(strongsPath);

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
