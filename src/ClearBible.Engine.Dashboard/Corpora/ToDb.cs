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
                //FIXME: put these into db instead of writing to console.

                //verse
                var verseRef = (VerseRef) textRow.Ref;

                // tokenId.ToString() -> $"{BookNum.ToString("000")}{ChapterNum.ToString("000")}{VerseNum.ToString("000")}{WordNum.ToString("000")}{SubWordNum.ToString("000")}"
                //source
                var sourceVerseText = string.Join(" ", textRow.SourceSegment);
                var sourceTokenIds = string.Join(" ", textRow.SourceTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");
                
                //target
                var targetVerseText = string.Join(" ", textRow.TargetSegment);
                var targetTokenIds = string.Join(" ", textRow.TargetTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"TargetTokenIds: {targetTokenIds}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">StringTokenizer to apply to Paratext source corpus</typeparam>
        /// <typeparam name="U">StringTokenizer to apply to Paratext target corpus</typeparam>
        /// <param name="sourceParatextTextCorpusPath"></param>
        /// <param name="targetParatextTextCorpusPath"></param>
        /// <param name="connection">connection string to db</param>
        /// <param name="parallelCorpusId">primary key of ParallelCorpusId db entity</param>
        /// <param name="engineVerseMappingList">null to use Machine versification, new() to initialize Engine versification from Machine and use 
        /// Engine versification (retrieved from EngineVerseMappingList property),
        /// or with values to use as versification.
        /// </param>
        /// <exception cref="InvalidDataException"></exception>
        public static void ParatextParatextParallelCorporaToDb<T,U>(
            string sourceParatextTextCorpusPath,
            string targetParatextTextCorpusPath,
            string connection,
            int parallelCorpusId,
            List<EngineVerseMapping>? engineVerseMappingList = null)
            where T : StringTokenizer, new()
            where U : StringTokenizer, new()
        {
            var sourceCorpus = new ParatextTextCorpus(sourceParatextTextCorpusPath)
                .Tokenize<T>()
                .Transform<IntoTokensTextRowProcessor>();

            var targetCorpus = new ParatextTextCorpus(targetParatextTextCorpusPath)
                .Tokenize<U>()
                .Transform<IntoTokensTextRowProcessor>();

            var parallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, engineVerseMappingList);

            foreach (EngineParallelTextRow textRow in parallelTextCorpus)
            {
                //FIXME: put these into db instead of writing to console.

                //verse
                var verseRef = (VerseRef)textRow.Ref;

                //source 
                var sourceVerseText = string.Join(" ", textRow.SourceSegment);
                var sourceTokenIds = string.Join(" ", textRow.SourceTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"SourceTokenIds: {sourceTokenIds}");
                
                var targetVerseText = string.Join(" ", textRow.TargetSegment);
                var targetTokenIds = string.Join(" ", textRow.TargetTokens?
                    .Select(token => token.TokenId.ToString()) ?? throw new InvalidDataException());
                Console.WriteLine($"TargetTokenIds: {targetTokenIds}");
            }
        }
    }
}
