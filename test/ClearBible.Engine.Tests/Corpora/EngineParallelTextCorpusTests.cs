using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Engine.Tests.Corpora
{
    public class EngineParallelTextCorpusTests
    {
		[Fact]
		public void GetGetRows_SameVerseRefOneToMany()
		{
			Versification.Table.Implementation.RemoveAllUnknownVersifications();
			string src = "&MAT 1:2-3 = MAT 1:2\nMAT 1:4 = MAT 1:3\n";
			ScrVers versification;
			using (var reader = new StringReader(src))
			{
				versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
			}

			var sourceCorpus = new DictionaryTextCorpus(
				new MemoryText("MAT", new[]
				{
					TextRow(new VerseRef("MAT 1:1", ScrVers.Original), "source chapter one, verse one ."),
					TextRow(new VerseRef("MAT 1:2", ScrVers.Original), "source chapter one, verse two ."),
					TextRow(new VerseRef("MAT 1:3", ScrVers.Original), "source chapter one, verse three .")
				}));
			var targetCorpus = new DictionaryTextCorpus(
				new MemoryText("MAT", new[]
				{
					TextRow(new VerseRef("MAT 1:1", versification), "target chapter one, verse one ."),
					TextRow(new VerseRef("MAT 1:2", versification),
						"target chapter one, verse two . target chapter one, verse three .", isInRange: true,
						isRangeStart: true),
					TextRow(new VerseRef("MAT 1:3", versification), isInRange: true),
					TextRow(new VerseRef("MAT 1:4", versification), "target chapter one, verse four .")
				}));

			var parallelCorpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
			ParallelTextRow[] rows = parallelCorpus.ToArray();
			Assert.Equal(3, rows.Length);
			Assert.Equal(Refs(new VerseRef("MAT 1:2", ScrVers.Original)), rows[1].SourceRefs.Cast<VerseRef>());
			Assert.Equal(Refs(new VerseRef("MAT 1:2", versification), new VerseRef("MAT 1:3", versification)), rows[1].TargetRefs.Cast<VerseRef>());
			Assert.Equal("source chapter one, verse two .".Split(), rows[1].SourceSegment);
			Assert.Equal("target chapter one, verse two . target chapter one, verse three .".Split(), rows[1].TargetSegment);
		}

		private static TextRow TextRow(int key, string text = "", bool isSentenceStart = true,
			bool isInRange = false, bool isRangeStart = false)
		{
			return new TextRow(new RowRef(key))
			{
				Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(),
				IsSentenceStart = isSentenceStart,
				IsInRange = isInRange,
				IsRangeStart = isRangeStart,
				IsEmpty = text.Length == 0
			};
		}

		private static TextRow TextRow(VerseRef vref, string text = "", bool isSentenceStart = true,
			bool isInRange = false, bool isRangeStart = false)
		{
			return new TextRow(vref)
			{
				Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(),
				IsSentenceStart = isSentenceStart,
				IsInRange = isInRange,
				IsRangeStart = isRangeStart,
				IsEmpty = text.Length == 0
			};
		}

		private static IEnumerable<RowRef> Refs(params int[] keys)
		{
			return keys.Select(key => new RowRef(key));
		}

		private static IEnumerable<VerseRef> Refs(params VerseRef[] verseRefs)
		{
			return verseRefs;
		}

		private static AlignmentRow AlignmentRow(int key, params AlignedWordPair[] pairs)
		{
			return new AlignmentRow(new RowRef(key))
			{
				AlignedWordPairs = new HashSet<AlignedWordPair>(pairs)
			};
		}
	}
}