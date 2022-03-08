using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileTextCorpus : ScriptureTextCorpus
    {
        public ManuscriptFileTextCorpus(IManuscriptText manuscriptCorpus) : base(null)
        {
            Books = manuscriptCorpus.GetBooks();
            Books
                .Select(book =>
                {
                    AddText(new ManuscriptFileText(manuscriptCorpus, book, Versification));
                    return book;
                }).ToList();
        }

        protected IEnumerable<string> Books { get; init; }
        public override ScrVers Versification => ScrVers.Original;
    }
}
