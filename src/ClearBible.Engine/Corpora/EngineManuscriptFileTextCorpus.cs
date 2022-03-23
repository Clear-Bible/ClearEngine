using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;


namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileTextCorpus : ManuscriptFileTextCorpus, IEngineCorpus
    {
        public EngineManuscriptFileTextCorpus(IManuscriptText manuscriptText) : base(manuscriptText)
        {
           Books
                .Select(book =>
                {
                    AddText(new EngineManuscriptFileText(manuscriptText, book, Versification, this));
                    return book;
                }).ToList();
        }

        public BaseTextSegmentProcessor? TextSegmentProcessor { get; set; } = null;
        public bool DoMachineVersification { get; set; } = true;

        public void Train(ParallelTextCorpus parallelTextCorpus, ITextCorpus textCorpus)
        {
            TextSegmentProcessor?.Train(parallelTextCorpus, textCorpus);
        }
    }
}
