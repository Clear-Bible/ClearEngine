using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

using SIL.Machine.Corpora;
using SIL.Scripture;
using Xunit.Abstractions;
using ClearBible.Engine.Corpora;

namespace ClearBible.Engine.Tests.Corpora
{
	public class ParallelCorpusTests
    {
		protected readonly ITestOutputHelper output_;

		public ParallelCorpusTests(ITestOutputHelper output)
		{
			output_ = output;
		}

		[Fact]
		public void ParallelCorpus__GetGetRows_SameVerseRefOneToMany()
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


        [Fact]
        public void ParallelCorpus__LimitByBook_FindByVerse()
        {
			/*
			 *  Should result in the following mapping for MAT 1:
			 *  
			 *  Original       Source         Target
			 *     1             2              3
			 *     2             3              2
			 *     3             1              1
			 * 
			 */
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
            string source = "&MAT 1:2 = MAT 1:1\nMAT 1:3 = MAT 1:2\nMAT 1:1 = MAT 1:3\n";
            ScrVers versificationSource;
            using (var reader = new StringReader(source))
            {
                versificationSource = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            }

            Versification.Table.Implementation.RemoveAllUnknownVersifications();
            string target = "&MAT 1:3 = MAT 1:1\nMAT 1:1 = MAT 1:3\nMAT 1:2 = MAT 1:2\n";
            ScrVers versificationTarget;
            using (var reader = new StringReader(target))
            {
                versificationTarget = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            }


            var sourceCorpus = new DictionaryTextCorpus(
                new MemoryText("MAT", new[]
                {
                    TextRow(new VerseRef("MAT 1:1", versificationSource), "source MAT chapter one, verse one ."),
                    TextRow(new VerseRef("MAT 1:2", versificationSource), "source MAT chapter one, verse two ."),
                    TextRow(new VerseRef("MAT 1:3", versificationSource), "source MAT chapter one, verse three .")
                }),
                new MemoryText("MRK", new[]
                {
                    TextRow(new VerseRef("MRK 1:1", versificationSource), "source MRK chapter one, verse one ."),
                    TextRow(new VerseRef("MRK 1:2", versificationSource), "source MRK chapter one, verse two ."),
                    TextRow(new VerseRef("MRK 1:3", versificationSource), "source MRK chapter one, verse three .")
                }));
            var targetCorpus = new DictionaryTextCorpus(
                new MemoryText("MAT", new[]
                {
                    TextRow(new VerseRef("MAT 1:1", versificationTarget), "target MAT chapter one, verse one ."),
                    TextRow(new VerseRef("MAT 1:2", versificationTarget), "target MAT chapter one, verse two ."),
                    TextRow(new VerseRef("MAT 1:3", versificationTarget), "target MAT chapter one, verse three .")
                }),
                new MemoryText("MRK", new[]
                {
                    TextRow(new VerseRef("MRK 1:1", versificationTarget), "target MRK chapter one, verse one ."),
                    TextRow(new VerseRef("MRK 1:2", versificationTarget), "target MRK chapter one, verse two ."),
                    TextRow(new VerseRef("MRK 1:3", versificationTarget), "target MRK chapter one, verse three .")
                }));
            var engineParallelTextCorpus = sourceCorpus.EngineAlignRows(targetCorpus, new());

            Assert.Equal(6, engineParallelTextCorpus.Count());
            Assert.Equal(3, engineParallelTextCorpus.SetLimitToBook("MRK").Count());
            Assert.Empty(engineParallelTextCorpus
                .Where(parallelTextRow => parallelTextRow.SourceRefs
                      .Select(r => (VerseRef)r)
                      .Where(v => !v.Book.Equals("MRK"))
                      .Count() > 0));
            Assert.Equal(3, engineParallelTextCorpus.SetLimitToBook("MAT").Count());
            Assert.Empty(engineParallelTextCorpus
                .Where(parallelTextRow => parallelTextRow.SourceRefs
                      .Select(r => (VerseRef)r)
                      .Where(v => !v.Book.Equals("MAT"))
                      .Count() > 0));
            Assert.Equal(6, engineParallelTextCorpus.SetLimitToBook().Count());

            // Original MAT 1:1 maps to a parallelTextRow where source is MAT 1:2 and target is MAT 1:3
            var originalMAT1 = new VerseRef("MAT 1:1", ScrVers.Original);
            originalMAT1.ChangeVersification(versificationSource); //NOTE: this is obtainable from engineParallelTextCorpus.SourceCorpusVersification when source corpus is a ScriptureTextCorpus
            var parallelTextRowOriginalMAT1 = engineParallelTextCorpus.SetLimitToBook("MAT")
                .Where(parallelTextRow => parallelTextRow.SourceRefs.Contains(originalMAT1))
                .FirstOrDefault() ?? throw new Exception("Doesn't contain verse");
            Assert.True(parallelTextRowOriginalMAT1.SourceRefs
                .Cast<VerseRef>()
                .Where(vr => vr.Equals(new VerseRef("MAT 1:2", versificationSource)))
                .Count() > 0);
            Assert.True(parallelTextRowOriginalMAT1.TargetRefs
                .Cast<VerseRef>()
                .Where(vr => vr.Equals(new VerseRef("MAT 1:3", versificationTarget)))
                .Count() > 0);

            engineParallelTextCorpus.SetLimitToBook();

            // Original MAT 1:2 maps to a parallelTextRow where source is MAT 1:3 and target is MAT 1:2
            var originalMAT2 = new VerseRef("MAT 1:2", ScrVers.Original);
            originalMAT2.ChangeVersification(versificationSource); //NOTE: this is obtainable from engineParallelTextCorpus.SourceCorpusVersification when source corpus is a ScriptureTextCorpus
            var parallelTextRowOriginalMAT2 = engineParallelTextCorpus.SetLimitToBook("MAT")
                .Where(parallelTextRow => parallelTextRow.SourceRefs.Contains(originalMAT2))
                .FirstOrDefault() ?? throw new Exception("Doesn't contain verse");
            Assert.True(parallelTextRowOriginalMAT2.SourceRefs
                .Cast<VerseRef>()
                .Where(vr => vr.Equals(new VerseRef("MAT 1:3", versificationSource)))
                .Count() > 0);
            Assert.True(parallelTextRowOriginalMAT2.TargetRefs
                .Cast<VerseRef>()
                .Where(vr => vr.Equals(new VerseRef("MAT 1:2", versificationTarget)))
                .Count() > 0);

            engineParallelTextCorpus.SetLimitToBook();

            // Original MAT 1:3 maps to a parallelTextRow where source is MAT 1:1 and target is MAT 1:1
            var originalMAT3 = new VerseRef("MAT 1:3", ScrVers.Original);
            originalMAT3.ChangeVersification(versificationSource); //NOTE: this is obtainable from engineParallelTextCorpus.SourceCorpusVersification when source corpus is a ScriptureTextCorpus
            var parallelTextRowOriginalMAT3 = engineParallelTextCorpus.SetLimitToBook("MAT")
                .Where(parallelTextRow => parallelTextRow.SourceRefs.Contains(originalMAT3))
                .FirstOrDefault() ?? throw new Exception("Doesn't contain verse");
            Assert.True(parallelTextRowOriginalMAT3.SourceRefs
                .Cast<VerseRef>()
                .Where(vr => vr.Equals(new VerseRef("MAT 1:1", versificationSource)))
                .Count() > 0);
            Assert.True(parallelTextRowOriginalMAT3.TargetRefs
                .Cast<VerseRef>()
                .Where(vr => vr.Equals(new VerseRef("MAT 1:1", versificationTarget)))
                .Count() > 0);

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