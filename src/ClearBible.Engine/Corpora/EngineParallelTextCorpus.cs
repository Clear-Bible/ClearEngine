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
    public record SourceTargetParallelVerses(IEnumerable<EngineVerseId> sourceVerseIds, IEnumerable<EngineVerseId> targetVerseIds);
	public static class Extensions
	{
		public static List<SourceTargetParallelVerses> Validate(this List<SourceTargetParallelVerses> list, IEnumerable<TextRow> sourceCorpus, IEnumerable<TextRow> targetCorpus)
		{
			//FIXME:validate that entries in each enumerable in SourceTargetParallelVerses are from the same book and chapter.
			//FIXME: Validate that entries in each enumerable have accompanying entries that together comprise a file verse grouping, e.g. "Fee fi fo" is for verses 1-3.
			return list;
		}
	}
	#endregion

	/// <summary>
	/// Used to create parallel segments of sourceCorpus with targetCorpus from method GetSegments(), which returns parallel segments.
	/// 
	/// Uses Engine's versification to map source segments to target segments.
	/// 
	/// If Engine's versification mapping is not provided through sourceTargetParallelVersesList, this class builds it
	/// from Machine's versification. 
	/// </summary>
	public class EngineParallelTextCorpus : ParallelTextCorpus
	{
		private readonly IComparer<object>? _segmentRefComparer;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceCorpus"></param>
		/// <param name="targetCorpus"></param>
		/// <param name="sourceTargetParallelVersesList">Engine's versification mapping. If not supplied, this
		/// class builds it from Machine's versification. Once built, Engine's versification mapping can be obtained
		/// from property SourceTargetParallelVersesList</param>
		/// <param name="textAlignmentCorpus">Prior alignment mapping to use as an override. Defaults to null.</param>
		/// <param name="segmentRefComparer">The comparer used to find parallel source and target segments. Defaults to null,
		/// which uses machine's VerseRef comparer.</param>
		public EngineParallelTextCorpus(
			IEnumerable<TextRow> sourceCorpus,
			IEnumerable<TextRow> targetCorpus,
			List<SourceTargetParallelVerses>? sourceTargetParallelVersesList = null,
			IEnumerable<AlignmentRow>? alignmentCorpus = null,
			IComparer<object>? segmentRefComparer = null)
			: base(sourceCorpus, targetCorpus, alignmentCorpus, segmentRefComparer = null)
		{
			// If sourceTargetParallelVerses is null, use Machine/sil/scripture versification to create it.
			if (sourceTargetParallelVersesList == null)
            {
				_segmentRefComparer = segmentRefComparer;

                //Versifications as used in machine doesn't support combining verses.
                sourceTargetParallelVersesList = new();

                _ = this
					.Select(parallelTextRow =>
						{
							var sourceTargetParallelVerses = new SourceTargetParallelVerses
							(
								// assume SegmentRef is VerseRef since corpora are ScriptureTextCorpus.
								parallelTextRow.SourceRefs
									.Select(sourceSegmentRef => new EngineVerseId((VerseRef)sourceSegmentRef)),
								parallelTextRow.TargetRefs
									.Select(targetSegmentRef => new EngineVerseId((VerseRef)targetSegmentRef))

							);
							sourceTargetParallelVersesList.Add(sourceTargetParallelVerses);
							return sourceTargetParallelVerses;
						}
					).ToList(); //cause the enumerable to evaluate

				SourceTargetParallelVersesList = sourceTargetParallelVersesList.Validate(sourceCorpus, targetCorpus);
			}
			else
			{
				//for rebuilding map from file: use to VerseRef.VerseRef(int bookNum, int chapterNum, int verseNum, ScrVers versification) constructor.
				SourceTargetParallelVersesList = sourceTargetParallelVersesList;
			}
		}
        public List<SourceTargetParallelVerses> SourceTargetParallelVersesList { get; set; }


		private List<TextRow>? _sourceTextRows = null;
		private List<TextRow>? _targetTextRows = null;

		protected override IEnumerable<ParallelTextRow> GetRows()
		{
			if (SourceTargetParallelVersesList == null)
			{
				var enumerator = base.GetRows().GetEnumerator();
				bool succeeded = enumerator.MoveNext();
				yield return enumerator.Current;
			}
			else
			{
				//ScriptureText.GetSegments is an IEnumerable based on an underlying list (ScriptureText.GetRows())
				// Get the list right from the start so that each foreach iteration's Where clause doesn't have to rebuild it
				// (within XTexts.GetSegment()) when ToList() is called while supplying
				// parameters to ParallelTextSegment's ctor.

				if (_sourceTextRows == null)
				{
					_sourceTextRows = SourceCorpus.ToList();
				}
				if (_targetTextRows == null)
				{
					_targetTextRows = TargetCorpus.ToList();
				}

				/* Not by ParallelText anymore
				//Counting on mappings not crossing Book boundaries
				var filteredSourceTargetParallelVersesList = SourceTargetParallelVersesList
					.Where(s => s.sourceVerseIds
						.Select(v => v.Book)
						.Contains(SourceText.Id))
					.Where(s => s.targetVerseIds
						.Select(v => v.Book)
						.Contains(TargetText.Id));
				*/

				//Believe it may be desirable to have ParallelTextSegments in order of sourceTargetParallelVerses, e.g. for Dashboard display?
				foreach (var sourceTargetParallelVerses in SourceTargetParallelVersesList)
				{
					if (sourceTargetParallelVerses != null)
					{
#pragma warning disable CS8604 // Already checked for null
						var sourceTextRows = (_sourceTextRows)
#pragma warning restore CS8604 // Already checked for null
						.Where(textRow => sourceTargetParallelVerses.sourceVerseIds.Contains(new EngineVerseId((VerseRef)textRow.Ref)));
#pragma warning disable CS8604 // Already checked for null
						var targetTextRows = (_targetTextRows)
#pragma warning restore CS8604 // Already checked for null
						.Where(textRow => sourceTargetParallelVerses.targetVerseIds.Contains(new EngineVerseId((VerseRef)textRow.Ref)));

						var sourceTextIds = sourceTextRows
							.Select(textRole => textRole.TextId)
							.Distinct();
						var targetTextIds = targetTextRows
							.Select(textRole => textRole.TextId)
							.Distinct();

						if (sourceTextIds?.Count() != 1 || targetTextIds?.Count() != 1)
                        {
							throw new InvalidDataException(@$"Versified verses are from different books.  source: {string.Join(" ", sourceTextIds ?? new string[] {"EMPTY" })} 
								target: {string.Join(" ", targetTextIds ?? new string[] { "EMPTY" })}");
                        }

						yield return new EngineParallelTextRow(
							sourceTextIds.FirstOrDefault() ?? targetTextIds.FirstOrDefault() ?? "",
							sourceTextRows,
							targetTextRows,
							AlignmentCorpus
						);

					}
				}
			}
		}
	}
}
