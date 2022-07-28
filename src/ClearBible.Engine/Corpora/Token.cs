

namespace ClearBible.Engine.Corpora
{
    public class Token
    {
        public Token(TokenId tokenId, string surfaceText, string trainingText)
        {
            TokenId = tokenId;
            SurfaceText = surfaceText;
            TrainingText = trainingText;
            //Use = true;
        }

        public TokenId TokenId { get; }
        public string SurfaceText { get; set; }

        public string TrainingText { get; set; }
        public int? Group { get; set; }
        //public bool Use { get; set; } //FIXME: not sure why I added this?!
    }
}
