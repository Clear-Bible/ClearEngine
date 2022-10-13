using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;

namespace ClearBible.Engine.Tokenization
{ 
    public class AddEExtendedPropertiesToTokens : IRowProcessor<TextRow>
    {
        private IExtendedPropertiesSource _extendedPropertiesSource;
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
                    var extendedPropertiesString = _extendedPropertiesSource.GetExtendedPropertiesObjectForToken(t.TokenId);
                    if (extendedPropertiesString != null)
                    {
                        t.AddToExtendedProperties(extendedPropertiesString);
                    }
                    return t;
                })
                .ToList();
            return textRow;
        }

        protected AddEExtendedPropertiesToTokens(IExtendedPropertiesSource extendedPropertiesSource)
        {
            _extendedPropertiesSource = extendedPropertiesSource;
        }
    }
}
