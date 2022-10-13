
using ClearBible.Engine.Corpora;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ClearBible.Engine.SyntaxTree.Corpora
{
    public class SyntaxTreeToken : Token
    {
        public  SyntaxTreeToken(TokenId tokenId, string surface, string strong, string partsOfSpeech, /*string analysis, */string lemma, string english) : base(tokenId, surface, lemma)
        {
            Strong = strong;
            PartsOfSpeech = partsOfSpeech;
            English = english;
            //Analysis = analysis;

            XmlSerializer thisXmlSerializer = new XmlSerializer(GetType());
            using (var writer = new StringWriter())
            {
                thisXmlSerializer.Serialize(writer, this);
                AddToExtendedProperties(writer.ToString());
            }
        }

        public SyntaxTreeToken() : base(new TokenId("000000000000000"))
        {
        }

        public string? Strong { get; set; }
        public string? PartsOfSpeech { get; set; }
        public string? English { get; set; }
        //public string Analysis { get; }
    }
}
