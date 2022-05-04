using System.Xml.Linq;

using ClearBible.Engine.Corpora;

namespace ClearBible.Engine.TreeAligner.Legacy
{
    internal static class Extensions
    {
        public static SourceID SourceID(this XElement term)
        {
            return new SourceID(term.MorphId());
        }
    }
}
