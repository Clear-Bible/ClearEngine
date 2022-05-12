using ClearBible.Engine.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public record CorpusUri
    {
        public enum SourceTypeEnum
        {
            ParatextPlugin,
            ParatextDirectory,
            Database
        }
        
        public SourceTypeEnum SourceType { get; }
        public string Identifier { get; }

        public CorpusUri(SourceTypeEnum sourceType, string identifier)
        {
            SourceType = sourceType;
            Identifier = identifier;
        }
        public CorpusUri(string uri)
        {
            Regex r = new Regex(@"^(?<sourceType>\w+)://(?<identifier>\w+)",
                                RegexOptions.None, TimeSpan.FromMilliseconds(150));
            Match m = r.Match(uri);
            if (m.Success)
            {
                Group sourceTypeGroup = m.Groups["sourceType"];
                if (sourceTypeGroup.Success)
                {
                    bool success = Enum.TryParse(sourceTypeGroup.Value, out SourceTypeEnum sourceType);
                    if (success)
                        SourceType = sourceType;
                    else
                        throw new InvalidParameterEngineException(message: "sourceType not parseable to int", name: "uri", value: uri);
                }
                else
                {
                    throw new InvalidParameterEngineException(message: "sourceType not found in uri format sourceType://identifier", name: "uri", value: uri);
                }
                Group identifierGroup = m.Groups["identifier"];
                if (identifierGroup.Success)
                {
                    Identifier = identifierGroup.Value;
                }
                else
                {
                    throw new InvalidParameterEngineException(message: "identifier not found in uri format sourceType://identifier", name: "uri", value: uri);
                }
            }
            else
            {
                throw new InvalidParameterEngineException(message: "could not successfully parse", name: "uri", value: uri);
            }
        }

        public override string ToString()
        {
            return $"{SourceType}://{Identifier}";
        }
    }
}
