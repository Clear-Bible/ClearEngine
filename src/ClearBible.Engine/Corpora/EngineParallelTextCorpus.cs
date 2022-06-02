using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
    #region Versification map data structure

	public record EngineVerseId
    {
		public EngineVerseId(string book, int chapterNum, int verseNum)
        {
			Book = book;
			ChapterNum = chapterNum;
			VerseNum = verseNum;
        }

		public EngineVerseId(VerseRef verseRef)
        {
			Book = verseRef.Book;
			ChapterNum = verseRef.ChapterNum;
			VerseNum = verseRef.VerseNum;
        }

		public string Book { get; }
		public int ChapterNum { get; }
		public int VerseNum { get;  }
	}
	/// <summary>
	/// Engine's versification map structure.
	/// </summary>
	/// <param name="sourceVerseRefs"></param>
	/// <param name="targetVerseRefs"></param>
    public record EngineVerseMapping(IEnumerable<EngineVerseId> sourceVerseIds, IEnumerable<EngineVerseId> targetVerseIds);
	public static class EngineVerseMappingExtensions
	{
		public static List<EngineVerseMapping> Validate(this List<EngineVerseMapping> list, IEnumerable<TextRow> sourceCorpus, IEnumerable<TextRow> targetCorpus)
		{
			//FIXME:validate that entries in each enumerable in engineVerseMapping are from the same book and chapter.
			//FIXME: Validate that entries in each enumerable have accompanying entries that together comprise a file verse grouping, e.g. "Fee fi fo" is for verses 1-3.
			return list;
		}
	}
	#endregion

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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceCorpus"></param>
		/// <param name="targetCorpus"></param>
		/// <param name="engineVerseMappingList">
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
			List<EngineVerseMapping>? engineVerseMappingList = null,
			IAlignmentCorpus? alignmentCorpus = null,
			IComparer<object>? rowRefComparer = null)
			: base(sourceCorpus, targetCorpus, alignmentCorpus, rowRefComparer = null)
		{
			// If engineVersificationMappingList is null, use Machine/sil/scripture versification to create it.

			if (engineVerseMappingList != null && engineVerseMappingList.Count == 0)
            {
				_segmentRefComparer = rowRefComparer;

				//Versifications as used in machine doesn't support combining verses.

                _ = base.GetRows()
					.Select(parallelTextRow =>
						{
							var engineVerseMapping = new EngineVerseMapping
							(
								// assume SegmentRef is VerseRef since corpora are ScriptureTextCorpus.
								parallelTextRow.SourceRefs
									.Select(sourceSegmentRef => new EngineVerseId((VerseRef)sourceSegmentRef)),
								parallelTextRow.TargetRefs
									.Select(targetSegmentRef => new EngineVerseId((VerseRef)targetSegmentRef))

							);
							engineVerseMappingList.Add(engineVerseMapping);
							return engineVerseMapping;
						}
					).ToList(); //cause the enumerable to evaluate

				EngineVerseMappingList = engineVerseMappingList.Validate(sourceCorpus, targetCorpus);
			}
			else if (engineVerseMappingList != null) // && engineVerseMappingList.Count > 0
			{
				//for rebuilding map from file: use to VerseRef.VerseRef(int bookNum, int chapterNum, int verseNum, ScrVers versification) constructor.
				EngineVerseMappingList = engineVerseMappingList;
			}
		}
		public List<EngineVerseMapping>? EngineVerseMappingList { get; set; } = null;

		public override IEnumerator<ParallelTextRow> GetEnumerator()
		{
			IEnumerable<string> sourceTextIds = SourceCorpus.Texts.Select(t => t.Id);
			IEnumerable<string> targetTextIds = TargetCorpus.Texts.Select(t => t.Id);

			IEnumerable<string> textIds;
			if (AllSourceRows && AllTargetRows)
				textIds = sourceTextIds.Union(targetTextIds);
			else if (!AllSourceRows && !AllTargetRows)
				textIds = sourceTextIds.Intersect(targetTextIds);
			else if (AllSourceRows)
				textIds = sourceTextIds;
			else
				textIds = targetTextIds;

			if (EngineVerseMappingList == null)
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
							sourceTextRows.Cast<TokensTextRow>(); //throws an invalidCastException if any of the members can't be cast to type
							sourceTokens = sourceTextRows
								.Where(tr => r.SourceRefs.Cast<VerseRef>().Contains((VerseRef)tr.Ref)) //sourceTextRows that pertain to paralleltextrow's sourcerefs
								.SelectMany(textRow => ((TokensTextRow)textRow).Tokens).ToList();
						}
						catch (InvalidCastException)
						{
						}

						List<Token> targetTokens = new();
						try
						{
							targetTextRows.Cast<TokensTextRow>(); //throws an invalidCastException if any of the members can't be cast to type
							targetTokens = targetTextRows
								.Where(tr => r.SourceRefs.Cast<VerseRef>().Contains((VerseRef)tr.Ref)) //targetTextRows that pertain to paralleltextrow's targetrefs
								.SelectMany(textRow => ((TokensTextRow)textRow).Tokens).ToList();
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
				return GetRowsUsingEngineVerseMappingList(textIds).GetEnumerator();
			}
		}

		public IEnumerable<ParallelTextRow> GetRowsUsingEngineVerseMappingList(IEnumerable<string> textIds)
		{
			List<TextRow> sourceTextRows = SourceCorpus.GetRows(textIds).ToList();
			List<TextRow> targetTextRows = TargetCorpus.GetRows(textIds).ToList();

			//Believe it may be desirable to have ParallelTextSegments in order of EngineVerseMappingList, e.g. for Dashboard display?
			if (EngineVerseMappingList == null)
            {
				throw new InvalidStateEngineException(message: "Member must not be null to use this method", name: "method", value: nameof(EngineVerseMappingList));
            }

			// only include versemappings where both source and target lists only contain books in textIds.
			foreach (var engineVerseMapping in EngineVerseMappingList
				.Where(mapping => mapping.sourceVerseIds
					.Select(svid => svid.Book)
					.Except(textIds)
					.Count() == 0)
				.Where(mapping => mapping.targetVerseIds
					.Select(tvid => tvid.Book)
					.Except(textIds)
					.Count() == 0))
			{
				var parallelSourceTextRows = sourceTextRows
					.Where(textRow => engineVerseMapping.sourceVerseIds.Contains(new EngineVerseId((VerseRef)textRow.Ref)));
				var parallelTargetTextRows = targetTextRows
					.Where(textRow => engineVerseMapping.targetVerseIds.Contains(new EngineVerseId((VerseRef)textRow.Ref)));

				yield return new EngineParallelTextRow(
					parallelSourceTextRows,
					parallelTargetTextRows,
					AlignmentCorpus
				);
			}
		}
	}
}
