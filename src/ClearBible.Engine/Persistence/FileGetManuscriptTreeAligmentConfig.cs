using ClearBible.Clear3.API;
using ClearBible.Clear3.SubTasks;
using ClearBible.Engine.Translation;

namespace ClearBible.Engine.Persistence
{
    public class FileGetManuscriptTreeAligmentConfig : IPersistGettable<FileGetManuscriptTreeAligmentConfig, ManuscriptWordAlignmentConfig>
    {
        public string? PathPrefix { get; private set; }

        string AddPathPrefix(string s) => Path.Combine(PathPrefix ?? "", s);

        public FileGetManuscriptTreeAligmentConfig()
        {
        }
        public override IPersistGettable<FileGetManuscriptTreeAligmentConfig, ManuscriptWordAlignmentConfig> SetLocation(string location)
        {
            PathPrefix = location;
            return this;
        }
        public override async Task<ManuscriptWordAlignmentConfig> GetAsync()
        {
            (List<string> puncs,
             List<string> stopWords,
             List<string> sourceFunctionWords,
             List<string> targetFunctionWords,
             TranslationModel manTransModel,
             Dictionary<string, int> goodLinks,
             Dictionary<string, int> badLinks,
             Dictionary<string, Gloss> glossTable,
             GroupTranslationsTable groups,
             Dictionary<string, Dictionary<string, string>> oldLinks,
             Dictionary<string, Dictionary<string, int>> strongs)
             =
             ImportAuxAssumptionsSubTask.Run(
             puncsPath: AddPathPrefix("puncs.txt"),
             stopWordsPath: AddPathPrefix("stopWords.txt"),
             sourceFuncWordsPath: AddPathPrefix("sourceFuncWords.txt"),
             targetFuncWordsPath: AddPathPrefix("targetFuncWords.txt"),
             manTransModelPath: AddPathPrefix("manTransModel.tsv"),
             goodLinksPath: AddPathPrefix("goodLinks.tsv"),
             badLinksPath: AddPathPrefix("badLinks.tsv"),
             glossTablePath: AddPathPrefix("Gloss.tsv"),
             groupsPath: AddPathPrefix("groups.tsv"),
             oldAlignmentPath: AddPathPrefix("oldAlignment.json"),
             strongsPath: AddPathPrefix("strongs.txt"));

            return await Task.Run(() => new ManuscriptWordAlignmentConfig(
                puncs,
                stopWords,
                sourceFunctionWords,
                targetFunctionWords,
                manTransModel,
                goodLinks,
                badLinks,
                glossTable,
                groups,
                oldLinks,
                strongs
            ));
        }
    }
}
