using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{
    /// <summary>
    /// Base implementation sets Training to Surface.ToUpper(). 
    /// </summary>
    public class SetTrainingTokensTextRowProcessor : IRowProcessor<TextRow>
    {
        public TextRow Process(TextRow textRow)
        {
            if (textRow is not TokensTextRow)
            {
                throw new InvalidTypeEngineException(name: "textRow", value: "TokensTextRow", message: "not type");
            }

            //Transform the tokens, then set the result back on the Tokens property so the Segments are also changed.
            ((TokensTextRow)textRow).Tokens
                .SelectMany(t =>
                    (t is CompositeToken) ?
                        ((CompositeToken)t).Tokens
                    :
                        new List<Token>() { t })
                .Select(t =>
                {
                    t.TrainingText = GetTrainingText(t.SurfaceText, t.TrainingText);
                    return t;
                })
                .ToList();
            ((TokensTextRow)textRow).Tokens = ((TokensTextRow)textRow).Tokens;
            return textRow;
        }

        protected virtual string GetTrainingText(string surfaceText, string trainingText)
        {
            return surfaceText;
        }
    }
}
