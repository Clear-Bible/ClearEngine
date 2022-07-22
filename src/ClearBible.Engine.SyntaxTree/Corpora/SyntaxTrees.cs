using System.Reflection;
using System.Xml.Linq;

using ClearBible.Engine.Exceptions;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.SyntaxTree.Corpora
{
    public record BookChapterVerseXElements(string Book, int ChapterNumber, IEnumerable<XElement> VerseXElements);
    public class SyntaxTrees : ISyntaxTreeText, ISyntaxTree
    {
        private readonly string _syntaxTreesPath;

        public SyntaxTrees(string? syntaxTreesPath = null)
        {
            _syntaxTreesPath = syntaxTreesPath ?? 
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                + Path.DirectorySeparatorChar 
                + "Corpora"
                + Path.DirectorySeparatorChar
                + "syntaxtrees";
        }

        #region ISyntaxTreeText

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns the books that exist in the syntax trees in three letter SIL abbreviation form.</returns>
        public IEnumerable<string> GetBooks()
        {

            var books = Directory.EnumerateFiles(_syntaxTreesPath, "*.xml")
                .Select(fileName => BookIds
                    .Where(bookId => bookId.clearTreeBookAbbrev.Equals(fileName
                        .Trim().Substring(_syntaxTreesPath.Length + 1, fileName.Length - _syntaxTreesPath.Length - 1 - 13)))
                    .FirstOrDefault()?.silCannonBookAbbrev ?? "")
                //names are  in b[bb]ccc.trees.xml format, and we want the b[bb] part, therefore subtracting
                // 13 characters and 1 more for the directory separator.
                .Where(silBookAbbrev => !silBookAbbrev.Trim().Equals(""))
                .Distinct() //unordered
                .OrderBy(s => s);

            return books;
        }

        /// <summary>
        /// Gets all the segments, e.g. verses, for this book.
        /// </summary>
        /// <param name="bookAbbreviation"></param>
        /// <param name="includeText"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTreeEngineException"></exception>
        public IEnumerable<(string chapter, string verse, IEnumerable<SyntaxTreeToken> syntaxTreeTokens, bool isSentenceStart)> GetTokensTextRowInfos(string bookAbbreviation)
        {
            return GetBookChapters(bookAbbreviation)
                .SelectMany(c => GetVerseXElementsForBookChapter(bookAbbreviation, c).VerseXElements
                    .Select(verse =>
                        (
                            chapter: verse
                                .GetLeafs()
                                .First()
                                ?.Chapter()
                                ?? throw new InvalidTreeEngineException($"Doesn't have a first leaf", new Dictionary<string, string> 
                                {
                                    {"bookAbbreviation", bookAbbreviation },
                                    {"chapter", c.ToString()}
                                }),
                            verse: verse
                                .GetLeafs()
                                .First()
                                ?.Verse()
                                ?? throw new InvalidTreeEngineException($"Doesn't have a first leaf", new Dictionary<string, string>
                                {
                                    {"bookAbbreviation", bookAbbreviation },
                                    {"chapter", c.ToString()}
                                }),
                            /*FIXME: remove? text: string.Join(
                                    " ",
                                    verse
                                        .GetTerminalNodes()
                                        .Select(leaf => leaf.Lemma().Replace(' ', '~'))),*/
                            syntaxTreeTokens: verse
                                .GetLeafs()
                                .Select((leaf, index) => new SyntaxTreeToken(
                                    leaf.TokenId(),
                                    leaf.Surface(),
                                    leaf.Strong(),
                                    leaf.Category(),
                                    leaf.Lemma()
                                    )),
                            isSentenceStart: true
                        ))
                    );
            /*
            return Directory.EnumerateFiles(_syntaxTreesPath, $"{bookAbbreviation}*.xml")
                .SelectMany(fileName =>
                    XElement
                        .Load(fileName)
                        .Descendants("Sentence")
                        .Select(verse => new BookSegment
                            (
                                verse
                                    .GetTerminalNodes()
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(2, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node 
                                                                            doesn't have a nodeId attribute. Cannot determine chapter number"),
                                verse
                                    .GetTerminalNodes()
                                    .First()
                                    ?.Attribute("morphId")?.Value.Substring(5, 3)
                                    ?? throw new InvalidDataException($@"Syntax tree {fileName} has a verse whose first leaf node doesn't 
                                                                             have a nodeId attribute. Cannot determine verse number"),
                                includeText ? 
                                    string.Join(
                                        " ",
                                        verse
                                            .GetTerminalNodes()
                                            .Select(leaf => leaf?
                                                .Attribute("UnicodeLemma")?.Value.Replace(' ', '~') ?? "")) 
                                    : ""
                            )
                        )
                );
            */
        }

        public IEnumerable<SyntaxTreeToken> GetSyntaxTreeTokensForSegment(string bookAbbreviation, int chapterNumber, int verseNumber)
        {
            IEnumerable<XElement> chapterXElements = GetVerseXElementsForBookChapter(bookAbbreviation, chapterNumber).VerseXElements;
            return chapterXElements
                .Where(verse => verse
                    .GetLeafs()
                    .First()
                     ?.Verse().Equals(verseNumber.ToString("000")) ?? false)
                .SelectMany(chapterElement => chapterElement
                    .Descendants("Node")
                    .Where(node => node.FirstNode is XText)
                    .Select(leaf => new SyntaxTreeToken(leaf.TokenId(), leaf.Surface(), leaf.Strong(), leaf.Category(), /*leaf.Analysis(),*/ leaf.Lemma())));
        }

        #endregion

        #region ISyntaxTree
        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">Three character book abbreviation (SIL)</param>
        /// <param name="chapterNumber"></param>
        /// <param name="verseNumbers"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTreeEngineException"></exception>
        public XElement? GetVersesXElementsCombined(BookChapterVerseXElements bookChapterVerseXElements, IEnumerable<int> verseNumbers)
        {
            List<XElement> verseXElements = new List<XElement>();

            foreach (int verseNumber in verseNumbers)
            {
                var verseXElement = bookChapterVerseXElements.VerseXElements
                    .Where(x =>
                    {
                        bool success = int.TryParse(x.NodeId()?.Substring(5, 3), out int attrVerseNumber);
                        if (success)
                        {
                            return verseNumber == attrVerseNumber;
                        }
                        else
                        {
                            return false;
                        }
                    })
                    .FirstOrDefault();

                if (verseXElement == null)
                {
                    throw new InvalidTreeEngineException($"Not found", new Dictionary<string, string>
                        {
                            {"book", bookChapterVerseXElements.Book },
                            {"chapterNumber", bookChapterVerseXElements.ChapterNumber.ToString()},
                            {"verseNumber", verseNumber.ToString()}
                        });
                }
                else
                {
                    verseXElements.Add(verseXElement);
                }
            }
            if (verseXElements.Count > 1)
            {
                return CombineVerseXElementsIntoOne(verseXElements);
            }
            else if (verseXElements.Count == 1)
            {
                return verseXElements[0];
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region bookChapters cache from filenames and access method

        /// <summary>
        /// A cache of chapterNumbers by SIL bookAbreviations.
        /// </summary>
        private Dictionary<string,List<int>> _bookChapters = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookAbbreviation">SIL book abbreviation</param>
        /// <returns></returns>
        /// <exception cref="InvalidTreeEngineException"></exception>
        /// <exception cref="InvalidBookMappingEngineException"></exception>
        protected IEnumerable<int> GetBookChapters(string bookAbbreviation)
        {
            if (!_bookChapters.ContainsKey(bookAbbreviation))
            {
                string? clearBookAbbreviation = BookIds
                    .Where(bookId => bookId.silCannonBookAbbrev.Equals(bookAbbreviation))
                    .Select(bookId => bookId.clearTreeBookAbbrev)
                    .FirstOrDefault();

                if (clearBookAbbreviation == null)
                {
                    throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookAbbrev", value: bookAbbreviation);
                }

                _bookChapters.Add(bookAbbreviation, Directory.EnumerateFiles(_syntaxTreesPath, $"{clearBookAbbreviation}???.trees.xml")
                    .Select(fileName =>
                    {
                        int chapterNumber;
                        var success = int.TryParse(fileName.Substring(fileName.Length - 13, 3), out chapterNumber);
                        if (!success)
                        {
                            throw new InvalidTreeEngineException($"Filename couldn't be parsed for chapter number", new Dictionary<string, string>
                                {
                                    {"bookAbbreviation", bookAbbreviation },
                                    {"fileName", fileName}
                                });
                        }
                        return chapterNumber;
                    }).ToList());
            }
            return _bookChapters[bookAbbreviation];
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">SIL book abbreviation</param>
        /// <param name="chapterNumber"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTreeEngineException"></exception>
        /// <exception cref="InvalidBookMappingEngineException"></exception>
        public BookChapterVerseXElements GetVerseXElementsForBookChapter(string book, int chapterNumber)
        {
            var codes = BookIds
                .Where(BookId => BookId.silCannonBookAbbrev.Equals(book))
                .Select(bookId => bookId.clearTreeBookAbbrev);

            if (codes.Count() != 1)
            {
                throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookAbbrev", value: book);
            }

            var path = Path.GetFullPath(_syntaxTreesPath);
            var fileName = Path.Combine(path, $"{codes.First()}{chapterNumber.ToString("000")}.trees.xml");

            if (!File.Exists(fileName))
            {
                throw new InvalidTreeEngineException($"File doesn't Exist.", new Dictionary<string, string>
                    {
                        {"fileName", fileName },
                        {"bookAbbreviation", book },
                        {"chapterNumber", chapterNumber.ToString() }
                    });
            }
            IEnumerable<XElement> verseXElements = XElement.Load(fileName)
                .Descendants("Sentence")
                .Select(s => s.Descendants("Node").First());
            return new BookChapterVerseXElements(book, chapterNumber, verseXElements);
        }
        protected XElement CombineVerseXElementsIntoOne(List<XElement> verseXElements)
        {

            List<XElement> subTrees =
                verseXElements.SelectMany(t => t.Elements()).ToList();

            int totalLength = subTrees
                .Select(x => Int32.Parse(x.Length()))
                .Sum();
            //FIXME? this will only include the first verse, but this may not matter if it's never used.
            string newNodeId =
                (subTrees[0].NodeId()?.Substring(0, 11) ?? throw new InvalidTreeEngineException($"Node doesn't have attribute.", new Dictionary<string, string>
                        {
                            {"attribute", "nodeId" }
                        })) +
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

