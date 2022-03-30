using ClearBible.Engine.Corpora;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    /// <summary>
    /// Changes TextRows into TokensTextRows.
    /// </summary>
    public class IntoTokensTextRowProcessor : IRowProcessor<TextRow>
    {
        public TextRow Process(TextRow textRow)
        {
            return new TokensTextRow(textRow);
        }
    }
}
