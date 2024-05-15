using ClearBible.Engine.Exceptions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ClearBible.Engine.Corpora
{
    public class Token : IEquatable<Token>
    {
        private string? _extendedProperties = null;
        protected Token(TokenId tokenId)
        {
            TokenId = tokenId;
        }


        public Token(TokenId tokenId, string surfaceText, string trainingText)
        {
            if (trainingText.Contains(" "))
            {
                bool foundFirstNonSpace = false;
                bool foundFirstSpaceAfterNonSpace = false;
                foreach (var c in trainingText)
                {
                    if (!c.Equals(' '))
                    {
                        if (!foundFirstNonSpace)
                            foundFirstNonSpace = true;
                        else if (foundFirstNonSpace && foundFirstSpaceAfterNonSpace)
                            throw new InvalidParameterEngineException(name: "trainingText", value: trainingText, message: "can't contain spaces between non-space characters because native Thot uses spaces to segment tokens");
                    }
                    else //space
                    {
                        if (foundFirstNonSpace)
                            foundFirstSpaceAfterNonSpace = true;
                    }
                }
            }
            TokenId = tokenId;
            SurfaceText = surfaceText;
            TrainingText = trainingText;

            Position = ulong.Parse(TokenId.ToString());
        }

        [XmlIgnore]
        public virtual TokenId TokenId { get; }

        [XmlIgnore]
        public virtual ulong Position { get; set; } = 0;

        [XmlIgnore]
        public virtual string TrainingText { get; set; } = "";

        [XmlIgnore]
        public virtual string SurfaceText { get; set; } = "";

        [XmlIgnore]
        /// <summary>
        /// Cannot include SurfaceText within this string
        /// </summary>
        public virtual string SurfaceTextPrefix { get; set; } = "";

        [XmlIgnore]
        /// <summary>
        /// Cannot include SurfaceText within this string
        /// </summary>
        public virtual string SurfaceTextSuffix { get; set; } = "";

        [XmlIgnore]
        public string TokenType
        {
            get
            {
                return GetType().AssemblyQualifiedName!;
            }
        }


        [XmlIgnore]
        public virtual Dictionary<string, object> Metadata { get; set; } = new();

        public bool HasMetadatum(string key)
        {
            return Metadata.ContainsKey(key);
        }

        public T GetMetadatum<T>(string key)
        {
            if (!Metadata.TryGetValue(key, out var item))
            {
                throw new KeyNotFoundException($"Key '{key}' not found in Metadata");
            }

            if (item is T metadatum)
            {
                return metadatum;
            }

            try
            {
                var obj = Convert.ChangeType(item, typeof(T));
                if (obj is T metadatum2)
                {
                    return metadatum2;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot cast '{item}' to {typeof(T).Name}", ex);
            }


            throw new InvalidCastException($"Value for key '{key}' is not of type '{typeof(T).Name}'");
        }


        public void AddToExtendedProperties(string xmlString)
        {
            XElement rootElement;
            if (_extendedProperties == null)
                rootElement = new XElement("ExtendedProperties");
            else
            {
                using var extendedPropertiesReader = new StringReader(_extendedProperties);
                rootElement = XElement.Load(extendedPropertiesReader);
            }

            var rootElementXmlString = XDocument.Parse(xmlString).Root;
            rootElement.Add(rootElementXmlString);

            using var writer = new StringWriter();
            rootElement.Save(writer);
            _extendedProperties = writer.ToString();
        }

        [XmlIgnore]
        public virtual string? ExtendedProperties
        {
            set
            {
                _extendedProperties = value;
            }
            get
            {
                return _extendedProperties;
            }
        }
        public override bool Equals(object? obj)
        {
            return obj is Token token &&
                   TokenId.Equals(token.TokenId);
        }

        public override int GetHashCode()
        {
            return TokenId.GetHashCode();
        }

        public bool Equals(Token? other)
        {
            return Equals((object?)other);
        }

        public override string ToString()
        {
            return $"{SurfaceTextPrefix}{SurfaceText}{SurfaceTextSuffix}";
        }
    }
}
