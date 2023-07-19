using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
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
        /// - Else use Engine's versemappings.
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
			SourceTextIdToVerseMappings = sourceTextIdToVerseMappings;

			if (sourceCorpus is ScriptureTextCorpus sourceScriptureTextCorpus)
			{
				SourceCorpusVersification = sourceScriptureTextCorpus.Versification;
			}
		}
		public SourceTextIdToVerseMappings? SourceTextIdToVerseMappings { get; set; } = null;

		public ScrVers? SourceCorpusVersification { get; set; } = null;
        public virtual IEnumerable<string>? LimitToSourceBooks { get; set; } = null;

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
								.Where(tokensTextRow => r.TargetRefs
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

			var allSourceBooksNeededAndAvailable = allSourceBooksNeeded
				.Intersect(sourceBooksAvailable)
				.ToList();

            var allTargetBooksNeeded = allVerseMappings
                    .SelectMany(verseMapping => verseMapping.TargetVerses
						.Select(targetVerse => targetVerse.Book))
					.Distinct();

            var allTargetBooksNeededAndAvailable = allTargetBooksNeeded
				.Intersect(targetBooksAvailable)
				.ToList();

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
	                    .All(b => allSourceBooksNeededAndAvailable.Contains(b))
                    &&
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
                    var sTextRows = verseMapping.SourceVerses
						.SelectMany(verse => sourceTextRows
							.Where(textRow => verse.Equals((VerseRef)textRow.Ref)))
						.ToList();

					var tTextRows = verseMapping.TargetVerses
						//.Where(verse => verse.TokenIds.Count() == 0)
						.SelectMany(verse => targetTextRows
							.Where(textRow => verse.Equals((VerseRef)textRow.Ref)))
						.ToList();

					if (sTextRows.Count() != 0 || tTextRows.Count() != 0) // see ParallelTextRow.ctor()
						yield return new EngineParallelTextRow(
							sTextRows,
							verseMapping.SourceVersesCompositeTokens,
							tTextRows,
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

		public static IEnumerable<VerseMapping> VerseMappingsForAllVerses(ScrVers sourceVersification, ScrVers targetVersification)
		{
			var sourceCorpus = ScriptureTextCorpus.CreateVersificationRefCorpus(sourceVersification);
            var targetCorpus = ScriptureTextCorpus.CreateVersificationRefCorpus(targetVersification);

			return sourceCorpus.AlignRows(targetCorpus)
				.Select(parallelTextRow => new VerseMapping(
						// assume SegmentRef is VerseRef since corpora are ScriptureText.
						parallelTextRow.SourceRefs
							.Select(sourceSegmentRef => new Verse((VerseRef)sourceSegmentRef))
							.ToList(),
						parallelTextRow.TargetRefs
							.Select(targetSegmentRef => new Verse((VerseRef)targetSegmentRef))
							.ToList()
				));
        }
    }
}
