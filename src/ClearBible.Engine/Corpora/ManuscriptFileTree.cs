using ClearBible.Clear3.API;
using ClearBible.Clear3.SubTasks;
using ClearBible.Engine.Persistence;
using ClearBible.Engine.Translation;
using SIL.Machine.Corpora;
using System.Xml.Linq;
using static ClearBible.Engine.Corpora.IManuscriptText;

namespace ClearBible.Engine.Corpora
{
    public class ManuscriptFileTree : IManuscriptText, IManuscriptTree
    {
        private readonly string _manuscriptTreesPath;

        public static string ConvertToSILBookAbbreviation(string fileNameBookPrefix)
        {
            var hasMapping = Mappings.ManuscriptFileBookToSILBookPrefixes.TryGetValue(fileNameBookPrefix, out var result);
            if (hasMapping)
            {
                return result != null ? result.code : throw new NullReferenceException($"Mapping.ManuscriptFileBookToSILBookPrefixes[{fileNameBookPrefix}] = null");
            }
            else
            {
                throw new KeyNotFoundException($"Mapping.ManuscriptFileBookToSILBookPrefixes[{fileNameBookPrefix}] doesn't exist.");
            }
        }

        public ManuscriptFileTree(string manuscriptTreesPath)
        {
            _manuscriptTreesPath = manuscriptTreesPath;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns the books that exist in the syntax trees in three letter SIL abbreviation form.</returns>
        public IEnumerable<string> GetBooks()
        {
            return Directory.EnumerateFiles(_manuscriptTreesPath, "*.xml")
                .Select(fileName => ConvertToSILBookAbbreviation(
                    fileName.Trim().Substring(_manuscriptTreesPath.Length + 1, fileName.Length - _manuscriptTreesPath.Length - 1 - 13)))
                //names are  in b[bb]ccc.trees.xml format, and we want the b[bb] part, therefore subtracting
                // 13 characters and 1 more for the directory separator.
                .Distinct();
        }

        /// <summary>
        /// Gets all the segments, e.g. verses, for this book.
        /// </summary>
        /// <param name="bookAbbreviation"></param>
        /// <param name="includeText"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public IEnumerable<BookSegment> GetBookSegments(
            string bookAbbreviation, 
            bool includeText = true)
        {
            return Directory.EnumerateFiles(_manuscriptTreesPath, $"{bookAbbreviation}*.xml")
                .SelectMany(fileName =>
                    XElement
                        .Load(fileName)
                        .Descendants("Sentence")
                        .Select(verse => new BookSegment
                            (
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(2, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node 
                                                                            doesn't have a nodeId attribute. Cannot determine chapter"),
                                verse
                                    .Descendants()
                                    .Where(node => node.FirstNode is XText)
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(5, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node doesn't 
                                                                             have a nodeId attribute. Cannot determine chapter"),
                                includeText ? 
                                    string.Join(
                                        " ",
                                        verse
                                            .Descendants()
                                            .Where(node => node.FirstNode is XText)
                                            .Select(leaf => leaf?
                                                .Attribute("UnicodeLemma")?.Value.Replace(' ', '~') ?? "")) 
                                    : ""
                            )
                        )
                );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">Three character book abbreviation (SIL)</param>
        /// <param name="chapter"></param>
        /// <param name="verses"></param>
        /// <returns></returns>
        public XElement? GetTreeNode(string book, int chapter, List<int> verses)
        {
            var codes = Mappings.ManuscriptFileBookToSILBookPrefixes
                .Where(bookToPrefixes => bookToPrefixes.Value.abbr.Equals(book))
                .Select(bookToPrefixes => bookToPrefixes.Key);

            if (codes.Count() != 1)
            {
                throw new InvalidDataException("Book not found in ManuscriptFileBookToSILBookPrefixes mapping");
            }

            var fileName = $"{codes.First()}{chapter.ToString("000")}.trees.xml";

            if (!File.Exists(fileName))
            {
                throw new FileLoadException($"{fileName} doesn't exist.");
            }

            var vs = XElement.Load(fileName)
                .Descendants("Sentence")
                .Select(s => s.Descendants("Node").First())
                .ToList();

            return _combineTrees(vs);
        }

        private XElement? _combineTrees(List<XElement>? trees)
        {
            if (trees == null)
            {
                return null;
            }
            else if (trees.Count() < 2)
            {
                return trees.FirstOrDefault(); //should return null if trees is empty.
            }

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
}

