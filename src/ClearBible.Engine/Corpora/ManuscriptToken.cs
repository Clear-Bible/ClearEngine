
namespace ClearBible.Engine.Corpora
{
    public record BookSegment(string chapter, string verse, string text);
    public class ManuscriptToken : Token
    {
        public  ManuscriptToken(TokenId tokenId, string surface, string strong, string category, string analysis, string lemma) : base(tokenId, lemma)
        {
            Surface = surface;
            Strong = strong;
            Category = category;
            Analysis = analysis;
        }
        public string Surface { get; }
        public string Strong { get; }
        public string Category { get; }
        public string Analysis { get; }
    }
}
