using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Aligner.Adapter;
using ClearBible.Engine.SyntaxTree.Aligner.Legacy;


namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
    public class SyntaxTreeWordAlignerHyperparameters
    {
        private static TranslationModel ToLegacyTranslationModel(Dictionary<string, Dictionary<string, double>> transMod)
        {
            return new TranslationModel(transMod
                .Select(kv => KeyValuePair.Create(new SourceLemma(kv.Key), kv.Value
                    .Select(v => KeyValuePair.Create(new TargetLemma(v.Key), new Score(v.Value)))
                    .ToDictionary(x => x.Key, x => x.Value)
                ))
                .ToDictionary(x => x.Key, x => x.Value)
            );
        }

        private static AlignmentModel ToLegacyAlignmentModel(List<IReadOnlyCollection<TokensAlignedWordPair>> alignMod)
        {
            //{book:D2}{chapter:D3}{verse:D3}{word:D3}{subsegment:D1}
            return new AlignmentModel(alignMod
                .SelectMany(c => c
                    .Select(p => KeyValuePair.Create(
                        new BareLink(
                            p.SourceToken?.TokenId.ToSourceId() ?? throw new InvalidDataEngineException(message: "Can't create AlignmentModel: sourceToken is null"),
                            p.TargetToken?.TokenId.ToTargetId() ?? throw new InvalidDataEngineException(message: "Can't create AlignmentModel: targetToken is null")),
                        new Score(p.AlignmentScore)))
                     )
                     .ToDictionary(x => x.Key, x => x.Value)
               );
        }


        public Dictionary<TokenId, TokenId> ApprovedAlignedTokenIdPairs { private get; set; } = new();
        public List<string> TargetPunctuation { private get; set; } = new();

        /// <summary>
        /// A database of Strong's information, consisting of a dictionary
        /// mapping a Strong number to a dictionary whose keys are the set
        /// of target texts that are possible definitions of the word.
        /// The auto-aligner gives preference to a Strong's definition when
        /// one is available.
        /// </summary>
        ///
        // FIXME: Use Dictionary<string, HashSet<string>> instead?
        //
        public Dictionary<string, Dictionary<string, int>> Strongs { get; set; } = new();
        //public Dictionary<string, Gloss> glossTable { get; }
        public Dictionary<string, Dictionary<string, string>> OldLinks { private get;  set; } = new();
        public  Dictionary<string, int> GoodLinks { private get; set; } = new();
        public Dictionary<string, int> BadLinks { private get; set; } = new();
        public  List<string> SourceFunctionWords { private get; set; } = new();
        public List<string> TargetFunctionWords { private get; set; } = new();
        public List<string> StopWords { private get; set; } = new();
        public TranslationModel ManTransModel { private get; set; } = new TranslationModel(new Dictionary<SourceLemma, Dictionary<TargetLemma, Score>>());
        //public GroupTranslationsTable groups { get; }

        /// <summary>
        /// The maximum number of alternatives that the auto-aligner should
        /// permit during its generation of alternatives using tree traversal.
        /// </summary>
        /// 
        public int MaxPaths { get; set; } = 1000000;
        public int GoodLinkMinCount { private get; set; } = 3;
        public int BadLinkMinCount { private get; set; } = 3;

        /// <summary>
        /// True if the estimated AlignmentModel should influence the
        /// probabilities of the possible target words identified for each
        /// source segment.
        /// </summary>
        /// 
        public bool UseAlignModel { get; set; } = true;

        /// <summary>
        /// True if the auto aligner should use content words only.
        /// </summary>
        /// 
        public bool ContentWordsOnly { get; set; } = false;

        /// <summary>
        /// True if lemma_cat was used to create the SMT models.
        /// </summary>
        /// 
        public bool UseLemmaCatModel { get; set; } = false;

        /// <summary>
        /// Returns true if the specified target text is punctuation.
        /// </summary>
        /// 



        internal Dictionary<string, Dictionary<string, double>> TranslationModel { private get; set; } = new();
        internal Dictionary<string, Dictionary<string, double>> TranslationModelTC { private get; set; } = new();
        internal List<IReadOnlyCollection<TokensAlignedWordPair>> AlignmentProbabilities { private get; set; } = new();
        internal List<IReadOnlyCollection<TokensAlignedWordPair>> AlignmentProbabilitiesPre { private get; set; } = new();


        internal bool IsTargetPunctuation(string text) => TargetPunctuation.Contains(text);

        /// <summary>
        /// Returns true if the specified source lemma or lowercased target
        /// text is a stop word.
        /// </summary>
        /// 
        internal bool IsStopWord(string text) => StopWords.Contains(text);

        /// <summary>
        /// Returns true if the specified source lemma is a function word.
        /// </summary>
        /// 
        internal bool IsSourceFunctionWord(string lemma) => SourceFunctionWords.Contains(lemma);

        /// <summary>
        /// Returns true if the specified lowercased target text is a
        /// function word.
        /// </summary>
        /// 
        internal bool IsTargetFunctionWord(string text) => TargetFunctionWords.Contains(text);

        /// <summary>
        /// Returns true if the specified lemma and lowercased target
        /// text has been identified as a bad link.
        /// </summary>
        /// 
        internal bool IsBadLink(string lemma, string targetTextLower)
        {
            string link = $"{lemma}#{targetTextLower}";
            return
                BadLinks.ContainsKey(link) &&
                BadLinks[link] >= BadLinkMinCount;
        }

        /// <summary>
        /// Returns true if the specified lemma and lowercased target
        /// text has been identified as a good link.
        /// </summary>
        /// 
        internal bool IsGoodLink(string lemma, string targetTextLower)
        {
            string link = $"{lemma}#{targetTextLower}";
            return
                GoodLinks.ContainsKey(link) &&
                GoodLinks[link] >= GoodLinkMinCount;
        }

        /// <summary>
        /// Get a dictionary of old links for a specified verse.  When there
        /// is an old link for a source lemma, the auto aligner should give
        /// priority to it.
        /// </summary>
        /// <returns>
        /// A dictionary that maps source word to target
        /// word, as identified by their alternate IDs.  (Alternate IDs have
        /// the form, for example, of "λόγος-2" to mean the second occurence
        /// of the surface form "λόγος" within the verse, or "word-2" to mean
        /// the second occurrence of the surface target text "word"
        /// within the verse.)
        /// </returns>
        /// 
        internal Dictionary<string, string> OldLinksForVerse(string legacyVerseID)
        {
            if (OldLinks.TryGetValue(legacyVerseID, out Dictionary<string, string>? linksForVerse))
            {
                return linksForVerse;
            }
            else
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Get the score from the estimated translation model for a
        /// specified lemma and target text, or 0 if the target text
        /// is not a translation for the lemma in the estimated translation
        /// model.
        /// </summary>
        /// 
        internal double GetTranslationModelScore(string sourceLemma, string targetLemma)
        {
            var _legacyTranslationModel = ToLegacyTranslationModel(TranslationModel);
            if (_legacyTranslationModel.Dictionary.TryGetValue(new SourceLemma(sourceLemma),
                out Dictionary<TargetLemma, Score>? translations))
            {
                if (translations.TryGetValue(new TargetLemma(targetLemma),
                    out Score? score))
                {
                    return score.Double;
                }
            }

            return 0;
        }

        /// <summary>
        /// Look up the score in the estimated alignment model for a
        /// link between specified source and target instances.
        /// </summary>
        /// <param name="score">
        /// Set to the score associated with the link, or to zero if the link
        /// does not occur in the estimated alignment model.
        /// </param>
        /// <returns>
        /// True if the link occurs in the estimated alignment model,
        /// and false otherwise.
        /// </returns>
        /// 
        internal bool TryGetAlignment(string sourceID, string targetID,  out double score)
        {
            var key = new BareLink(new SourceID(sourceID), new TargetID(targetID));

            if (ToLegacyAlignmentModel(AlignmentProbabilities).Dictionary.TryGetValue(key, out Score? score2))
            {
                score = score2.Double;
                return true;
            }
            else
            {
                score = 0;
                return false;
            }
        }

        /// <summary>
        /// Given a source instance, attempt to find some target instance
        /// to which it is linked in the estimated alignment model.
        /// </summary>
        /// <param name="targetID">
        /// Set to the ID of some target instance that is linked to the given
        /// source instance in the estimated alignment model, or "" if no
        /// such link can be found.
        /// </param>
        /// <returns>
        /// True if some target link was found, and false otherwise.
        /// </returns>
        /// 
        internal bool TryGetPreAlignment(string sourceID, out string? targetID) =>
            ToLegacyAlignmentModel(AlignmentProbabilitiesPre).Dictionary.Keys
                .GroupBy(bareLink => bareLink.SourceID)
                .Where(group => group.Any())
                .ToDictionary(
                    group => group.Key.AsCanonicalString,
                    group => group.First().TargetID.AsCanonicalString)
                .TryGetValue(sourceID, out targetID);


        // 2021.05.27 CL: Changed to use the translationModelTC to let us use different models to get translations
        // when getting terminal candidates (TC) and when getting alignments for the rest.
        /// <summary>
        /// Get the translation of a source lemma from the estimated
        /// translation model.
        /// </summary>
        /// <param name="tryGetScoreForTargetText">
        /// Will be set to a function that looks up scores for
        /// target texts, or null if there are no translations for
        /// the lemma.
        /// </param>
        /// <returns>
        /// True if any translations exist and so tryGetScoreForTargetText
        /// is not null.
        /// </returns>
        /// 
        internal bool TryGetTranslations(string lemma, out TryGet<string, double> tryGetScoreForTargetText) =>
            TryGetFromTransModel(ToLegacyTranslationModel(TranslationModelTC), lemma, out tryGetScoreForTargetText);

        /// <summary>
        /// Get the translation of a source lemma from the confirmed
        /// translation model.
        /// </summary>
        /// <param name="tryGetScoreForTargetText">
        /// Will be set to a function that looks up scores for
        /// target texts, or null if there are no translations for
        /// the lemma.
        /// </param>
        /// <returns>
        /// True if any translations exist and so tryGetScoreForTargetText
        /// is not null.
        /// </returns>
        /// 
        internal bool TryGetManTranslations( string lemma, out TryGet<string, double> tryGetScoreForTargetText) =>
            TryGetFromTransModel(ManTransModel, lemma, out tryGetScoreForTargetText);


        private bool TryGetFromTransModel(
            TranslationModel translationModel,
            string lemma,
            out TryGet<string, double> tryGetScoreForTargetText)
        {
            if (translationModel.Dictionary.TryGetValue(
                new SourceLemma(lemma),
                out Dictionary<TargetLemma, Score>? translations))
            {
                tryGetScoreForTargetText =
                   (string targetLemma, out double score) =>
                   {
                       if (translations.TryGetValue(
                           new TargetLemma(targetLemma),
                           out Score? score2))
                       {
                           score = score2.Double;
                           return true;
                       }
                       else
                       {
                           score = 0;
                           return false;
                       }
                   };
                return true;
            }
            else
            {
                tryGetScoreForTargetText = // should be never used since returning false. This is to avoid making the out nullable.
                   (string targetLemma, out double score) =>
                   {
                           score = 0;
                           return false;
                    };
                return false;
            }
        }
    }
}
