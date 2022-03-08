﻿using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Corpora
{
	/// <summary>
	/// Used by Engine to obtain the parallel text segments (e.g. verse id and verse) from parallel texts (i.e. books)
	/// using Engine's versification from its GetSegments() override. 
	/// </summary>
	public class EngineParallelText : ParallelText
	{
		private readonly IEnumerable<SourceTargetParallelVerses> _sourceTargetParallelVersesList;

		public EngineParallelText(IText sourceText, IText targetText, IEnumerable<SourceTargetParallelVerses> sourceTargetParallelVersesList,
			ITextAlignmentCollection textAlignmentCollection, IComparer<object>? segmentRefComparer = null)
			: base(sourceText, targetText, textAlignmentCollection, segmentRefComparer)
		{
			_sourceTargetParallelVersesList = sourceTargetParallelVersesList;
		}
		/// <summary>
		/// Generates parallel text segments (e.g. verse id and verse) from parallel texts (i.e. books)
		/// using Engine's versification from its GetSegments() override. 
		/// </summary>
		/// <param name="allSourceSegments"></param>
		/// <param name="allTargetSegments"></param>
		/// <param name="includeText"></param>
		/// <returns>the parallel text segments (e.g. verse id and verse) from Engine's versification.</returns>
		public override IEnumerable<ParallelTextSegment> GetSegments(bool allSourceSegments = false,
			bool allTargetSegments = false, bool includeText = true)
		{
			//ScriptureText.GetSegments is an IEnumerable based on an underlying list
			// Get the list right from the start so that each foreach iteration's Where clause doesn't have to rebuild it
			// (within XTexts.GetSegment()) when ToList() is called while supplying
			// parameters to ParallelTextSegment's ctor.
			var sourceSegments = SourceText.GetSegments(includeText).ToList(); 
			var targetSegments = TargetText.GetSegments(includeText).ToList();

			//Counting on mappings not crossing Book boundaries, otherwise
			var filteredSourceTargetParallelVersesList = _sourceTargetParallelVersesList
				.Where(s => s.sourceVerseIds
					.Select(v => v.Book)
					.Contains(SourceText.Id))
				.Where(s => s.targetVerseIds
					.Select(v => v.Book)
					.Contains(TargetText.Id));


			//Believe it may be desirable to have ParallelTextSegments in order of sourceTargetParallelVerses, e.g. for Dashboard display?
			foreach (var sourceTargetParallelVerses in filteredSourceTargetParallelVersesList)
			{
				if (sourceTargetParallelVerses != null)
				{
					/*
					var sourceTextSegments = sourceSegments
						.Where(textSegment => sourceTargetParallelVerses.sourceVerseIds.Contains(new EngineVerseId((VerseRef)textSegment.SegmentRef)))
						.SelectMany(textSegment => textSegment.Segment).ToList();
					var targetTextSegments = targetSegments
						.Where(textSegment => sourceTargetParallelVerses.targetVerseIds.Contains(new EngineVerseId((VerseRef)textSegment.SegmentRef)))
						.SelectMany(textSegment => textSegment.Segment).ToList();

					var sourceSegmentRefs = sourceSegments
						.Where(textSegment => sourceTargetParallelVerses.sourceVerseIds.Contains(new EngineVerseId((VerseRef)textSegment.SegmentRef)))
						.Select(textSegment => textSegment.SegmentRef).ToList();
					var targetSegmentRefs = targetSegments
						.Where(textSegment => sourceTargetParallelVerses.targetVerseIds.Contains(new EngineVerseId((VerseRef)textSegment.SegmentRef)))
						.Select(textSegment => textSegment.SegmentRef).ToList();
					*/

					var parallelVersesSourceSegments = sourceSegments
						.Where(textSegment => sourceTargetParallelVerses.sourceVerseIds.Contains(new EngineVerseId((VerseRef)textSegment.SegmentRef)));
					var parallelVersesTargetSegments = targetSegments
						.Where(textSegment => sourceTargetParallelVerses.targetVerseIds.Contains(new EngineVerseId((VerseRef)textSegment.SegmentRef)));
					/*
					var sourceSegmentRefs = sourceSegmentsParallelVerses
						.Select(textSegment => textSegment.SegmentRef).ToList();
					var targetSegmentRefs = sourceSegmentsParallelVerses
						.Select(textSegment => textSegment.SegmentRef).ToList();

					IReadOnlyCollection<AlignedWordPair>? alignedWordPairs = null;
					if ( (sourceSegmentRefs.Count() == 1) && (TextAlignmentCollection.Alignments.Count() > 0))
					{
						alignedWordPairs = TextAlignmentCollection.Alignments
							.Where(alignment => alignment.SegmentRef.Equals(sourceSegmentRefs.First()))
							.Select(textAlignment => textAlignment.AlignedWordPairs)
							.FirstOrDefault();
					}
					*/
					yield return new EngineParallelTextSegment(
						Id,
						parallelVersesSourceSegments,
						parallelVersesTargetSegments,
						TextAlignmentCollection
					);
				}
			}
		}
	}
}