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
            string? surfaceText = null, 
            string? english = null,
            string? notes = null)
        {
            TokenId = tokenId;
            MorphId = morphId;
            PronominalReferencesAsStrings = pronominalReferencesAsStrings;
            VerbSubjectReferencesAsStrings = verbSubjectReferencesAsStrings;
            SurfaceText = surfaceText;
            English = english;
            Notes = notes;
        }

        internal PronominalReferenceDetails DeepCopyIntoPronominalReferenceDetails()
        {
            return new PronominalReferenceDetails(TokenIdString, SurfaceText, English, Notes);
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
        public List<PronominalReferenceDetails?>? PronominalReferenceDetails { get; set; }
        
        [XmlElement(ElementName = "VerbSubjectReferences")]
        public List<PronominalReferenceDetails?>? VerbSubjectReferenceDetails { get; set; }
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

    public class PronominalReferenceDetails
    {
        public PronominalReferenceDetails()
        { }
        internal PronominalReferenceDetails(
            string? tokenIdString,
            string? surfaceText = null,
            string? english = null,
            string? notes = null)
        {
            TokenIdString = tokenIdString;
            SurfaceText = surfaceText;
            English = english;
            Notes = notes;
        }

        [XmlElement(ElementName = "TokenId")]
        public string? TokenIdString { get; set; }
        public string? SurfaceText { get; set; }
        public string? English { get; set; }
        public string? Notes { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is PronominalReferenceDetails details &&
                   TokenIdString == details.TokenIdString;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TokenIdString);
        }
    }
}
