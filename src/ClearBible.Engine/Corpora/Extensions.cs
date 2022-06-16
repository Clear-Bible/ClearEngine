using SIL.Machine.Corpora;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public static class Extensions
    {
		public static EngineParallelTextCorpus EngineAlignRows(this ITextCorpus sourceCorpus, ITextCorpus targetCorpus,
			List<EngineVerseMapping>? sourceTargetParallelVersesList = null, IAlignmentCorpus? alignmentCorpus = null, bool allSourceRows = false, bool allTargetRows = false,
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
				return _corpus.GetRows().Where(_predicate);
			}

            IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}
