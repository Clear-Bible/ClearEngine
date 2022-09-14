using System.Text.RegularExpressions;
using System.Xml.Linq;

using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.SyntaxTree.Corpora
{
    public static class Extensions
    {
        #region Node element atributes
        public static string? NodeId(this XElement node) =>
            node.Attribute("nodeId")?.Value ?? null;

        public static string Length(this XElement node) =>
            node.Attribute("Length")?.Value ?? throw new InvalidTreeEngineException($"node missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", node.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Length" }
                });

        public static XElement? ParentIfParentNotTreeElement(this XElement node) =>
            node.Parent?.Name.LocalName != "Tree" ? node.Parent : null;

        #endregion


        #region Node that has XText ('leaf' or 'terminal' node) attributes

        public static IEnumerable<XElement> GetLeafs(this XElement element)
        {
            return element
                .Descendants()
                .Where(e => e.FirstNode is XText);
        }
        public static string MorphId(this XElement leaf)
        {
            string morphId = leaf.Attribute("morphId")?.Value ?? throw new InvalidDataException($"leaf node id {leaf.Attribute("nodeId")} doesn't have a morphId attribute.");

            if (morphId.Length == 11)
            {
                morphId = morphId + "1";
            }
            else if (morphId.Length != 12)
            {
                throw new InvalidTreeEngineException($"leaf doesn't have attribute or it isn't length 11 or 12.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            return morphId;
        }
        public static string Lemma(this XElement leaf) =>
            leaf.Attribute("UnicodeLemma")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "UnicodeLemma" }
                });
        public static string Surface(this XElement leaf) =>
            Regex.Replace(
                leaf.Attribute("Unicode")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Unicode" }
                        }), "\u200E", "", RegexOptions.Compiled);  //TREEBUG: trees have u+200E left to right character in them.

        public static string Strong(this XElement leaf) =>
            (leaf.Attribute("Language")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Language" }
                        })) +
            (leaf.Attribute("StrongNumberX")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "StrongNumberX" }
                        }));
        public static string Category(this XElement leaf) =>
            leaf.Attribute("Cat")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Cat" }
                        });

        public static string English(this XElement leaf) =>
            leaf.Attribute("English")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "English" }
                });
        /// <summary>
        /// SIL Book Abbreviation
        /// </summary>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public static string Book(this XElement leaf)
        {
            string subString = leaf.MorphId().Substring(0, 2);
            return BookIds
                .Where(bookId => bookId.clearTreeBookNum.Equals(subString.Trim()))
                .FirstOrDefault()?.silCannonBookAbbrev ?? throw new InvalidTreeEngineException($"leaf attribute position 0 length 2 isn't parsable into a SIL book number integer.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute missing>"},
                            {"attribute", "morphId" },
                            {"subString(0,2)", subString }
                        });
        }
        /// <summary>
        /// SIL Book Number
        /// </summary>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public static int BookNum(this XElement leaf)
        {
            bool succeeded = int.TryParse(leaf.MorphId().Substring(0, 2), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"leaf attribute position 0 length 2 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                string bookNumberString = BookIds
                    .Where(bookId => int.Parse(bookId.clearTreeBookNum) == num)
                    .FirstOrDefault()?.silCannonBookNum ?? throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookNum", value: num.ToString());

                return int.Parse(bookNumberString);
            }
        }
        public static string Chapter(this XElement leaf)
        {
            return leaf.MorphId().Substring(2, 3);
        }

        public static int ChapterNumber(this XElement leaf)
        {
            bool succeeded = int.TryParse(leaf.MorphId().Substring(2, 3), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"leaf attribute position 2 length 3 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }

        public static string Verse(this XElement leaf)
        {
            return leaf.MorphId().Substring(5, 3);
        }

        public static string BookChapterVerse(this XElement leaf)
        {
            return leaf.MorphId().Substring(0, 8);
        }
        public static int VerseNumber(this XElement leaf)
        {
            bool succeeded = int.TryParse(leaf.MorphId().Substring(5, 3), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"leaf attribute position 5 length 3 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }
        public static string WordNumberString(this XElement leaf)
        {
            return leaf.MorphId().Substring(8, 3);
        }

        public static int WordNumber(this XElement leaf)
        {
            bool succeeded = int.TryParse(leaf.MorphId().Substring(8, 3), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"leaf attribute position 8 length 3 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }
        public static string SubwordNumberString(this XElement leaf)
        {
            return leaf.MorphId().Substring(11, 1);
        }

        public static int SubwordNumber(this XElement leaf)
        {
            bool succeeded = int.TryParse(leaf.MorphId().Substring(11, 1), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"leaf attribute position 11 length 1 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }

        public static TokenId TokenId(this XElement leaf)
        {
            return new TokenId(leaf.BookNum(), leaf.ChapterNumber(), leaf.VerseNumber(), leaf.WordNumber(), leaf.SubwordNumber());
        }

        #endregion

    }
}
