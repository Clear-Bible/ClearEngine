using ClearBible.Engine.Persistence;
using ClearBible.Engine.TreeAligner.Translation;

using ClearBible.Engine.TreeAligner.Legacy;

namespace ClearBible.Engine.TreeAligner.Persistence
{
    public class FileGetManuscriptTreeAlignerParams : IPersistGettable<FileGetManuscriptTreeAlignerParams, ManuscriptTreeWordAlignerParams>
    {
        public string? PathPrefix { get; private set; }

        string AddPathPrefix(string s) => Path.Combine(PathPrefix ?? "", s);

        public FileGetManuscriptTreeAlignerParams()
        {
        }
        public override IPersistGettable<FileGetManuscriptTreeAlignerParams, ManuscriptTreeWordAlignerParams> SetLocation(string location)
        {
            PathPrefix = location;
            return this;
        }
        public override async Task<ManuscriptTreeWordAlignerParams> GetAsync()
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

            return await Task.Run(() => new ManuscriptTreeWordAlignerParams(
                strongs,
                glossTable,
                oldLinks,
                goodLinks,
                badLinks,
                sourceFunctionWords,
                targetFunctionWords,
                stopWords,
                puncs,
                manTransModel,
                groups
            ));
        }
    }
}
