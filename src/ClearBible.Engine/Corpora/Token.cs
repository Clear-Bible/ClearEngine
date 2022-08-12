

using System.Text;

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

            Position = ulong.Parse(TokenId.ToString());
        }

        public virtual TokenId TokenId { get; }

        public virtual ulong Position { get; set; } = 0;

        public virtual string TrainingText { get; set; } = "";

        public virtual string SurfaceText { get; set; } = "";

        /// <summary>
        /// Cannot include SurfaceText within this string
        /// </summary>
        public virtual string SurfaceTextPrefix { get; set; } = "";

        /// <summary>
        /// Cannot include SurfaceText within this string
        /// </summary>
        public virtual string SurfaceTextSuffix { get; set; } = "";


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

        public override string ToString()
        {
            return $"{SurfaceTextPrefix}{SurfaceText}{SurfaceTextSuffix}";
        }
    }
}
