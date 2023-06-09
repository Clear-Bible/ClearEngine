using ClearBible.Engine.Exceptions;
using System.Collections;

namespace ClearBible.Engine.Corpora
{
    public class CompositeToken : Token, IEnumerable<Token>
    {
        public readonly string CompositeTokensTextDelimiter = "_";

        private IEnumerable<Token> tokens_ = new List<Token>(); //will always be set by constructor. This is to suppress incorrect nullable warning for constructor.
        public IEnumerable<Token> Tokens 
        {
            get
            {
                return tokens_;
            }
            set 
            {
                if (value.OfType<CompositeToken>().Any())
                {
                    throw new InvalidParameterEngineException(name: "Tokens", value: "OfType<CompositeToken>().Any() == true", message: "Cannot contain any Tokens of type CompositeToken");
                }

                tokens_ = value
                    .OrderBy(t => t.Position)
                    .ToList();
                base.TrainingText = string.Join(CompositeTokensTextDelimiter, tokens_.Select(t => t.TrainingText));
                base.SurfaceText = string.Join(CompositeTokensTextDelimiter, tokens_.Select(t => t.SurfaceText));
            }
        }


        private IEnumerable<Token> otherTokens_ = new List<Token>(); 
        public IEnumerable<Token> OtherTokens
        {
            get
            {
                return otherTokens_;
            }
            set
            {
                if (value.OfType<CompositeToken>().Any())
                {
                    throw new InvalidParameterEngineException(name: "OtherTokens", value: "OfType<CompositeToken>().Any() == true", message: "Cannot contain any OtherTokens of type CompositeToken");
                }

                otherTokens_ = value;
            }
        }

        public CompositeToken(IEnumerable<Token> tokens, IEnumerable<Token>? otherTokens = null) : base(new CompositeTokenId(otherTokens == null ? tokens : tokens.Concat(otherTokens)))
        {
            Tokens = tokens;
            OtherTokens = otherTokens ?? new List<Token>();
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

        public override ulong Position
        {
            get
            {
                throw new EngineException("CompositeToken is not a positional token.");
            }
            set
            {
                throw new EngineException("CompositeToken is not a positional token.");
            }
        }
    }
}
