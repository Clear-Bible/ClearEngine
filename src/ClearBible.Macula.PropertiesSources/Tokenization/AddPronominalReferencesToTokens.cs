using ClearBible.Engine.Tokenization;
using ClearBible.Macula.PronominalReferencePropertiesSource.Corpora;


namespace ClearBible.Macula.PropertiesSources.Tokenization
{
    public class AddPronominalReferencesToTokens : AddEExtendedPropertiesToTokens
    {
        private static PronominalReferencesPropertiesSource? _pronominalReferencesPropertiesSource;
        public static void Init(string? pronominalReferencesPropertiesSourceLoadFilePath = null)
        {
            _pronominalReferencesPropertiesSource = new PronominalReferencesPropertiesSource(pronominalReferencesPropertiesSourceLoadFilePath);
        }
        public AddPronominalReferencesToTokens() :base(new PronominalReferencesPropertiesSource())
        {
            if (_pronominalReferencesPropertiesSource == null)
                _pronominalReferencesPropertiesSource = new PronominalReferencesPropertiesSource();
        }
    }
}
