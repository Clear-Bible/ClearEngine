using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileTextCorpus : ScriptureTextCorpus
    {
        public ManuscriptFileTextCorpus(IManuscriptText manuscriptText) : base(null)
        {
            Books = manuscriptText.GetBooks();
            Books
                .Select(book =>
                {
                    AddText(new ManuscriptFileText(manuscriptText, book, Versification));
                    return book;
                }).ToList();
        }

        protected IEnumerable<string> Books { get; init; }
        public override ScrVers Versification => ScrVers.Original;
    }
}
