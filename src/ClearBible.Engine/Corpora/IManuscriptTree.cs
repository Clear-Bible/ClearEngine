using System.Xml.Linq;

namespace ClearBible.Engine.Corpora
{
    //Implementers can be used by TreeAligner to obtain manuscript tree nodes.
    public interface IManuscriptTree
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">Three character book abbreviation (SIL)</param>
        /// <param name="chapter"></param>
        /// <param name="verses"></param>
        /// <returns></returns>
        XElement? GetTreeNode(string book, int chapter, List<int> verses);
    }
}
