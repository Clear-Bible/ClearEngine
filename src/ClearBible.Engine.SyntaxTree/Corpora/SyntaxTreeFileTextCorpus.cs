using SIL.Machine.Corpora;
using SIL.Scripture;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.SyntaxTree.Corpora
{

    public class SyntaxTreeFileTextCorpus : ScriptureTextCorpus
    {
        public SyntaxTreeFileTextCorpus(ISyntaxTreeText syntaxTreeText, LanguageCodeEnum? languageCodeEnum = null)
        {
            syntaxTreeText.GetBooks()
                .Where(b => languageCodeEnum != null ? BookIds  //if not null
                    .Where(bookId => bookId.silCannonBookAbbrev.Equals(b)) // where only books that match that language code.
                    .Select(bookId => bookId.languageCode)
                    .FirstOrDefault()
                    .Equals(languageCodeEnum)
                    : true // otherwise all
                )
                .Select(b =>
                {
                    AddText(new SyntaxTreeFileText(syntaxTreeText, b, Versification));
                    return b;
                }).ToList();
        }
        public override ScrVers Versification => ScrVers.Original;
    }
}
