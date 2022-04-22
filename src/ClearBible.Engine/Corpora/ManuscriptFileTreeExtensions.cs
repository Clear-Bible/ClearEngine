using ClearBible.Engine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
    public static class ManuscriptFileTreeExtensions
    {
        private static string  GetMorphId(XElement textNode)
        {
            string morphId = textNode.Attribute("morphId")?.Value ?? throw new InvalidDataException($"textNode node id {textNode.Attribute("nodeId")} doesn't have a morphId attribute.");

            if (morphId.Length == 11)
            {
                morphId = morphId + "1";
            }
            else if (morphId.Length != 12)
            {
                throw new InvalidTreeEngineException($"doesn't have attribute or it isn't length 11 or 12.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            return morphId;
        }
        /// <summary>
        /// SIL Book Abbreviation
        /// </summary>
        /// <param name="textNode"></param>
        /// <returns></returns>
        public static string Book(this XElement textNode)
        {
            string subString = GetMorphId(textNode).Substring(0, 2);
            return BookIds
                .Where(bookId => bookId.clearTreeBookNum.Equals(subString.Trim()))
                .FirstOrDefault()?.silCannonBookAbbrev ?? throw new InvalidTreeEngineException($"position 0 length 2 isn't parsable into a SIL book number integer.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute missing>"},
                            {"attribute", "morphId" },
                            {"subString(0,2)", subString }
                        });
        }
        /// <summary>
        /// SIL Book Number
        /// </summary>
        /// <param name="textNode"></param>
        /// <returns></returns>
        public static int BookNum(this XElement textNode)
        {
            string subString = GetMorphId(textNode).Substring(0, 2);
            string bookNumberString = BookIds
                .Where(bookId => bookId.clearTreeBookNum.Equals(subString.Trim()))
                .FirstOrDefault()?.silCannonBookNum ?? throw new InvalidTreeEngineException($"position 0 length 2 isn't parsable into a SIL book number integer.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute missing>"},
                            {"attribute", "morphId" },
                            {"subString(0,2)", subString }
                        });

            bool succeeded = int.TryParse(bookNumberString, out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"position 0 length 2 isn't parsable into a SIL book number integer.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }
        public static string Chapter(this XElement textNode)
        {
            return GetMorphId(textNode).Substring(2, 3);
        }

        public static int ChapterNumber(this XElement textNode)
        {
            bool succeeded = int.TryParse(GetMorphId(textNode).Substring(2, 3), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"position 2 length 3 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }

        public static string Verse(this XElement textNode)
        {
            return GetMorphId(textNode).Substring(5, 3);
        }
        public static int VerseNumber(this XElement textNode)
        {
            bool succeeded = int.TryParse(GetMorphId(textNode).Substring(5, 3), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"position 5 length 3 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }
        public static string WordNumberString(this XElement textNode)
        {
            return GetMorphId(textNode).Substring(8, 3);
        }

        public static int WordNumber(this XElement textNode)
        {
            bool succeeded = int.TryParse(GetMorphId(textNode).Substring(8, 3), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"position 8 length 3 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }
        public static string SubwordNumberString(this XElement textNode)
        {
            return GetMorphId(textNode).Substring(11, 1);
        }

        public static int SubwordNumber(this XElement textNode)
        {
            bool succeeded = int.TryParse(GetMorphId(textNode).Substring(11, 1), out int num);
            if (!succeeded)
            {
                throw new InvalidTreeEngineException($"position 11 length 1 isn't parsable into an int.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "morphId" }
                        });
            }
            else
            {
                return num;
            }
        }

        public static TokenId TokenId(this XElement textNode)
        {
            return new TokenId(textNode.BookNum(), textNode.ChapterNumber(), textNode.VerseNumber(), textNode.WordNumber(), textNode.SubwordNumber());
        }

        public static string Lemma(this XElement textNode) =>
            textNode.Attribute("UnicodeLemma")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "UnicodeLemma" }
                        });
        public static string Surface(this XElement textNode) =>
            textNode.Attribute("Unicode")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "Unicode" }
                        });
        public static string Strong(this XElement textNode) =>
            (textNode.Attribute("Language")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "Language" }
                        })) +
            (textNode.Attribute("StrongNumberX")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "StrongNumberX" }
                        }));
        public static string Category(this XElement textNode) =>
            textNode.Attribute("Cat")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                        {
                            {"nodeId", textNode.Attribute("nodeId")?.Value ?? "<nodeId attribute also missing>"},
                            {"attribute", "Cat" }
                        });
    }
}
