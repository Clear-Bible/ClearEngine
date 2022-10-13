using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using System.Xml.Serialization;

namespace ClearBible.MaculaPropertiesSources.ETL.PronominalReferences
{
    public class PronominalReferences
    {
        public PronominalReferences()
        { }
        internal PronominalReferences(
            TokenId tokenId, 
            string morphId, 
            List<string>? pronominalReferencesAsStrings = null, 
            List<string>? verbSubjectReferencesAsStrings = null,
            List<PronominalReferences>? dereferencedPronominalReferences = null,
            List<PronominalReferences>? dereferencedVerbSubjectReferences = null,
            string? surfaceText = null, 
            string? english = null,
            string? notes = null)
        {
            TokenId = tokenId;
            MorphId = morphId;
            PronominalReferencesAsStrings = pronominalReferencesAsStrings;
            VerbSubjectReferencesAsStrings = verbSubjectReferencesAsStrings;
            DereferencedPronominalReferences = dereferencedPronominalReferences;
            DereferencedVerbSubjectReferences = dereferencedVerbSubjectReferences;
            SurfaceText = surfaceText;
            English = english;
            Notes = notes;
        }

        [XmlIgnore]
        internal TokenId? TokenId { get; set; }


        [XmlElement(ElementName = "TokenId")]
        public string? TokenIdString { 
            get
            {
                return TokenId?.ToString();
            }
            set
            {
                TokenId = new TokenId(value ?? throw new InvalidParameterEngineException(name: "TokenIdString", value:"null"));
            }
        }
        
        [XmlIgnore]
        internal string? MorphId { get; set; }
        
        [XmlIgnore]
        internal List<string>? PronominalReferencesAsStrings { get; set; }
       
        [XmlIgnore]
        internal List<string>? VerbSubjectReferencesAsStrings { get; set; }
       
        [XmlElement(ElementName = "PronominalReferences")]
        public List<PronominalReferences>? DereferencedPronominalReferences { get; set; }
        
        [XmlElement(ElementName = "VerbSubjectReferences")]
        public List<PronominalReferences>? DereferencedVerbSubjectReferences { get; set; }
        public string? SurfaceText { get; set; }
        public string? English { get; set; }
        public string? Notes { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is PronominalReferences reference &&
                   EqualityComparer<TokenId>.Default.Equals(TokenId, reference.TokenId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TokenId);
        }

        public override string ToString()
        {
            return $"TokenId: {TokenIdString}, English: {English}, SurfaceText: {SurfaceText}";
        }
    }
}
