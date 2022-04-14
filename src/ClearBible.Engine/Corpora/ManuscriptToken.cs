
namespace ClearBible.Engine.Corpora
{
    public record BookSegment(string chapter, string verse, string text);
    public class ManuscriptToken : Token
    {
        public  ManuscriptToken(TokenId tokenId, string surface, string strong, string partsOfSpeech, /*string analysis, */string lemma) : base(tokenId, lemma)
        {
            Surface = surface;
            Strong = strong;
            PartsOfSpeech = partsOfSpeech;
            //Analysis = analysis;
        }
        public string Surface { get; }
        public string Strong { get; }
        public string PartsOfSpeech { get; }
        //public string Analysis { get; }
    }
}
