using ClearBible.Engine.Exceptions;
using SIL.Machine.Corpora;
using SIL.Scripture;
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
					.Select(v => v.Book)
					.Distinct()
					.Skip(1)
					.Any()
						||
					verseMapping.TargetVerses //not all of the targetVerses for a given verseMapping are for the same book
                    .Select(v => v.Book)
                    .Distinct()
					.Skip(1)
					.Any()
						||
					! verseMapping.SourceVerses // the sourceVerses book and targetVerses book are not the same
                    .Select(v => v.Book)
                    .Distinct()
					.First().Equals(verseMapping.TargetVerses
					.Select(v => v.Book)
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

		/*
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
		*/

		public static IEnumerable<string> GetSurfaceTexts(this IEnumerable<Token>? tokens)
        {
			return tokens?
				.GetPositionalSortedBaseTokens()
				.Select(t => t.SurfaceText)
				?? new List<string>();
		}

		public static IEnumerable<Token> GetPositionalSortedBaseTokens(this IEnumerable<Token> tokens)
		{
			return tokens
				.SelectMany(t =>
					(t is CompositeToken) ?
						((CompositeToken)t).Tokens
					:
						new List<Token>() { t })
				.OrderBy(t => t.Position);
        }


		public static (IEnumerable<TextRow> textRows, int indexOfVerse) GetByVerseRange(this ScriptureTextCorpus scriptureTextCorpus,
            VerseRef verseRef,
            ushort numberOfVersesInChapterBefore,
            ushort numberOfVersesInChapterAfter,
            ScrVers? versification = null)
		{
			versification = versification
                ?? scriptureTextCorpus.Versification 
				?? throw new InvalidStateEngineException(name: "versification", value: "null", message: "both parameter null and textRows isn't a ScriptureTextCorpus");


            var textRows = new List<TextRow>();

            var bookTextRows = scriptureTextCorpus[verseRef.Book].GetRows();

            var firstVerseNumber = verseRef.VerseNum - numberOfVersesInChapterBefore;
            var count = numberOfVersesInChapterAfter + numberOfVersesInChapterBefore + 1;
            //var indexOfVerse = (numberOfVersesInChapterBefore + numberOfVersesInChapterAfter) / 2;
            if (firstVerseNumber <= 0)
            {
                count = count + firstVerseNumber - 1;
                //indexOfVerse = indexOfVerse + firstVerse - 1;
                firstVerseNumber = 1;
            }

            foreach (var verseNumber in Enumerable.Range(firstVerseNumber, count))
            {
                var currentVerseRef = verseRef;
                currentVerseRef.VerseNum = verseNumber;
                currentVerseRef.ChangeVersification(versification);

                var textRowForVerse = bookTextRows
                    .Where(textRow => textRow.Ref.Equals(currentVerseRef))
                    .FirstOrDefault();

                if (textRowForVerse == null)
                {
                    break;
                }
                else
                {
                    textRows.Add(textRowForVerse);
                }
            }

            verseRef.ChangeVersification(versification);
            return (textRows, textRows.FindIndex(tr => tr.Ref.Equals(verseRef)));
        }


        public static (IEnumerable<ParallelTextRow> parallelTextRows, int indexOfVerse) GetByVerseRange(this EngineParallelTextCorpus engineParallelTextCorpus, 
			VerseRef verseRef, 
			ushort numberOfVersesInChapterBefore, 
			ushort numberOfVersesInChapterAfter, 
			ScrVers? sourceCorpusVersification = null)
		{
            sourceCorpusVersification = sourceCorpusVersification 
				?? engineParallelTextCorpus.SourceCorpusVersification 
				?? throw new InvalidStateEngineException(name: "sourceCorpusVersification", value: "null", message: "both parameter null and engineParallelTextCorpus.SourceCorpusVersification is null");

            var priorLimitToBook = engineParallelTextCorpus.GetLimitToBook();

			var parallelTextRows = new List<ParallelTextRow>();

			engineParallelTextCorpus.SetLimitToBook(verseRef.Book);

			var firstVerseNumber = verseRef.VerseNum - numberOfVersesInChapterBefore;
			var count = numberOfVersesInChapterAfter + numberOfVersesInChapterBefore + 1;
			//var indexOfVerse = (numberOfVersesInChapterBefore + numberOfVersesInChapterAfter) / 2;
			if (firstVerseNumber <= 0) 
			{
				count = count + firstVerseNumber - 1;
				//indexOfVerse = indexOfVerse + firstVerse - 1;
				firstVerseNumber = 1;
			}

			foreach (var verseNumber in Enumerable.Range(firstVerseNumber, count))
			{
                var currentVerseRef = verseRef; 
				currentVerseRef.VerseNum = verseNumber;
				currentVerseRef.ChangeVersification(sourceCorpusVersification);

				var engineParallelTextRowForVerse = engineParallelTextCorpus
					.Where(parallelTextRow => parallelTextRow.SourceRefs.Contains(currentVerseRef))
					.FirstOrDefault();

				if (engineParallelTextRowForVerse == null)
				{
					break;
				}
				else
				{ 
					if (!parallelTextRows.Contains(engineParallelTextRowForVerse))
						parallelTextRows.Add(engineParallelTextRowForVerse);
				}
            }

			engineParallelTextCorpus.SetLimitToBook(priorLimitToBook);
			verseRef.ChangeVersification(sourceCorpusVersification);
			return (parallelTextRows, parallelTextRows.FindIndex(pr => pr.SourceRefs.Contains(verseRef)));
		}
    }
}
