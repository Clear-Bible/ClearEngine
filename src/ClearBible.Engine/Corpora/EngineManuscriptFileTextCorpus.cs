using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;


namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileTextCorpus : ManuscriptFileTextCorpus, IEngineCorpus, IEngineTextConfig
    {
        public EngineManuscriptFileTextCorpus(IManuscriptText manuscriptCorpus) : base(manuscriptCorpus)
        {
           Books
                .Select(book =>
                {
                    var engineText = new EngineManuscriptFileText(manuscriptCorpus, book, Versification, this);
                    EngineTextDictionary[engineText.Id] = engineText;
                    return book;
                }).ToList();
        }

        public ITextSegmentProcessor? TextSegmentProcessor { get; set; } = null;
        public bool DoMachineVersification { get; set; } = true;
        protected Dictionary<string, IText> EngineTextDictionary { get; } = new Dictionary<string, IText>();
        public IText GetEngineText(string id)
        {
            if (EngineTextDictionary.TryGetValue(id, out IText? text))
                return text;
            return CreateNullText(id);
        }
    }
}
