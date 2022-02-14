using ClearBible.Clear3.API;

namespace ClearBible.Engine.Translation
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="TargetPunctuation"></param>
    /// <param name="TargetWordsToNotAlign">(StopWords)</param>
    /// <param name="SourceFunctionWords"></param>
    /// <param name="TargetFunctionWords"></param>
    /// <param name="TranslationModelOverrides">(manTransModel)</param>
    /// <param name="AlwaysAlignSourceToTargetPairs">(goodLinks)</param>
    /// <param name="NeverAlignSourceToTargetPairs">(badLinks)</param>
    /// <param name="TargetWordGlosses">(GlossTable)</param>
    /// <param name="Groups"></param>
    /// <param name="alignmentsToKeep"></param>
    /// <param name="Strongs"></param>
    public record ManuscriptWordAlignmentConfig(
        List<string> TargetPunctuation,
        List<string> TargetWordsToNotAlign,
        List<string> SourceFunctionWords,
        List<string> TargetFunctionWords,
        TranslationModel TranslationModelOverrides,
        Dictionary<string, int> AlwaysAlignSourceToTargetPairs,
        Dictionary<string, int> NeverAlignSourceToTargetPairs,
        Dictionary<string, Gloss> TargetWordGlosses,
        GroupTranslationsTable Groups,
        Dictionary<string, Dictionary<string, string>> alignmentsToKeep,
        Dictionary<string, Dictionary<string, int>> Strongs
        );
}
