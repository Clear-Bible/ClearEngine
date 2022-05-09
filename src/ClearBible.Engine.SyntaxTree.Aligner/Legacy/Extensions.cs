using System.Xml.Linq;

using ClearBible.Engine.SyntaxTree.Corpora;

namespace ClearBible.Engine.SyntaxTree.Aligner.Legacy
{
    internal static class Extensions
    {
        public static SourceID SourceID(this XElement term)
        {
            return new SourceID(term.MorphId());
        }
    }
}
