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
    using System.Runtime.CompilerServices;
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
                $"{totalLength:D3}" +
                "0";

            return
                new XElement("Node",
                    new XAttribute("Cat", "S"),
                    new XAttribute("Head", "0"),
                    new XAttribute("nodeId", newNodeId),
                    new XAttribute("Length", totalLength.ToString()),
                    subTrees);
        }
    }


    /// <summary>
    /// Extension methods to be applied to an XElement that is
    /// a node in the syntax tree.
    /// </summary>
    /// 
    public static class TreeNodeExtensions
    {
        public static TreeNodeID TreeNodeID(this XElement node)
        {
            return new TreeNodeID(node.Attribute("nodeId").Value);
        }

        public static TreeNodeStackID TreeNodeStackID(this XElement node)
        {
            return node.TreeNodeID().TreeNodeStackID;
        }
    }


    /// <summary>
    /// Extension methods to be applied to an XElement that is
    /// a terminal node in the syntax tree.
    /// </summary>
    /// 
    public static class TerminalNodeExtensions
    {
        public static SourceID SourceID(this XElement term)
        {
            string sourceIDTag = term.Attribute("morphId").Value;
            if (sourceIDTag.Length == 11)
            {
                sourceIDTag += "1";
            }
            return new SourceID(sourceIDTag);
        }

        public static string Lemma(this XElement term) =>
            term.Attribute("UnicodeLemma").Value;

        public static string Surface(this XElement term) =>
            term.Attribute("Unicode").Value;

        public static string Strong(this XElement term) =>
            term.Attribute("Language").Value +
            term.Attribute("StrongNumberX").Value;

        //public string English { get; }
        //public string Category { get; }
        //public string PartOfSpeech { get; }
        //public string Morphology { get; }
    }






    /// <summary>
    /// Identifies a node in the tree by describing the position of
    /// its first terminal, the number of terminals, and the level of
    /// the node within the stack of such nodes.
    /// </summary>
    /// 
    public struct TreeNodeID
    {
        /// <summary>
        /// The tag is a string of decimal digits of the form
        /// BBCCCVVVPPPSSSL, where BBCCCVVV identifies the verse (as in
        /// a canonical VerseID string), PPP is the position, SSS is the
        /// span, and L is the level.
        /// </summary>
        /// 
        private string _tag;

        public string AsCanonicalString => _tag;

        public TreeNodeID(string canonicalString)
        {
            _tag = canonicalString;
        }

        public TreeNodeID(
            int book,
            int chapter,
            int verse,
            int position,
            int span,
            int level)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{position:D3}{span:D3}{level:D1}";
        }

        public TreeNodeID(
            VerseID verseID,
            int position,
            int span,
            int level)
        {
            _tag = $"{verseID.Book:D2}{verseID.Chapter:D3}{verseID.Verse:D3}{position:D3}{span:D3}{level:D1}";
        }

        /// <summary>
        /// The verse containing the first terminal under the node.
        /// </summary>
        /// 
        public VerseID VerseID =>
            new VerseID(_tag.Substring(0, 8));

        /// <summary>
        /// The 1-based position of the first terminal under the node
        /// within the verse.
        /// </summary>
        /// 
        public int Position => int.Parse(_tag.Substring(8, 3));

        /// <summary>
        /// The number of terminals under the node.
        /// </summary>
        /// 
        public int Span => int.Parse(_tag.Substring(11, 3));

        /// <summary>
        /// The 0-based position of this node in a stack of nodes
        /// that all have the same verse, start, and span.  Each node
        /// except the bottom one has just one child, which is the node
        /// below it in the stack.  The position is counted from the
        /// bottom of the stack, nearest the leaves of the tree.
        /// </summary>
        /// 
        public int Level => int.Parse(_tag.Substring(14, 1));

        public TreeNodeStackID TreeNodeStackID =>
            new TreeNodeStackID(_tag.Substring(0, 14));
    }


    /// <summary>
    /// Identifies a stack of tree nodes.  Each node in the stack
    /// has the same VerseID, Position, and Span.  Each node except
    /// the bottom one has just one child, which is the node below
    /// it in the stack.
    /// </summary>
    /// 
    public struct TreeNodeStackID
    {
        /// <summary>
        /// The tag is a string of decimal digits of the form
        /// BBCCCVVVPPPSSS, where BBCCCVVV identifies the verse (as in
        /// a canonical VerseID string), PPP is the position, and SSS is
        /// the span.
        /// </summary>
        /// 
        private string _tag;

        public string AsCanonicalString => _tag;

        public TreeNodeStackID(string canonicalString)
        {
            _tag = canonicalString;
        }

        public TreeNodeStackID(
            int book,
            int chapter,
            int verse,
            int position,
            int span)
        {
            _tag = $"{book:D2}{chapter:D3}{verse:D3}{position:D3}{span:D3}";
        }

        public TreeNodeStackID(
            VerseID verseID,
            int position,
            int span)
        {
            _tag = $"{verseID.Book:D2}{verseID.Chapter:D3}{verseID.Verse:D3}{position:D3}{span:D3}";
        }

        /// <summary>
        /// The verse containing the first terminal under the node.
        /// </summary>
        /// 
        public VerseID VerseID =>
            new VerseID(_tag.Substring(0, 8));

        /// <summary>
        /// The 1-based position of the first terminal within
        /// the verse.
        /// </summary>
        /// 
        public int Position => int.Parse(_tag.Substring(8, 3));

        /// <summary>
        /// The number of terminals under the node.
        /// </summary>
        /// 
        public int Span => int.Parse(_tag.Substring(11, 3));
    }
}
