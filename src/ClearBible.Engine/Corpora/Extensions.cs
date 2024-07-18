using ClearBible.Engine.Exceptions;
using Newtonsoft.Json.Linq;
using SIL.Machine.Corpora;
using SIL.Scripture;
using System.Collections;

namespace ClearBible.Engine.Corpora
{
    public static class Extensions
    {
		public static EngineParallelTextCorpus EngineAlignRows(
			this ITextCorpus sourceCorpus, 
			ITextCorpus targetCorpus,
            SourceTextIdToVerseMappings? sourceTargetParallelVersesList = null, 
			IAlignmentCorpus? alignmentCorpus = null, 
			bool allSourceRows = false, 
			bool allTargetRows = false,
			IComparer<object>? rowRefComparer = null
		)
		{
			return new EngineParallelTextCorpus(sourceCorpus, targetCorpus, sourceTargetParallelVersesList, alignmentCorpus, rowRefComparer)
			{
				AllSourceRows = allSourceRows,
				AllTargetRows = allTargetRows
			};
		}
        public static bool CanPackWith(this CompositeToken compositeToken, CompositeToken other)
        {
            return other.TokenId.Equals(compositeToken.TokenId)
                && (other.ExtendedProperties?.Equals(compositeToken.ExtendedProperties) ?? compositeToken.ExtendedProperties == null ? true : false)
                && other.Tokens.Concat(other.OtherTokens)
                    .All(compositeToken.Tokens.Concat(compositeToken.OtherTokens).Contains)
                && compositeToken.Tokens.Concat(compositeToken.OtherTokens)
                    .All(other.Tokens.Concat(other.OtherTokens).Contains);
        }
        public static IEnumerable<Token> PackComposites(this IEnumerable<Token> tokens)
		{
			var compositeTokensGroupedByTokenIds = tokens
				.Where(token => token is CompositeToken)
				.GroupBy(compositeToken => compositeToken.TokenId)
				.Select(g => g
					.Select(t => t as CompositeToken));

			var packedCompositeTokens = new List<Token>();
			foreach (var compositeTokensGroupedByTokenId in compositeTokensGroupedByTokenIds)
			{
				if (compositeTokensGroupedByTokenId.Count() == 0)
					continue;

				CompositeToken? packedCompositeToken = null;
				foreach (var compositeToken in compositeTokensGroupedByTokenId)
				{
					if (compositeToken == null) 
						continue;

					if (packedCompositeToken == null)
					{
						packedCompositeToken = compositeToken;
						continue;
					}

#if DEBUG
					//validate that composites have the same value
					if (!compositeToken.CanPackWith(packedCompositeToken))
						throw new InvalidDataEngineException(message: $"Composite token {compositeToken.TokenId} cannot pack with {packedCompositeToken.TokenId}: ExtendedPropeties and/or combination of tokens and othertokens don't match.");
#endif
					packedCompositeToken.Tokens = packedCompositeToken.Tokens
						.Concat(compositeToken.Tokens);

#if DEBUG
					//validate compositetoken didn't have any token that was already in packedCompositeToken
					if (packedCompositeToken.Tokens.GroupBy(t => t.TokenId).Any(g => g.Count() > 1))
                        throw new InvalidDataEngineException(message: $"Packed composite token {compositeToken.TokenId} Tokens contains one or more Tokens with the same TokenId.");
#endif
					packedCompositeToken.OtherTokens = packedCompositeToken.OtherTokens
						.Concat(compositeToken.OtherTokens)
						.Except(packedCompositeToken.Tokens)
						.Distinct();
                }
				if (packedCompositeToken != null)
					packedCompositeTokens.Add(packedCompositeToken);
			}

			var packedTokens = tokens
                .Where(token => token is not CompositeToken)
                .Concat(packedCompositeTokens);

#if DEBUG
			//validate that there are no duplicate tokens
			if (packedTokens
                .SelectMany(t => (t is CompositeToken) ? ((CompositeToken)t).Tokens.Concat(((CompositeToken)t).OtherTokens) : new List<Token>() { t })
				.GroupBy(t => t.TokenId)
				.Any(g => g.Count() > 1))
            {
                throw new InvalidDataEngineException(name: "Tokens", message: "set of all Tokens and CompositeToken children Tokens and OtherTokens has one or more duplicate Token.TokenIds");
            }
#endif
			return packedTokens;

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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="textCorpus"></param>
        /// <param name="verseRef"></param>
        /// <param name="numberOfVersesInChapterBefore"></param>
        /// <param name="numberOfVersesInChapterAfter"></param>
        /// <param name="versification">if null, treat verseRef as in textCorpusVersification if set, or scriptureTextCorpus.Versification,
        /// else treat verseRef in versification and change to textCorpusVersification if set, or scriptureTextCorpus.Versification, before finding textrows in textCorpus</param>
		/// <param name="textCorpusVersification">if set use this value, otherwise assume textCorpus is a ScriptureTextCorpus and obtain versification from it.</param>
        /// <returns></returns>
        public static (IEnumerable<TextRow> textRows, int indexOfVerse) GetByVerseRange(this ITextCorpus textCorpus,
            VerseRef verseRef,
            ushort numberOfVersesInChapterBefore,
            ushort numberOfVersesInChapterAfter,
            ScrVers? versification = null,
			ScrVers? textCorpusVersification = null)
		{
            if (textCorpus is not ScriptureTextCorpus && textCorpusVersification == null)
            {
                throw new InvalidStateEngineException(
                    name: "sourceCorpusVersification",
                    value: "null",
                    message: $"textCorpus is a {textCorpus.GetType().Name} which is not a ScriptureTextCorpus so textCorpusVersification must be set but isn't.");
            }

			ScrVers txtCorpusVersification;
			if (textCorpusVersification != null)
				txtCorpusVersification = textCorpusVersification;
			else
				txtCorpusVersification = ((ScriptureTextCorpus)textCorpus).Versification;

            if (versification != null)
				verseRef.Versification = versification;
			else
				verseRef.Versification = txtCorpusVersification;

            var firstVerseNumber = verseRef.VerseNum - numberOfVersesInChapterBefore;
            var count = numberOfVersesInChapterAfter + numberOfVersesInChapterBefore + 1;
            if (firstVerseNumber <= 0)
            {
                count = count + firstVerseNumber - 1;
                firstVerseNumber = 1;
            }

            var verseRefs = Enumerable.Range(firstVerseNumber, count)
				.SelectMany(vn =>
				{
					var vref = new VerseRef(verseRef)
					{
						VerseNum = vn
					};

					if (versification != null) 
					{
						vref.ChangeVersification(txtCorpusVersification);
                        return vref.AllVerses();
                    }
					return new List<VerseRef>() { vref };
				})
				.ToList();

			var books = verseRefs
				.Select(vr => vr.Book)
				.Distinct()
				.ToList();

            var bookTextRows = textCorpus.GetRows(books);
            
			var textRows = new List<TextRow>();

            foreach (var vRef in verseRefs)
            {
                var textRowForVerse = bookTextRows
                    //.Where(textRow => textRow.Ref.Equals(vRef))
                    .FirstOrDefault(textRow => textRow.Ref.Equals(vRef));

                if (textRowForVerse == null)
                {
                    continue;
                }
                else
                {
                    textRows.Add(textRowForVerse);
                }
            }

            verseRef.ChangeVersification(txtCorpusVersification);

            return (textRows, textRows.FindIndex(tr => tr.Ref.Equals(verseRef)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="engineParallelTextCorpus">Requires that engineParallelTextCorpus.SourceCorpusVersification is not null. If engineParallelTextCorpus is not
		/// a ScriptureTextCorpus this property must be manually set.</param>
        /// <param name="verseRef"></param>
        /// <param name="numberOfVersesInChapterBefore"></param>
        /// <param name="numberOfVersesInChapterAfter"></param>
        /// <param name="versification">if null, treat verseRef as in engineParallelCorpus.SourceCorpusVersification,
        /// else treat verseRef in versification and change to engineParallelCorpus.SourceCorpusVersification before finding 
		/// paralleltextrows in engineParallelCorpus</param>
        /// <returns></returns>
        /// <exception cref="InvalidStateEngineException"></exception>
        public static (IEnumerable<ParallelTextRow> parallelTextRows, int indexOfVerse) GetByVerseRange(this EngineParallelTextCorpus engineParallelTextCorpus, 
			VerseRef verseRef, 
			ushort numberOfVersesInChapterBefore, 
			ushort numberOfVersesInChapterAfter,
            ScrVers? versification = null)
		{
            if (engineParallelTextCorpus.SourceCorpusVersification == null)
            {
                throw new InvalidStateEngineException(
                    name: "sourceCorpusVersification",
                    value: "null",
                    message: $"engineParallelTextCorpus.SourceCorpusVersification property is not set because SourceCorpus is {engineParallelTextCorpus.SourceCorpus.GetType().Name} which is not type ScriptureTextCorpus. Please set engineParallelTextCorpus.SourceCorpusVersification property manually before calling this method.");
            }

            if (versification != null)
                verseRef.Versification = versification;
            else
                verseRef.Versification = engineParallelTextCorpus.SourceCorpusVersification;

			var firstVerseNumber = verseRef.VerseNum - numberOfVersesInChapterBefore;
			var count = numberOfVersesInChapterAfter + numberOfVersesInChapterBefore + 1;
			if (firstVerseNumber <= 0) 
			{
				count = count + firstVerseNumber - 1;
				firstVerseNumber = 1;
			}

            var verseRefs = Enumerable.Range(firstVerseNumber, count)
				.SelectMany(vn =>
				{
					var vref = new VerseRef(verseRef)
					{
						VerseNum = vn
					};

					if (versification != null)
					{
						vref.ChangeVersification(engineParallelTextCorpus.SourceCorpusVersification);
						return vref.AllVerses();
					}
					return new List<VerseRef>() { vref };
				})
				.ToList();

			engineParallelTextCorpus.LimitToSourceBooks = verseRefs
				.Select(vr => vr.Book)
				.Distinct()
				.ToList();

            var parallelTextRows = new List<ParallelTextRow>();

            foreach (var sVerseRef in verseRefs)
			{
				var engineParallelTextRowForVerse = engineParallelTextCorpus
					//.Where(parallelTextRow => parallelTextRow.SourceRefs.Contains(sVerseRef))
					.FirstOrDefault(parallelTextRow => parallelTextRow.SourceRefs.Contains(sVerseRef));

				if (engineParallelTextRowForVerse == null)
				{
					continue;
				}
				else
				{ 
					parallelTextRows.Add(engineParallelTextRowForVerse);
				}
            }

			engineParallelTextCorpus.LimitToSourceBooks = null;

            verseRef.ChangeVersification(engineParallelTextCorpus.SourceCorpusVersification);

            return (parallelTextRows, parallelTextRows
				.FindIndex(pr => pr.SourceRefs
					.Contains(verseRef)));
		}
    }
}
