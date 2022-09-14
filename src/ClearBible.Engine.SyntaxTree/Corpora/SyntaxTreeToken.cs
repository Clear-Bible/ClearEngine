
using ClearBible.Engine.Corpora;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClearBible.Engine.SyntaxTree.Corpora
{
    public class SyntaxTreeToken : Token
    {
        public  SyntaxTreeToken(TokenId tokenId, string surface, string strong, string partsOfSpeech, /*string analysis, */string lemma, string english) : base(tokenId, surface, lemma)
        {
            Strong = strong;
            PartsOfSpeech = partsOfSpeech;
            English = english;
            //Analysis = analysis;
        }
        public string Strong { get; }
        public string PartsOfSpeech { get; }
        public string English { get; }
        //public string Analysis { get; }

        [JsonIgnore]
        public override string? PropertiesJson => JsonSerializer.Serialize(this);
    }
}
