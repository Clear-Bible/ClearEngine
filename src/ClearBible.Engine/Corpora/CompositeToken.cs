using ClearBible.Engine.Exceptions;
using System.Collections;

namespace ClearBible.Engine.Corpora
{
    public class CompositeToken : Token, IEnumerable<Token>
    {
        public readonly string CompositeTokensTrainingTextDelimiter = "_";

        private IEnumerable<Token> tokens_ = new List<Token>(); //will always be set by constructur. This is to suppress incorrect nullable warning for constructor.
        public IEnumerable<Token> Tokens 
        {
            get
            {
                return tokens_;
            }
            set 
            {
                tokens_ = value;
                base.TrainingText = string.Join(CompositeTokensTrainingTextDelimiter, tokens_.Select(t => t.TrainingText));

            }
        }

        public CompositeToken(IEnumerable<Token> tokens) : base(new CompositeTokenId(tokens))
        {
            if (tokens.OfType<CompositeToken>().Any())
            {
                throw new InvalidParameterEngineException(name: "tokens", value: "OfType<CompositeToken>().Any() == true", message: "Cannot contain any tokens of type CompositeToken");
            }

            Tokens = tokens;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            return Tokens.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public override string SurfaceText
        {
            get
            {
                throw new EngineException("Cannot get surface text from type CompositeToken. Retrieve surface text directly from composed tokens available by enumerating this.");
            }
            set
            {
                throw new EngineException("Cannot set surface text for type CompositeToken.");
            }
        }

        public override string TrainingText
        {
            set
            {
                throw new EngineException("Cannot set training text directly on type CompositeToken. CompositeToken's Training text can be set by setting this.Tokens.");
            }
        }
    }
}
