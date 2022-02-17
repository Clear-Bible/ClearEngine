using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    /// <summary>
    /// Implementers can be used by ManuscriptFileText to obtain manuscript Texts.
    /// </summary>
    public interface IManuscriptText
    {
        public record BookSegment(string chapter, string verse, string text);
        IEnumerable<string> GetBooks();
        IEnumerable<BookSegment> GetBookSegments(string bookAbbreviation, bool includeText);
    }
}
