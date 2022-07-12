using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using System.Collections;

namespace ClearBible.Engine.Corpora
{
    public static class Extensions
    {
		public static List<VerseMapping> Validate(this List<VerseMapping> verseMappingList, IEnumerable<TextRow> sourceCorpus, IEnumerable<TextRow> targetCorpus)
		{
			foreach (var verseMapping in verseMappingList)
            {
				if (
					verseMapping.SourceVerses //not all of the sourceverses for a given verseMapping are for the same book
					.Distinct()
					.Skip(1)
					.Any()
						||
					verseMapping.TargetVerses //not all of the targetVerses for a given verseMapping are for the same book
					.Distinct()
					.Skip(1)
					.Any()
						||
					! verseMapping.SourceVerses // the sourceVerses book and targetVerses book are not the same
					.Distinct()
					.First().Equals(verseMapping.TargetVerses
					.Distinct()
					.First())
					)
                {
					throw new InvalidDataEngineException(
						name: "List<VerseMapping>", 
						value: "VerseMapping.SourceVerses andVerseMapping.TargetVerses not all for same book", 
						message: "all sourceVerses and targetverses for a given versemapping must be for the same book");
				}

			}
			return verseMappingList;
		}
		public static EngineParallelTextCorpus EngineAlignRows(this ITextCorpus sourceCorpus, ITextCorpus targetCorpus,
			List<VerseMapping>? sourceTargetParallelVersesList = null, IAlignmentCorpus? alignmentCorpus = null, bool allSourceRows = false, bool allTargetRows = false,
			IComparer<object>? rowRefComparer = null)
		{
			return new EngineParallelTextCorpus(sourceCorpus, targetCorpus, sourceTargetParallelVersesList, alignmentCorpus, rowRefComparer)
			{
				AllSourceRows = allSourceRows,
				AllTargetRows = allTargetRows
			};
		}
		public static ITextCorpus Transform<T>(this ITextCorpus corpus)
			where T : IRowProcessor<TextRow>, new()
		{
			var textRowProcessor = new T();
			return new TransformTextCorpus(corpus, textRowProcessor.Process);
		}

		private class TransformTextCorpus : ITextCorpus
		{
			private readonly ITextCorpus _corpus;
			private readonly Func<TextRow, TextRow> _transform;

			public TransformTextCorpus(ITextCorpus corpus, Func<TextRow, TextRow> transform)
			{
				_corpus = corpus;
				_transform = transform;
			}

			public IEnumerable<IText> Texts => _corpus.Texts;

			public IEnumerator<TextRow> GetEnumerator()
			{
				return GetRows().GetEnumerator();
			}

			public IEnumerable<TextRow> GetRows(IEnumerable<string>? textIds = null)
			{
				return _corpus.GetRows(textIds).Select(_transform);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public static ITextCorpus Filter(this ITextCorpus corpus, Func<TextRow, bool> predicate)
		{
			return new FilterTextCorpus(corpus, predicate);
		}

		public static ITextCorpus Filter(this ITextCorpus corpus, IRowFilter<TextRow> filter)
		{
			return corpus.Filter(filter.Process);
		}

		public static ITextCorpus Filter<T>(this ITextCorpus corpus)
			where T : IRowFilter<TextRow>, new()
		{
			var textRowFilter = new T();
			return new FilterTextCorpus(corpus, textRowFilter.Process);
		}
		private class FilterTextCorpus : ITextCorpus
		{
			private readonly ITextCorpus _corpus;
			private readonly Func<TextRow, bool> _predicate;

			public FilterTextCorpus(ITextCorpus corpus, Func<TextRow, bool> predicate)
			{
				_corpus = corpus;
				_predicate = predicate;
			}

			public IEnumerable<IText> Texts => _corpus.Texts;

			public IEnumerator<TextRow> GetEnumerator()
			{
				return GetRows().GetEnumerator();
			}

			public IEnumerable<TextRow> GetRows(IEnumerable<string>? textIds = null)
			{
				return _corpus.GetRows(textIds).Where(_predicate);
			}

            IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}
