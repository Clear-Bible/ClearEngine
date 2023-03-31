using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    public class SourceTextIdToVerseMappingsFromMachine : SourceTextIdToVerseMappingsFromVerseMappings
    {
        /// <summary>
        /// used to tell EngineParallelTextCorpus to initialize versemappings from Machine's versification, then use Engine's versemappings.
        /// </summary>
        public SourceTextIdToVerseMappingsFromMachine() : base()
        {
        }
        public IEnumerable<VerseMapping> VerseMappings
        {
            set => verseMappings_ = value;
        }
    }

    /// <summary>
    /// Used to create parallel segments of sourceCorpus with targetCorpus from method GetRows(), which returns enhanced ParallelTextRows, which
    /// is included with either source and/or target corpus when corpus TextRows have been transformed into TokensTextRows.
    /// 
    /// Can be instructed to do the following (see sourceTextIdToVerseMappings constructor parameter):
	/// 1. Use Engine's versemappings to map source segments to target segments.
	/// 2. Use Machine's versification to build Engine's versemappings then uses Engine's versemappings
    /// 3. Use Machine's versification
    /// </summary>
    public class EngineParallelTextCorpus : ParallelTextCorpus
	{
		private readonly IComparer<object>? _segmentRefComparer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceCorpus"></param>
        /// <param name="targetCorpus"></param>
        /// <param name="sourceTextIdToVerseMappings">
        /// Engine's versification mapping. 
        /// 
        /// - If null, use Machine's versification
        /// 
        /// - If is a SourceTextIdToVerseMappingsFromMachine, use Machine's versification to build Engine's versemappings, then use Engine's versemappings.
        /// 
        /// - If is not a SourceTextIdToVerseMappingsFromMachine, use Engine's versemappings.
        /// </param>
        /// <param name="textAlignmentCorpus">Prior alignment mapping to use as an override. Defaults to null.</param>
        /// <param name="rowRefComparer">The comparer used to find parallel source and target segments. Defaults to null,
        /// which uses machine's VerseRef comparer.</param>
        public EngineParallelTextCorpus(
			ITextCorpus sourceCorpus,
			ITextCorpus targetCorpus,
            SourceTextIdToVerseMappings? sourceTextIdToVerseMappings = null,
			IAlignmentCorpus? alignmentCorpus = null,
			IComparer<object>? rowRefComparer = null)
			: base(sourceCorpus, targetCorpus, alignmentCorpus, rowRefComparer = null)
		{
			if (sourceTextIdToVerseMappings != null && sourceTextIdToVerseMappings is SourceTextIdToVerseMappingsFromMachine)
			{
				_segmentRefComparer = rowRefComparer;

				List<VerseMapping> verseMappings = new List<VerseMapping>();
				//Versifications as used in machine doesn't support combining verses.

				_ = base.GetRows()
					.Select(parallelTextRow =>
						{
							var verseMapping = new VerseMapping
							(
								// assume SegmentRef is VerseRef since corpora are ScriptureTextCorpus.
								parallelTextRow.SourceRefs
									.Select(sourceSegmentRef => new Verse((VerseRef)sourceSegmentRef)),
								parallelTextRow.TargetRefs
									.Select(targetSegmentRef => new Verse((VerseRef)targetSegmentRef))

							);
							verseMappings.Add(verseMapping);
							return verseMapping;
						}
					).ToList(); //cause the enumerable to evaluate

				((SourceTextIdToVerseMappingsFromMachine) sourceTextIdToVerseMappings).VerseMappings 
					= verseMappings.Validate(sourceCorpus, targetCorpus);
				SourceTextIdToVerseMappings = sourceTextIdToVerseMappings;
			}
			else if (sourceTextIdToVerseMappings != null) // sourceTextIdToVerseMappings is NOT SourceTextIdToVerseMappingsFromMachine
            {
				//for rebuilding map from file: use to VerseRef.VerseRef(int bookNum, int chapterNum, int verseNum, ScrVers versification) constructor.
				SourceTextIdToVerseMappings = sourceTextIdToVerseMappings;
			}
            else //sourceTextIdToVerseMappings == null
            {
				SourceTextIdToVerseMappings = null;
			}

			if (sourceCorpus is ScriptureTextCorpus sourceScriptureTextCorpus)
			{
				SourceCorpusVersification = sourceScriptureTextCorpus.Versification;
			}
		}
		public SourceTextIdToVerseMappings? SourceTextIdToVerseMappings { get; set; } = null;

		public ScrVers? SourceCorpusVersification { get; set; }

		public IEnumerable<string>? LimitToSourceBooks { get; set; } = null;

        public override IEnumerator<ParallelTextRow> GetEnumerator()
		{
            if (SourceTextIdToVerseMappings == null)
            {
                IEnumerable<string> sourceTextIds = SourceCorpus.Texts.Select(t => t.Id);
                IEnumerable<string> targetTextIds = TargetCorpus.Texts.Select(t => t.Id);

                IEnumerable<string> textIds;
				if (AllSourceRows && AllTargetRows)
					textIds = sourceTextIds.Union(targetTextIds);
				else if (!AllSourceRows && !AllTargetRows)
				{
					textIds = sourceTextIds.Intersect(targetTextIds);
                    if (LimitToSourceBooks != null)
                    {
                        textIds = LimitToSourceBooks.Intersect(textIds);
                    }
                }
				else if (AllSourceRows)
					textIds = sourceTextIds;
				else
					textIds = targetTextIds;

                //ScriptureText.GetSegments is an IEnumerable based on an underlying list (ScriptureText.GetRows())
                //so retrieving at once shouldn't impact performance or memory.
                List<TextRow> sourceTextRows = SourceCorpus.GetRows(textIds).ToList();
				List<TextRow> targetTextRows = TargetCorpus.GetRows(textIds).ToList();

				return GetRows()
					.Select(r =>
					{
						List<Token> sourceTokens = new();
						try
						{
							//Only set SourceTokens if all the members of sourceSegments can be cast to a TokensTextSegment
							sourceTokens = sourceTextRows
								.Cast<TokensTextRow>()  //throws an invalidCastException if any of the members can't be cast to type
								.Where(tokensTextRow => r.SourceRefs
									.Cast<VerseRef>()
									.Contains((VerseRef)tokensTextRow.Ref)) //sourceTextRows that pertain to paralleltextrow's sourcerefs
								.SelectMany(tokensTextRow => tokensTextRow.Tokens)
								.ToList();
						}
						catch (InvalidCastException)
						{
						}

						List<Token> targetTokens = new();
						try
						{
							targetTokens = targetTextRows
								.Cast<TokensTextRow>() //throws an invalidCastException if any of the members can't be cast to type
								.Where(tokensTextRow => r.SourceRefs
									.Cast<VerseRef>()
									.Contains((VerseRef)tokensTextRow.Ref)) //targetTextRows that pertain to paralleltextrow's targetrefs
								.SelectMany(tokensTextRow => tokensTextRow.Tokens)
								.ToList();
						}
						catch (InvalidCastException)
						{
						}

						return new EngineParallelTextRow(r, sourceTokens, targetTokens);
					})
					.GetEnumerator();
			}
			else
			{
				return GetRowsUsingVerseMappingList().GetEnumerator();
			}
		}

		public IEnumerable<ParallelTextRow> GetRowsUsingVerseMappingList()
		{

			if (SourceTextIdToVerseMappings == null)
			{
				throw new InvalidStateEngineException(message: "Member must not be null to use this method", name: "method", value: nameof(SourceTextIdToVerseMappings));
			}

			bool corporaHaveTokensTextRows = false;
			if ((SourceCorpus.Any() && SourceCorpus.First() is TokensTextRow) && (TargetCorpus.Any() && TargetCorpus.First() is TokensTextRow)) //don't use .Cast<>() approach because it needs to be evaluated which will impact memory.
			{
				corporaHaveTokensTextRows = true;
			}

			var sourceBooksAvailable = SourceCorpus.Texts.Select(t => t.Id);
			var targetBooksAvailable = TargetCorpus.Texts.Select(t => t.Id);

			var sourceBooks = LimitToSourceBooks ?? sourceBooksAvailable;

			var allVerseMappings = sourceBooks
					.SelectMany(sourceTextId => SourceTextIdToVerseMappings[sourceTextId])
					.ToList();

            var allSourceBooksNeeded = allVerseMappings
                    .SelectMany(verseMapping => verseMapping.SourceVerses
						.Select(sourceVerse => sourceVerse.Book))
					.Distinct();

			var allSourceBooksNeededAndAvailable = allSourceBooksNeeded.Intersect(sourceBooksAvailable);

            var allTargetBooksNeeded = allVerseMappings
                    .SelectMany(verseMapping => verseMapping.TargetVerses
						.Select(targetVerse => targetVerse.Book))
					.Distinct();

            var allTargetBooksNeededAndAvailable = allTargetBooksNeeded.Intersect(targetBooksAvailable);

            // get verse mappings that are supported by the sourceTextIds and targetTextIds
            var verseMappingsNeededAndAvailable = allVerseMappings
                .Where(verseMapping =>   // filter for only verse mappings where all the source verses are associated with books in sourceTextIds
					verseMapping.SourceVerses
						.Where(verse => verse.TokenIds.Count() == 0) // either for verses that have no token ids
						.Select(v => v.Book)
						.Distinct()
						.All(b => allSourceBooksNeededAndAvailable.Contains(b))
                    &&
                    verseMapping.SourceVerses
						.Where(verse => verse.TokenIds.Count() > 0) //or verses that do have token ids.
						.SelectMany(v => v.TokenIds)
						.Select(t => t.Book)
                        .Distinct()
	                    .All(b => allSourceBooksNeededAndAvailable.Contains(b)))
                .Where(verseMapping => // further filter for only verse mappings where all the target verses are associated with books in targetTextIds
                    verseMapping.TargetVerses
                        .Where(verse => verse.TokenIds.Count() == 0) // either for verses that have no token ids
                        .Select(v => v.Book)
                        .Distinct()
                        .All(b => allTargetBooksNeededAndAvailable.Contains(b))
                    &&
                    verseMapping.TargetVerses
                        .Where(verse => verse.TokenIds.Count() > 0) //or verses that do have token ids.
                        .SelectMany(v => v.TokenIds)
                        .Select(t => t.Book)
                        .Distinct()
                        .All(b => allTargetBooksNeededAndAvailable.Contains(b)));

			//now figure out the actual set of srcTextIds for which rows will be needed and get them.
            var sourceBooksNeededAndAvailable = verseMappingsNeededAndAvailable
                .SelectMany(vm => vm.SourceVerses
                    .Select(v => v.Book))
                .Distinct();
            List<TextRow> sourceTextRows = SourceCorpus.GetRows(sourceBooksNeededAndAvailable).ToList();

            //and figure out the actual set of trgTextIds for which rows will be needed and get them.
            var targetBooksNeededAndAvailable = verseMappingsNeededAndAvailable
                .SelectMany(vm => vm.TargetVerses
                    .Select(v => v.Book))
                .Distinct();
            List<TextRow> targetTextRows = TargetCorpus.GetRows(targetBooksNeededAndAvailable).ToList();

			//now iterate the verse mappings, getting the verses for each and constructing a paralleltextrow
            foreach (var verseMapping in verseMappingsNeededAndAvailable)
            {
				bool notTokenMode = verseMapping.SourceVerses
					.SelectMany(verse => verse.TokenIds)
					.Count() == 0
					&&
				verseMapping.TargetVerses
					.SelectMany(verse => verse.TokenIds)
					.Count() == 0;

				if (!notTokenMode && !corporaHaveTokensTextRows) //token mode and corpora arent tokens text row
                {
					throw new InvalidTypeEngineException(name: "VerseMapping", value: verseMapping.ToString(), message: "Corpora are not tokens textrow and a versemapping contains one or more Verses with TokenIds.");
				}

				if (notTokenMode)
				{
                    var parallelSourceTextRows = verseMapping.SourceVerses
						.SelectMany(verse => sourceTextRows
							.Where(textRow => (new Verse((VerseRef)textRow.Ref).Equals(verse))));

					var parallelTargetTextRows = verseMapping.TargetVerses
						//.Where(verse => verse.TokenIds.Count() == 0)
						.SelectMany(verse => targetTextRows
							.Where(textRow => (new Verse((VerseRef)textRow.Ref).Equals(verse))));

					yield return new EngineParallelTextRow(
						parallelSourceTextRows,
                        verseMapping.SourceVersesCompositeTokens,
                        parallelTargetTextRows,
                        verseMapping.TargetVersesCompositeTokens,
                        AlignmentCorpus
					);
				}
				else //token mode
                {
					throw new NotImplementedException();
                }
			}
		}

		protected override TargetCorpusEnumerator GetTargetCorpusEnumerator(IEnumerator<TextRow> enumerator)
		{
			return new EngineTargetCorpusEnumerator(enumerator);
		}
		protected class EngineTargetCorpusEnumerator : TargetCorpusEnumerator
        {
			public EngineTargetCorpusEnumerator(IEnumerator<TextRow> enumerator) : base(enumerator) 
			{ 
			}
			protected override TextRow CreateTextRow(TextRow textRow, TextRow? concatSegmentTextRow = null)
			{
				if (textRow is TokensTextRow)
				{
					if (concatSegmentTextRow != null && concatSegmentTextRow is not TokensTextRow)
                    {
						throw new InvalidTypeEngineException(name: "concatSegmentTextRow", value: "not TokensTextRow", message: "concatSegmentTextRow should always be TokensTextRow if textRow is TokensTextRow. ");
                    }
					else if (concatSegmentTextRow != null)
                    {
						var newTextRow = new TokensTextRow(textRow.Ref, ((TokensTextRow)textRow).Tokens.Concat(((TokensTextRow)concatSegmentTextRow).Tokens).ToArray());
						return newTextRow;
					}
					else //textRowSegmentConcatted == null
					{
						return new TokensTextRow(textRow.Ref);
					}
				}
				else
				{
					return base.CreateTextRow(textRow, concatSegmentTextRow);
				}
			}
		}
	}
}
