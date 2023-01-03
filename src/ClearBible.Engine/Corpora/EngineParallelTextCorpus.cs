using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Scripture;
using static ClearBible.Engine.Persistence.FileGetBookIds;

namespace ClearBible.Engine.Corpora
{
	/// <summary>
	/// Used to create parallel segments of sourceCorpus with targetCorpus from method GetRows(), which returns enhanced ParallelTextRows, which
	/// is included with either source and/or target corpus when corpus TextRows have been transformed into TokensTextRows.
	/// 
	/// Uses Engine's versification to map source segments to target segments if provided with values.
	/// Uses Machine's versificattion to build Engine's versification then uses Engine's versification if provided empty.
	/// Uses Machine's versification if null
	/// </summary>
	public class EngineParallelTextCorpus : ParallelTextCorpus
	{
		private readonly IComparer<object>? _segmentRefComparer;

		private string? _limitToBookAbbreviation = null;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceCorpus"></param>
		/// <param name="targetCorpus"></param>
		/// <param name="verseMappingList">
		/// Engine's versification mapping. 
		/// 
		/// - If null, use Machine's versification.
		/// 
		/// - If empty, initialize with Machine's versification then use Engine's versification.
		/// 
		/// - If not empty, use Engine's versification
		/// </param>
		/// <param name="textAlignmentCorpus">Prior alignment mapping to use as an override. Defaults to null.</param>
		/// <param name="rowRefComparer">The comparer used to find parallel source and target segments. Defaults to null,
		/// which uses machine's VerseRef comparer.</param>
		public EngineParallelTextCorpus(
			ITextCorpus sourceCorpus,
			ITextCorpus targetCorpus,
			List<VerseMapping>? verseMappingList = null,
			IAlignmentCorpus? alignmentCorpus = null,
			IComparer<object>? rowRefComparer = null)
			: base(sourceCorpus, targetCorpus, alignmentCorpus, rowRefComparer = null)
		{
			// If engineVersificationMappingList is null, use Machine/sil/scripture versification to create it.

			if (verseMappingList != null && verseMappingList.Count == 0)
			{
				_segmentRefComparer = rowRefComparer;

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
							verseMappingList.Add(verseMapping);
							return verseMapping;
						}
					).ToList(); //cause the enumerable to evaluate

				VerseMappingList = verseMappingList.Validate(sourceCorpus, targetCorpus);
			}
			else if (verseMappingList != null) // && verseMappingList.Count > 0
			{
				//for rebuilding map from file: use to VerseRef.VerseRef(int bookNum, int chapterNum, int verseNum, ScrVers versification) constructor.
				VerseMappingList = verseMappingList;
			}
			if (sourceCorpus is ScriptureTextCorpus sourceScriptureTextCorpus)
			{
				SourceCorpusVersification = sourceScriptureTextCorpus.Versification;
			}
		}
		public List<VerseMapping>? VerseMappingList { get; set; } = null;

		public ScrVers? SourceCorpusVersification { get; }

		public IEnumerable<ParallelTextRow> SetLimitToBook(string? bookAbbreviation)
		{
            _limitToBookAbbreviation = bookAbbreviation;
            return this;
        }
        public IEnumerable<ParallelTextRow> SetLimitToBook(int? bookNumber = null)
        {
            if (bookNumber == null)
            {
                _limitToBookAbbreviation = null;
            }
            else
            {
                _limitToBookAbbreviation = BookIds
                    .Where(b => int.Parse(b.silCannonBookNum) == bookNumber)
                    .FirstOrDefault()?
                    .silCannonBookAbbrev
                    ?? throw new InvalidBookMappingEngineException(message: "Doesn't exist", name: "silCannonBookNum", value: bookNumber?.ToString() ?? "null");
            }
			return this;
        }
        public string? GetLimitToBook()
        {
			return _limitToBookAbbreviation;
        }

        public override IEnumerator<ParallelTextRow> GetEnumerator()
		{
			IEnumerable<string> sourceTextIds = SourceCorpus.Texts.Select(t => t.Id);
			IEnumerable<string> targetTextIds = TargetCorpus.Texts.Select(t => t.Id);

			IEnumerable<string> textIds;
			if (AllSourceRows && AllTargetRows)
				textIds = sourceTextIds.Union(targetTextIds);
			else if (!AllSourceRows && !AllTargetRows)
			{
				textIds = sourceTextIds.Intersect(targetTextIds);
				if (_limitToBookAbbreviation != null)
				{
					textIds = new List<string>() { _limitToBookAbbreviation }.Intersect(textIds);
				}
			}
			else if (AllSourceRows)
				textIds = sourceTextIds;
			else
				textIds = targetTextIds;

			if (VerseMappingList == null)
			{
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
								.Where(tokensTextRow => r.SourceRefs.Cast<VerseRef>().Contains((VerseRef)tokensTextRow.Ref)) //sourceTextRows that pertain to paralleltextrow's sourcerefs
								.SelectMany(tokensTextRow => tokensTextRow.Tokens).ToList();
						}
						catch (InvalidCastException)
						{
						}

						List<Token> targetTokens = new();
						try
						{
							targetTokens = targetTextRows
								.Cast<TokensTextRow>() //throws an invalidCastException if any of the members can't be cast to type
								.Where(tokensTextRow => r.SourceRefs.Cast<VerseRef>().Contains((VerseRef)tokensTextRow.Ref)) //targetTextRows that pertain to paralleltextrow's targetrefs
								.SelectMany(tokensTextRow => tokensTextRow.Tokens).ToList();
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
				return GetRowsUsingVerseMappingList(textIds).GetEnumerator();
			}
		}

		public IEnumerable<ParallelTextRow> GetRowsUsingVerseMappingList(IEnumerable<string> textIds)
		{

			if (VerseMappingList == null)
			{
				throw new InvalidStateEngineException(message: "Member must not be null to use this method", name: "method", value: nameof(VerseMappingList));
			}

			bool corporaHaveTokensTextRows = false;
			if ((SourceCorpus.Any() && SourceCorpus.First() is TokensTextRow) && (TargetCorpus.Any() && TargetCorpus.First() is TokensTextRow)) //don't use .Cast<>() approach because it needs to be evaluated which will impact memory.
			{
				corporaHaveTokensTextRows = true;
			}

			foreach (var text in textIds)
            {
				List<TextRow> sourceTextRows = SourceCorpus.GetRows(new List<string>() { text }).ToList();
				List<TextRow> targetTextRows = TargetCorpus.GetRows(new List<string>() { text }).ToList();

				foreach (var verseMappingForBook in VerseMappingList // resulting verses are in VerseMappingList order
					.Where(verseMapping => verseMapping.SourceVerses
						.First().Book.Equals(text))) 
					//already verified in this that all Verses in each VerseMapping is from same book from call to Validate(this List<VerseMapping> ....) extension method
					//.Where(verseMapping => verseMapping.SourceVerses
					//	.Select(verse => verse.Book)
					//	.Except(new List<string>() { text })
					//	.Count() == 0))
					//.Where(mapping => mapping.TargetVerses // this should never filter anything out since Validate(this List<VerseMapping> ....) extension method ensures that the verses in every versemapping are from the same book.
					//	.Select(tvid => tvid.Book)
					//	.Except(new List<string>() { text })
					//	.Count() == 0))
				{

					bool notTokenMode = verseMappingForBook.SourceVerses
						.SelectMany(verse => verse.TokenIds)
						.Count() == 0
						&&
					verseMappingForBook.TargetVerses
						.SelectMany(verse => verse.TokenIds)
						.Count() == 0;

					if (!notTokenMode && !corporaHaveTokensTextRows) //token mode and corpora arent tokens text row
                    {
						throw new InvalidTypeEngineException(name: "VerseMapping", value: verseMappingForBook.ToString(), message: "Corpora are not tokens textrow and a versemapping contains one or more Verses with TokenIds.");
					}

					if (notTokenMode)
					{
						var parallelSourceTextRows = verseMappingForBook.SourceVerses
							.SelectMany(verse => sourceTextRows
								.Where(textRow => (new Verse((VerseRef)textRow.Ref).Equals(verse))));

						var parallelTargetTextRows = verseMappingForBook.TargetVerses
							.Where(verse => verse.TokenIds.Count() == 0)
							.SelectMany(verse => targetTextRows
								.Where(textRow => (new Verse((VerseRef)textRow.Ref).Equals(verse))));

						yield return new EngineParallelTextRow(
							parallelSourceTextRows,
                            parallelTargetTextRows,
							AlignmentCorpus
						);
					}
					else //token mode
                    {
						throw new NotImplementedException();
                    }
				}
			}

			/*
			List<TextRow> sourceTextRows = SourceCorpus.GetRows(textIds).ToList();
			List<TextRow> targetTextRows = TargetCorpus.GetRows(textIds).ToList();


			//Believe it may be desirable to have ParallelTextSegments in order of VerseMappingList, e.g. for Dashboard display?
			if (VerseMappingList == null)
			{
				throw new InvalidStateEngineException(message: "Member must not be null to use this method", name: "method", value: nameof(VerseMappingList));
			}

			// only include versemappings where both source and target lists only contain books in textIds.
			foreach (var verseMapping in VerseMappingList
				.Where(mapping => mapping.SourceVerses
					.Select(svid => svid.Book)
					.Except(textIds)
					.Count() == 0)
				.Where(mapping => mapping.TargetVerses
					.Select(tvid => tvid.Book)
					.Except(textIds)
					.Count() == 0))
			{
				var parallelSourceTextRows = sourceTextRows
					.Where(textRow => verseMapping.SourceVerses.Contains(new Verse((VerseRef)textRow.Ref)));
				var parallelTargetTextRows = targetTextRows
					.Where(textRow => verseMapping.TargetVerses.Contains(new Verse((VerseRef)textRow.Ref)));

				yield return new EngineParallelTextRow(
					parallelSourceTextRows,
					parallelTargetTextRows,
					AlignmentCorpus
				);
			}
			*/
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
