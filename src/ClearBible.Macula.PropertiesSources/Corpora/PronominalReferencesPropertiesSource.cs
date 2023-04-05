using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using System.Reflection;
using System.Xml.Linq;

namespace ClearBible.Macula.PronominalReferencePropertiesSource.Corpora
{
    internal class PronominalReferencesPropertiesSource : IExtendedPropertiesSource
    {
        private string? _loadFilePath;

        private Dictionary<TokenId, string> tokenIdToXmlMap = new();
        
        public PronominalReferencesPropertiesSource(string? loadFilePath = null)
        {
            _loadFilePath = loadFilePath ??
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                + Path.DirectorySeparatorChar
                + "Corpora"
                + Path.DirectorySeparatorChar
                + "pronominalreferences"
                + Path.DirectorySeparatorChar
                + "PronominalReferences.xml";

            var pronominalReferences = XDocument.Load(_loadFilePath).Root?.Elements()
                ?? throw new InvalidParameterEngineException(name: "_loadFilePath", value: _loadFilePath);

            foreach (var pronominalReference in pronominalReferences)
            {
                var tokenIdString = pronominalReference.Element("TokenId")?.Value
                    ?? throw new InvalidDataEngineException(name: "TokenId", value: "no such element");

                //because some pronominal references have an invaild token id
                if (tokenIdString.Contains("00n"))
                {
                    continue;
                }

                var tokenId = new TokenId(tokenIdString);

                using var writer = new StringWriter();
                pronominalReference.Save(writer);
                var xml = writer.ToString();
                tokenIdToXmlMap.Add(tokenId, xml);
            }
        }
        public string? GetExtendedPropertiesObjectForToken(TokenId tokenId)
        {
            return tokenIdToXmlMap.GetValueOrDefault(tokenId);
        }
    }
}
