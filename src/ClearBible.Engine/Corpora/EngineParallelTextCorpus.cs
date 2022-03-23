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
		public static List<SourceTargetParallelVerses> Validate(this List<SourceTargetParallelVerses> list, IEngineCorpus sourceCorpus, IEngineCorpus targetCorpus)
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
			IEngineCorpus sourceCorpus,
			IEngineCorpus targetCorpus,
			List<SourceTargetParallelVerses>? sourceTargetParallelVersesList = null,
			ITextAlignmentCorpus? textAlignmentCorpus = null,
			IComparer<object>? segmentRefComparer = null)
			: base(sourceCorpus, targetCorpus, textAlignmentCorpus, segmentRefComparer = null)
		{
			// If sourceTargetParallelVerses is null, use Machine/sil/scripture versification to create it.
			if (sourceTargetParallelVersesList == null)
            {
				_segmentRefComparer = segmentRefComparer;

                //Versifications as used in machine doesn't support combining verses.
                sourceTargetParallelVersesList = new();

                _ = GetSegments(includeText: false)
					.Select(parallelTextSegment =>
						{
							var sourceTargetParallelVerses = new SourceTargetParallelVerses
							(
								// assume SegmentRef is VerseRef since corpora are ScriptureTextCorpus.
								parallelTextSegment.SourceSegmentRefs
									.Select(sourceSegmentRef => new EngineVerseId((VerseRef)sourceSegmentRef)),
								parallelTextSegment.TargetSegmentRefs
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
			sourceCorpus.Train(this, sourceCorpus);
			targetCorpus.Train(this, targetCorpus);
		}
        public List<SourceTargetParallelVerses> SourceTargetParallelVersesList { get; set; }


		public override ParallelTextCorpus Invert()
		{
			return new EngineParallelTextCorpus((IEngineCorpus) TargetCorpus, (IEngineCorpus) SourceCorpus, SourceTargetParallelVersesList, TextAlignmentCorpus.Invert());
		}

		protected override ParallelText CreateParallelText(string id)
		{
			IText sourceText = SourceCorpus[id];
			IText targetText = TargetCorpus[id];

			if (SourceTargetParallelVersesList != null)
            {
				((IEngineCorpus)SourceCorpus).DoMachineVersification = false;
				((IEngineCorpus)TargetCorpus).DoMachineVersification = false;
				ITextAlignmentCollection textAlignmentCollection = TextAlignmentCorpus[id];
				return new EngineParallelText(sourceText, targetText, SourceTargetParallelVersesList, textAlignmentCollection, _segmentRefComparer);
			}
			else
            {
				((IEngineCorpus)SourceCorpus).DoMachineVersification = true;
				((IEngineCorpus)TargetCorpus).DoMachineVersification = true;
				ITextAlignmentCollection textAlignmentCollection = TextAlignmentCorpus[id];
				return new ParallelText(sourceText, targetText, textAlignmentCollection, _segmentRefComparer);
			}
		}
	}
}
