using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Aligner.Adapter;
using ClearBible.Engine.SyntaxTree.Aligner.Legacy;


namespace ClearBible.Engine.SyntaxTree.Aligner.Translation
{
    public class SyntaxTreeWordAlignerHyperparameters
    {
        private static TranslationModel ToLegacyTranslationModel(Dictionary<string, Dictionary<string, double>>? transMod)
        {
            if (transMod == null)
            {
                throw new InvalidDataEngineException(message: "translation model is null");
            }
            return new TranslationModel(transMod
                .Select(kv => KeyValuePair.Create(new SourceLemma(kv.Key), kv.Value
                    .Select(v => KeyValuePair.Create(new TargetLemma(v.Key), new Score(v.Value)))
                    .ToDictionary(x => x.Key, x => x.Value)
                ))
                .ToDictionary(x => x.Key, x => x.Value)
            );
        }

        private static AlignmentModel ToLegacyAlignmentModel(List<IReadOnlyCollection<TokensAlignedWordPair>>? alignMod)
        {
            if (alignMod == null)
            {
                throw new InvalidDataEngineException(message: "alignment model is null");
            }

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
        public SyntaxTreeWordAlignerHyperparameters(
            Dictionary<string, Dictionary<string, int>> strongs,
            //Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            Dictionary<string, int> goodLinks,
            Dictionary<string, int> badLinks,
            List<string> sourceFunctionWords,
            List<string> targetFunctionWords,
            List<string> stopWords,
            List<string> puncs,
            TranslationModel manTransModel//,
            //GroupTranslationsTable groups
            )
        {
            this.Strongs = strongs;
            //this.glossTable = glossTable;
            this.OldLinks = oldLinks;
            this.GoodLinks = goodLinks;
            this.BadLinks = badLinks;
            this.SourceFunctionWords = sourceFunctionWords;
            this.TargetFunctionWords = targetFunctionWords;
            this.StopWords = stopWords;
            this.Puncs = puncs;
            this.manTransModel = manTransModel;
            //this.groups = groups;
        }

        public SyntaxTreeWordAlignerHyperparameters(
            Dictionary<string, Dictionary<string, int>> strongs,
            //Dictionary<string, Gloss> glossTable,
            Dictionary<string, Dictionary<string, string>> oldLinks,
            Dictionary<string, int> goodLinks,
            Dictionary<string, int> badLinks,
            List<string> sourceFunctionWords,
            List<string> targetFunctionWords,
            List<string> stopWords,
            List<string> puncs,
            TranslationModel manTransModel,
            //GroupTranslationsTable groups,
            int maxPaths,
            int goodLinkMinCount,
            int badLinkMinCount,
            bool useAlignModel,
            bool contentWordsOnly,
            bool useLemmaCatModel)
        {
            this.MaxPaths = maxPaths;
            this.GoodLinkMinCount = goodLinkMinCount;
            this.BadLinkMinCount = badLinkMinCount;
            this.UseAlignModel = useAlignModel;
            this.ContentWordsOnly = contentWordsOnly;
            this.UseLemmaCatModel = useLemmaCatModel;
            this.Strongs = strongs;
            //this.glossTable = glossTable;
            this.OldLinks = oldLinks;
            this.GoodLinks = goodLinks;
            this.BadLinks = badLinks;
            this.SourceFunctionWords = sourceFunctionWords;
            this.TargetFunctionWords = targetFunctionWords;
            this.StopWords = stopWords;
            this.Puncs = puncs;
            this.manTransModel = manTransModel;
            //this.groups = groups;
        }

        public Dictionary<string, Dictionary<string, double>>? TranslationModel { get; set; }
        public Dictionary<string, Dictionary<string, double>>? TranslationModelTC { get; set; }
        public List<IReadOnlyCollection<TokensAlignedWordPair>>? AlignmentProbabilities { get; set; }
        public List<IReadOnlyCollection<TokensAlignedWordPair>>? AlignmentProbabilitiesPre { get; set; }



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
        public Dictionary<string, Dictionary<string, int>> Strongs { get; }
        //public Dictionary<string, Gloss> glossTable { get; }
        private Dictionary<string, Dictionary<string, string>> OldLinks { get; }
        private Dictionary<string, int> GoodLinks { get; }
        private Dictionary<string, int> BadLinks { get; }
        private List<string> SourceFunctionWords { get; }
        private List<string> TargetFunctionWords { get; }
        private List<string> StopWords { get; }
        private List<string> Puncs { get; }
        public TranslationModel manTransModel { get; }
        //public GroupTranslationsTable groups { get; }

        /// <summary>
        /// The maximum number of alternatives that the auto-aligner should
        /// permit during its generation of alternatives using tree traversal.
        /// </summary>
        /// 
        public int MaxPaths { get; set; }
        public int GoodLinkMinCount { get; set; }
        public int BadLinkMinCount { get; set; }

        /// <summary>
        /// True if the estimated AlignmentModel should influence the
        /// probabilities of the possible target words identified for each
        /// source segment.
        /// </summary>
        /// 
        public bool UseAlignModel { get; set; }

        /// <summary>
        /// True if the auto aligner should use content words only.
        /// </summary>
        /// 
        public bool ContentWordsOnly { get; set; }

        /// <summary>
        /// True if lemma_cat was used to create the SMT models.
        /// </summary>
        /// 
        public bool UseLemmaCatModel { get; set; }

        /// <summary>
        /// Returns true if the specified target text is punctuation.
        /// </summary>
        /// 
        public bool IsPunctuation(string text) => Puncs.Contains(text);

        /// <summary>
        /// Returns true if the specified source lemma or lowercased target
        /// text is a stop word.
        /// </summary>
        /// 
        public bool IsStopWord(string text) => StopWords.Contains(text);

        /// <summary>
        /// Returns true if the specified source lemma is a function word.
        /// </summary>
        /// 
        public bool IsSourceFunctionWord(string lemma) => SourceFunctionWords.Contains(lemma);

        /// <summary>
        /// Returns true if the specified lowercased target text is a
        /// function word.
        /// </summary>
        /// 
        public bool IsTargetFunctionWord(string text) => TargetFunctionWords.Contains(text);

        /// <summary>
        /// Returns true if the specified lemma and lowercased target
        /// text has been identified as a bad link.
        /// </summary>
        /// 
        public bool IsBadLink(string lemma, string targetTextLower)
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
        public bool IsGoodLink(string lemma, string targetTextLower)
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
        public Dictionary<string, string> OldLinksForVerse(string legacyVerseID)
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
        public double GetTranslationModelScore(string sourceLemma, string targetLemma)
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
        public bool TryGetAlignment(string sourceID, string targetID,  out double score)
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
        public bool TryGetPreAlignment(string sourceID, out string? targetID) =>
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
        public bool TryGetTranslations(string lemma, out TryGet<string, double> tryGetScoreForTargetText) =>
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
        public bool TryGetManTranslations( string lemma, out TryGet<string, double> tryGetScoreForTargetText) =>
            TryGetFromTransModel(manTransModel, lemma, out tryGetScoreForTargetText);


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
