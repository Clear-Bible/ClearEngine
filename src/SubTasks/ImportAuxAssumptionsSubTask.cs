using System;
using System.Collections.Generic;
using System.Linq;

namespace ClearBible.Clear3.SubTasks
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.APIImportExport;
    using ClearBible.Clear3.ServiceImportExport;

    // FIXME: Put this in the API.
    using Stats = ClearBible.Clear3.Impl.Data.Stats;

    // FIXME: Get rid of this dependency on AlignmentTool.
    using Data = AlignmentTool.Data;

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
            //IClear30ServiceAPI clearService =
            //    Clear30Service.FindOrCreate();

            IClear30ServiceAPIImportExport importExportService =
                Clear30ServiceImportExport.Create();


            List<string> puncs = Data.GetWordList(puncsPath);

            List<string> stopWords = Data.GetStopWords(stopWordsPath);

            List<string> sourceFuncWords = Data.GetWordList(sourceFuncWordsPath);
            List<string> targetFuncWords = Data.GetWordList(targetFuncWordsPath);

            Dictionary<string, Dictionary<string, Stats>> manTransModelOrig =
                Data.GetTranslationModel2(manTransModelPath);
           
            TranslationModel manTransModel =
                new TranslationModel(
                    manTransModelOrig.ToDictionary(
                        kvp => new Lemma(kvp.Key),
                        kvp => kvp.Value.ToDictionary(
                            kvp2 => new TargetText(kvp2.Key),
                            kvp2 => new Score(kvp2.Value.Prob))));

            Dictionary<string, int> goodLinks = Data.GetXLinks(goodLinksPath);
            Dictionary<string, int> badLinks = Data.GetXLinks(badLinksPath);
           
            Dictionary<string, Gloss> glossTable =
                Data.BuildGlossTableFromFile(glossTablePath);

            GroupTranslationsTable groups =
                importExportService.ImportGroupTranslationsTable(groupsPath);

            Dictionary<string, Dictionary<string, string>> oldLinks =
                Data.GetOldLinks(oldAlignmentPath, groups);

            Dictionary<string, Dictionary<string, int>> strongs =
                Data.BuildStrongTable(strongsPath);

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
