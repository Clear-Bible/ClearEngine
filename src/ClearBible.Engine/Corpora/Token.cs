

namespace ClearBible.Engine.Corpora
{
    public class Token
    {
        public Token(TokenId tokenId, string text)
        {
            TokenId = tokenId;
            Text = text;
            //Use = true;
        }

        public TokenId TokenId { get; }
        public string Text { get; }
        public int? Group { get; set; }
        //public bool Use { get; set; } //FIXME: not sure why I added this?!
    }
}
