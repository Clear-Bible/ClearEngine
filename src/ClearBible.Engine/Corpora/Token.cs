

namespace ClearBible.Engine.Corpora
{
    public class Token : IEquatable<Token>
    {
        protected Token(TokenId tokenId)
        {
            TokenId = tokenId;
        }

        public Token(TokenId tokenId, string surfaceText, string trainingText)
        {
            TokenId = tokenId;
            SurfaceText = surfaceText;
            TrainingText = trainingText;
            //Use = true;
        }

        public virtual TokenId TokenId { get; }
        public virtual string SurfaceText { get; set; } = "";

        public virtual string TrainingText { get; set; } = "";
        public int? Group { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Token token &&
                   TokenId == token.TokenId;
        }

        public override int GetHashCode()
        {
            return TokenId.GetHashCode();
        }

        public bool Equals(Token? other)
        {
            return Equals((object?)other);
        }

        //public bool Use { get; set; } //FIXME: not sure why I added this?!
    }
}
