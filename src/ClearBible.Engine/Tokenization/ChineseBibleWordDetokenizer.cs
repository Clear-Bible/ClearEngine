using SIL.Machine.Tokenization;

namespace ClearBible.Engine.Tokenization
{
    /// <summary>
    /// Since punctuation like comma, question marks, etc., are basically not native to Chinese but instead
    /// concepts taken from latin languages, treat punctuation as for latin language. 
    /// Even if Chinese differs in terms of adding spaces, e.g. a space before a begin quote, a Chinese reader can still read it.
    /// </summary>
    public class ChineseBibleWordDetokenizer : LatinWordDetokenizer
    {
        protected override DetokenizeOperation GetOperation(object ctxt, string token)
        {
            var op = base.GetOperation(ctxt, token);
            if (op == DetokenizeOperation.NoOperation) //not punctuation or special characters so treat as word 
            {                                          //and MergeBoth so no space separator is added between words.
                return DetokenizeOperation.MergeBoth;
            }
            else
                return op; //otherwise, since other punctuation is basically taken from latin languages by chinese,
                            // treat them like latin languages. In some cases this may not be how Chinese typically displays it,
                            // e.g. maybe no space before a begin quote, but a Chinese reader can still read it if spaces
                            // added as for latin languages even if not typically added for Chinese.
        }
    }
}
