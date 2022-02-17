using SIL.Machine.Corpora;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public class EngineManuscriptFileTextCorpus : ManuscriptFileTextCorpus, IEngineCorpus
    {
        public EngineManuscriptFileTextCorpus(IManuscriptText manuscriptCorpus) : base(manuscriptCorpus)
        {
           manuscriptCorpus.GetBooks()
                .Select(book =>
                {
                    var engineText = new EngineManuscriptFileText(manuscriptCorpus, book, Versification);
                    EngineTextDictionary[engineText.Id] = engineText;
                    return book;
                }).ToList();
        }
        protected Dictionary<string, IText> EngineTextDictionary { get; } = new Dictionary<string, IText>();
        public IText GetEngineText(string id)
        {
            if (EngineTextDictionary.TryGetValue(id, out IText? text))
                return text;
            return CreateNullText(id);
        }
    }
}
