using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public record CorpusId
    {
        public CorpusId(int id)
        {
            Id = id;
        }

        public CorpusId(string corpusUriIdentifier)
        {
            Id = corpusUriIdentifier.AsInt("corpusUri.Identifier");
        }

        public int Id { get; }
    }
}
