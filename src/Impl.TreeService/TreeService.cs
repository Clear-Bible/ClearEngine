using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Utilities;


namespace ClearBible.Clear3.Impl.TreeService
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Miscellaneous;

    /// <summary>
    /// Represents a treebank.
    /// Implements ClearBible.Clear3.API.ITreeService, and
    /// supplies additional services to internal Clear3 code. 
    /// </summary>
    /// 
    public class TreeService : ITreeService
    {
        // Maps verse IDs to their trees, for those that
        // have been preloaded by PreloadTreesForChapter().
        //
        private Dictionary<VerseID, XElement> _trees2 =
            new Dictionary<VerseID, XElement>();

        // Identifies the chapter for which trees have
        // currently been preloaded.
        //
        private ChapterID _currentChapterID = ChapterID.None;

        // Folder where the XML files for the treebank reside.
        //
        private string _treeFolder;

        // Maps book number to bookname, as used in the
        // names of the XML files for the treebank.
        //
        private Dictionary<int, string> _bookNames;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="treeFolder">
        /// Folder where the XML files for the treebank reside.
        /// </param>
        /// <param name="bookNames">
        /// Maps book number to bookname, as used in the names
        /// of the XML files for the treebank.
        /// </param>
        /// 
        public TreeService(
            string treeFolder,
            Dictionary<int, string> bookNames)
        {
            _treeFolder = treeFolder;
            _bookNames = bookNames;
        }

        /// <summary>
        /// Preload trees for the specified chapter.  Also loads
        /// the trees for the preceding and following chapters, in
        /// case a verse range spans a chapter boundary.
        /// </summary>
        /// <param name="chapterID">
        /// Chapter for which trees are to be preloaded.
        /// </param>
        /// 
        public void PreloadTreesForChapter(ChapterID chapterID)
        {
            if (!chapterID.Equals(_currentChapterID))
            {
                int book = chapterID.Book;
                int chapter = chapterID.Chapter;

                _trees2 =
                    GetVerseTreesForChapter(book, chapter)
                    .Concat(GetVerseTreesForChapter(book, chapter + 1))
                    .Concat(GetVerseTreesForChapter(book, chapter - 1))
                    .ToDictionary(
                        x => new VerseID(
                            x.Attribute("nodeId").Value.Substring(0, 8)),
                        x => x);

                _currentChapterID = chapterID;
            }
        }


        /// <summary>
        /// Get the verse trees for the specified chapter by
        /// consulting the XML files in _treeFolder.
        /// </summary>
        /// 
        public IEnumerable<XElement> GetVerseTreesForChapter(
            int bookNumber,
            int chapterNumber)
        {
            if (chapterNumber >= 1)
            {
                string bookName = _bookNames[bookNumber];
                string treeFile = Path.Combine(
                    _treeFolder,
                    $"{bookName}{chapterNumber:D3}.trees.xml");

                if (File.Exists(treeFile))
                {
                    return
                        XElement.Load(treeFile)
                        .Descendants("Sentence")
                        .Select(s => s.Descendants("Node").First());
                }
            }
 
            return Enumerable.Empty<XElement>();
        }


        /// <summary>
        /// Get a tree node that covers a specified verse range.
        /// The result might be a newly constructed node to cover
        /// more than one verse.  Assumes that the verse range
        /// lies in the chapters that have been preloaded by
        /// PreloadTreesForChapter().
        /// </summary>
        /// <param name="start">Starting verse.</param>
        /// <param name="end">Ending verse.</param>
        /// 
        public XElement GetTreeNode(VerseID start, VerseID end)
        {
            List<XElement> verseTrees = GetVerseTrees(start, end).ToList();

            if (verseTrees.Count == 1)
            {
                return verseTrees[0];
            }
            else
            {
                return CombineTrees(verseTrees);
            }
        }


        /// <summary>
        /// Get the verse trees associated with a range of verses.
        /// Assumes that the range is part of what has been preloaded
        /// by PreloadTreesForChapter().
        /// </summary>
        /// <param name="start">Starting verse.</param>
        /// <param name="end">Ending verse.</param>
        /// <returns>
        /// Sequence of all of the verse trees within
        /// the specified range.
        /// </returns>
        /// 
        IEnumerable<XElement> GetVerseTrees(VerseID start, VerseID end)
        {
            int book = start.Book;

            if (start.Chapter == end.Chapter)
            {
                return GetChapterSubrange(book, start.Chapter, start.Verse, end.Verse);
            }
            else
            {
                return
                    GetChapterTail(book, start.Chapter, start.Verse)
                    .Concat(GetChapterSubrange(book, end.Chapter, 1, end.Verse));
            }
        }


        /// <summary>
        /// Get verse trees for a range that lies within a single
        /// chapter.
        /// </summary>
        /// 
        IEnumerable<XElement> GetChapterSubrange(
            int book,
            int chapter,
            int startVerse,
            int endVerse)
        {
            for (int i = startVerse; i <= endVerse; i++)
            {
                if (_trees2.TryGetValue(
                    new VerseID(book, chapter, i),
                    out XElement subTree))
                {
                    yield return subTree;
                }
                else
                {
                    yield break;
                }
            }
        }


        /// <summary>
        /// Get all of the verse trees from a chapter, starting with
        /// a specified verse and continuing to the end of the chapter.
        /// </summary>
        /// 
        IEnumerable<XElement> GetChapterTail(
            int book,
            int chapter,
            int startVerse)
        {
            for (int i = startVerse; true; i++)
            {
                if (_trees2.TryGetValue(
                    new VerseID(book, chapter, i),
                    out XElement subTree))
                {
                    yield return subTree;
                }
                else
                {
                    yield break;
                }
            }
        }


        /// <summary>
        /// Synthesize a new node for a list of verse trees.
        /// The children of the new node consist of all of the children
        /// of the verse trees, with the order preserved.  (The verse
        /// tree nodes themselves do not occur in the synthesized node.)
        /// </summary>
        /// 
        XElement CombineTrees(List<XElement> trees)
        {
            List<XElement> subTrees =
                trees.SelectMany(t => t.Elements()).ToList();

            int totalLength = subTrees
                .Select(x => Int32.Parse(x.Attribute("Length").Value))
                .Sum();

            string newNodeId =
                subTrees[0].Attribute("nodeId").Value.Substring(0, 11) +
                $"{totalLength:D3}";

            return
                new XElement("Node",
                    new XAttribute("Cat", "S"),
                    new XAttribute("Head", "0"),
                    new XAttribute("nodeId", newNodeId),
                    new XAttribute("Length", totalLength.ToString()),
                    subTrees);
        }


        public static void QueryTerminalNode(
            XElement terminalNode,
            out string sourceID,
            out string lemma,
            out string strong)
        {
            sourceID = terminalNode.Attribute("morphId").Value;
            if (sourceID.Length == 11)
            {
                sourceID += "1";
            }

            lemma = terminalNode.Attribute("UnicodeLemma").Value;
            strong = terminalNode.Attribute("Language").Value +
                terminalNode.Attribute("StrongNumberX").Value;
        }


        public static void Query2TerminalNode(
            XElement terminalNode,
            out string sourceID,
            out string surfaceForm)
        {
            sourceID = terminalNode.Attribute("morphId").Value;
            if (sourceID.Length == 11)
            {
                sourceID += "1";
            }

            surfaceForm = terminalNode.Attribute("Unicode").Value;
        }
    }
}
