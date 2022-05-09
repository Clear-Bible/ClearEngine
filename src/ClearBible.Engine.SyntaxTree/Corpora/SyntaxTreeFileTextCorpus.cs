using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.SyntaxTree.Corpora
{
    public class SyntaxTreeFileTextCorpus : ScriptureTextCorpus
    {
        public SyntaxTreeFileTextCorpus(ISyntaxTreeText syntaxTreeText)
        {
            Books = syntaxTreeText.GetBooks();
            Books
                .Select(book =>
                {
                    AddText(new SyntaxTreeFileText(syntaxTreeText, book, Versification));
                    return book;
                }).ToList();
        }

        protected IEnumerable<string> Books { get; init; }
        public override ScrVers Versification => ScrVers.Original;
    }
}
