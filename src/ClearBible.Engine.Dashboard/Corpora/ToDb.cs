using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace ClearBible.Engine.Dashboard.Corpora
{
    public static class ToDb
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">StringTokenizer to apply to Paratext target corpus</typeparam>
        /// <param name="syntaxTreesPath"></param>
        /// <param name="targetParatextTextCorpusPath">path to target paratext project</param>
        /// <param name="connection">connection string to db</param>
        /// <param name="parallelCorpusId">primary key of ParallelCorpus db entity</param>
        /// <param name="engineVerseMappingList">null to use Machine versification, new() to initialize Engine versification from Machine and use 
        /// Engine versification (retrieved from EngineVerseMappingList property),
        /// or with values to use as versification.
        /// </param>
        /// <exception cref="InvalidDataException"></exception>
        public static void ManuscriptParatextParallelCorporaToDb<T>(
            string syntaxTreesPath, 
            string targetParatextTextCorpusPath,
            string connection, 
            int parallelCorpusId,
            List<EngineVerseMapping>? engineVerseMappingList = null) 
            where T : StringTokenizer, new() 
        {
            var manuscriptTree = new ManuscriptFileTree(syntaxTreesPath);
            var sourceCorpus = new ManuscriptFileTextCorpus(manuscriptTree)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>();

            var targetCorpus = new ParatextTextCorpus(targetParatextTextCorpusPath)
                .Tokenize<T>()
                .Transform<IntoTokensTextRowProcessor>();

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, engineVerseMappingList);

            foreach (EngineParallelTextRow textRow in parallelTextCorpus)
            {
                // (for 1-3, look in next method comments)
                // 4. iterate sourcetokens, inserting into Token with appropriate VerseId, CorpusId. 
                //   NOTE: for manuscript, sourceTokens should now be ManuscriptTokens, which inculde the extra data to INSERT into Adornment



                // tokenId.ToString() -> $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}"
                //source
                var sourceTokenIds = string.Join(" ", textRow.SourceTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");
                
                //target
                var targetTokenIds = string.Join(" ", textRow.TargetTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"TargetTokenIds: {targetTokenIds}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSourceStringTokenizer">StringTokenizer to apply to Paratext source corpus</typeparam>
        /// <typeparam name="TTargetStringTokenizer">StringTokenizer to apply to Paratext target corpus</typeparam>
        /// <param name="sourceParatextTextCorpusPath"></param>
        /// <param name="targetParatextTextCorpusPath"></param>
        /// <param name="connection">connection string to db</param>
        /// <param name="parallelCorpusId">primary key of ParallelCorpusId db entity</param>
        /// <param name="engineVerseMappingList">null to use Machine versification, new() to initialize Engine versification from Machine and use 
        /// Engine versification (retrieved from EngineVerseMappingList property),
        /// or with values to use as versification.
        /// </param>
        /// <exception cref="InvalidDataException"></exception>
        public static void ParatextParatextParallelCorporaToDb<TSourceStringTokenizer,TTargetStringTokenizer>(
            string sourceParatextTextCorpusPath,
            string targetParatextTextCorpusPath,
            string connection,
            int parallelCorpusId,
            List<EngineVerseMapping>? engineVerseMappingList = null)
            where TSourceStringTokenizer : StringTokenizer, new()
            where TTargetStringTokenizer : StringTokenizer, new()
        {
            var sourceCorpus = new ParatextTextCorpus(sourceParatextTextCorpusPath)
                .Tokenize<TSourceStringTokenizer>()
                .Transform<IntoTokensTextRowProcessor>();

            var targetCorpus = new ParatextTextCorpus(targetParatextTextCorpusPath)
                .Tokenize<TTargetStringTokenizer>()
                .Transform<IntoTokensTextRowProcessor>();

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, engineVerseMappingList);

            // PREREQUISITE: create ParallelCorpus per green line, which includes Corpus, getting back ParallelCorpusId and CorpaIds
            foreach (EngineParallelTextRow textRow in parallelTextCorpus)
            {
                // 1. INSERT into ParallelVerseLink, providing ParallelCorpusId and getting a ParallelVerseLink.Id
                // 2. * look in sourcetokens and find unique verseNumbers. INSERT those found into Verse and get VerseIds.
                // 3. Foreach VerseId, INSERT  into VerseLink providing VerseId and ParallelVerseLink.Id.
                // 4. iterate sourcetokens, inserting into Token with appropriate VerseId, CorpusId
                //
                /* 
                 * 
                var verseNums = textRow.SourceTokens
                    .GroupBy(t => t.TokenId.VerseNum)
                    .SelectMany(g => g.Select(t => t.TokenId.VerseNum));
                */
                var sourceTokenIds = string.Join(" ", textRow.SourceTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");
                
                //var targetVerseText = string.Join(" ", textRow.TargetSegment);
                var targetTokenIds = string.Join(" ", textRow.TargetTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"TargetTokenIds: {targetTokenIds}");
            }
        }
    }
}
