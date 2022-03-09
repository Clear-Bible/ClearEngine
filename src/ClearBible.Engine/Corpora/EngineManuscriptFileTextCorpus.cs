using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;


namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileTextCorpus : ManuscriptFileTextCorpus, IEngineCorpus
    {
        public EngineManuscriptFileTextCorpus(IManuscriptText manuscriptCorpus) : base(manuscriptCorpus)
        {
           Books
                .Select(book =>
                {
                    AddText(new EngineManuscriptFileText(manuscriptCorpus, book, Versification, this));
                    return book;
                }).ToList();
        }

        public ITextSegmentProcessor? TextSegmentProcessor { get; set; } = null;
        public bool DoMachineVersification { get; set; } = true;
    }
}
