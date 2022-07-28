using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    /// <summary>
    /// Base implementation sets Training to Surface.ToUpper(). 
    /// </summary>
    public class SetTrainingBySurfaceTokensTextRowProcessor : IRowProcessor<TextRow>
    {
        public TextRow Process(TextRow textRow)
        {
            if (textRow is not TokensTextRow)
            {
                throw new InvalidTypeEngineException(name: "textRow", value: "TokensTextRow", message: "not type");
            }

            //Transform the tokens, then set the result back on the Tokens property so the Segments are also changed.
            ((TokensTextRow) textRow).Tokens = ((TokensTextRow) textRow).Tokens.Select(t =>
            {
                t.TrainingText = GetTrainingText(t.SurfaceText);
                return t;
            }).ToList();
            return textRow;
        }

        protected virtual string GetTrainingText(string surfaceText)
        {
            return surfaceText.ToUpper();
        }
    }
}
