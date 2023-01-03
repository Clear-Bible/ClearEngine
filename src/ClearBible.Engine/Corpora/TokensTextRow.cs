
using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class TokensTextRow : TextRow
    {
        private List<Token>? tokens_;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segmentRef"></param>
        /// <returns>A tuple of SIL Book abbreviation, chapter number, verse number</returns>
        public static (string, int, int) GetBookChapterVerse(object segmentRef)
        {
            return (
                ((VerseRef)segmentRef).Book,
                ((VerseRef)segmentRef).ChapterNum,
                ((VerseRef)segmentRef).VerseNum);
        }

        /// <summary>
        /// ONLY USED INTERNAL TO ENGINE: Used by EngineParallelTextCorpus when joining TextRows together from versification. 
        /// </summary>
        /// <param name="rowRef"></param>
        /// <param name="tokens"></param>
        public TokensTextRow(object rowRef, IReadOnlyList<Token>? tokens = null) : base(rowRef)
        {
            Tokens = tokens?.ToList() ?? new List<Token>();
            //Implementation should not copy OriginalText because there is no such thing when joining verses together.
        }

        /// <summary>
        /// ONLY USED INTERNAL TO ENGINE: Used by IntoTokensTextRowProcessor.
        /// </summary>
        /// <param name="textRow"></param>
        public TokensTextRow(TextRow textRow)
            : base(textRow.Ref)
        {
            IsSentenceStart = textRow.IsSentenceStart;
            IsInRange = textRow.IsInRange;
            IsRangeStart = textRow.IsRangeStart;
            IsEmpty = textRow.IsEmpty;

            (string bookAbbreviation, int chapterNumber, int verseNumber) = GetBookChapterVerse(Ref);

            Tokens = textRow.Segment
                .Select((stringToken, index) => new Token(new TokenId(bookAbbreviation, chapterNumber, verseNumber, index + 1, 1), stringToken, stringToken))
                .ToList();
            OriginalText = textRow.OriginalText;
        }

        /// <summary>
        /// Use to convert from TextRow to TokensTextRow. Used internally in engine by SyntaxTree.
        /// </summary>
        /// <param name="textRow"></param>
        /// <param name="tokens"></param>
        public TokensTextRow(TextRow textRow, IReadOnlyList<Token> tokens)
            : base(textRow.Ref)
        {
            IsSentenceStart = textRow.IsSentenceStart;
            IsInRange = textRow.IsInRange;
            IsRangeStart = textRow.IsRangeStart;
            IsEmpty = false;
            Tokens = tokens.ToList();
            //No OriginalText for SyntaxTrees: they are pre-tokenized and not versioned
        }

        public List<Token> Tokens {
            get
            {
                return tokens_
                    ?? new List<Token>();
            }
            set
            {
                // if there are Tokens with duplicate tokenIds
                tokens_ = value
                    .PackComposites()
                    .ToList();
                Segment = tokens_
                    .Where(t => (t is CompositeToken) ? ((CompositeToken)t).OtherTokens.Count() == 0 : true)
                    .Select(t => t.TrainingText)
                    .ToList();
            }
        }
    }
}
