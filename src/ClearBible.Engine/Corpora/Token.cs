using System.Text.Json.Serialization;

namespace ClearBible.Engine.Corpora
{
    public class Token : IEquatable<Token>
    {
        private string? _propertiesJson = null;
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

        [JsonIgnore]
        public virtual TokenId TokenId { get; }

        [JsonIgnore]
        public virtual ulong Position { get; set; } = 0;

        [JsonIgnore]
        public virtual string TrainingText { get; set; } = "";

        [JsonIgnore]
        public virtual string SurfaceText { get; set; } = "";

        [JsonIgnore]
        /// <summary>
        /// Cannot include SurfaceText within this string
        /// </summary>
        public virtual string SurfaceTextPrefix { get; set; } = "";

        [JsonIgnore]
        /// <summary>
        /// Cannot include SurfaceText within this string
        /// </summary>
        public virtual string SurfaceTextSuffix { get; set; } = "";

        [JsonIgnore]
        public string TokenType
        {
            get
            {
                return GetType().AssemblyQualifiedName!;
            }
        }
        [JsonIgnore]
        public virtual string? PropertiesJson
        {
            set
            {
                if (GetType() == typeof(Token))
                {
                    _propertiesJson = value;
                }
            }
            get
            {
                if (GetType() == typeof(Token) && _propertiesJson != null)
                {
                    return _propertiesJson;
                }
                else
                {
                    return null;
                }
            }
        }
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
