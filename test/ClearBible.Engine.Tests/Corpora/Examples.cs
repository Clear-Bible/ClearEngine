using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using ClearBible.Alignment.DataServices.Corpora;
using MediatR;
using ClearBible.Engine.Tests.Corpora.Handlers;

namespace ClearBible.Engine.Tests.Corpora
{
    public class Examples
    {
		protected readonly ITestOutputHelper output_;
		public Examples(ITestOutputHelper output)
		{
			output_ = output;
		}

		[Fact]
		public async void CorpusImportAndSaveToDb()
		{
			IMediator mediator = new MediatorMock();

			//Import
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestDataHelpers.UsfmTestProjectPath)
				.Tokenize<LatinWordTokenizer>()
				.Transform<IntoTokensTextRowProcessor>();

			var dbCorpus = await corpus.Create(mediator, true, "NameX", "LanguageX", "LanguageType", ".Tokenize<LatinWordTokenizer>().Transform<IntoTokensTextRowProcessor>()");

			foreach (var tokensTextRow in dbCorpus.Take(5).Cast<TokensTextRow>())
			{
				//display verse info
				var verseRefStr = tokensTextRow.Ref.ToString();
				output_.WriteLine(verseRefStr);

				//display legacy segment
				var segmentText = string.Join(" ", tokensTextRow.Segment);
				output_.WriteLine($"segmentText: {segmentText}");

				//display tokenIds
				var tokenIds = string.Join(" ", tokensTextRow.Tokens.Select(t => t.TokenId.ToString()));
				output_.WriteLine($"tokenIds: {tokenIds}");

				//display tokens tokenized
				var tokensText = string.Join(" ", tokensTextRow.Tokens.Select(t => t.Text));
				output_.WriteLine($"tokensText: {tokensText}");

				//display tokens detokenized
				var detokenizer = new LatinWordDetokenizer();
				var tokensTextDetokenized = detokenizer.Detokenize(tokensTextRow.Tokens.Select(t => t.Text).ToList());
				output_.WriteLine($"tokensTextDetokenized: {tokensTextDetokenized}");
			}
		}

		[Fact]
		public async void CorpusGetFromDb()
		{
			IMediator mediator = new MediatorMock();

			var dbCorpus = await TokenizedTextCorpus.Get(mediator, new TokenizedCorpusId(new Guid()));

			foreach (var tokensTextRow in dbCorpus.Take(5).Cast<TokensTextRow>())
			{
				//display verse info
				var verseRefStr = tokensTextRow.Ref.ToString();
				output_.WriteLine(verseRefStr);

				//display legacy segment
				var segmentText = string.Join(" ", tokensTextRow.Segment);
				output_.WriteLine($"segmentText: {segmentText}");

				//display tokenIds
				var tokenIds = string.Join(" ", tokensTextRow.Tokens.Select(t => t.TokenId.ToString()));
				output_.WriteLine($"tokenIds: {tokenIds}");

				//display tokens tokenized
				var tokensText = string.Join(" ", tokensTextRow.Tokens.Select(t => t.Text));
				output_.WriteLine($"tokensText: {tokensText}");

				//display tokens detokenized
				var detokenizer = new LatinWordDetokenizer();
				var tokensTextDetokenized = detokenizer.Detokenize(tokensTextRow.Tokens.Select(t => t.Text).ToList());
				output_.WriteLine($"tokensTextDetokenized: {tokensTextDetokenized}");
			}
		}

		[Fact]
		public void PutCorpus()
        {

        }

		[Fact]
		[Trait("Category", "Example")]
		public void GetCorpusByBook()
		{
			var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestDataHelpers.UsfmTestProjectPath)
				.Tokenize<LatinWordTokenizer>()
				.Transform<IntoTokensTextRowProcessor>();

			foreach (var tokensTextRow in corpus.Take(5).Cast<TokensTextRow>())
			{
				//display verse info
				var verseRefStr = tokensTextRow.Ref.ToString();
				output_.WriteLine(verseRefStr);

				//display legacy segment
				var segmentText = string.Join(" ", tokensTextRow.Segment);
				output_.WriteLine($"segmentText: {segmentText}");

				//display tokenIds
				var tokenIds = string.Join(" ", tokensTextRow.Tokens.Select(t => t.TokenId.ToString()));
				output_.WriteLine($"tokenIds: {tokenIds}");

				//display tokens tokenized
				var tokensText = string.Join(" ", tokensTextRow.Tokens.Select(t => t.Text));
				output_.WriteLine($"tokensText: {tokensText}");

				//display tokens detokenized
				var detokenizer = new LatinWordDetokenizer();
				var tokensTextDetokenized = detokenizer.Detokenize(tokensTextRow.Tokens.Select(t => t.Text).ToList());
				output_.WriteLine($"tokensTextDetokenized: {tokensTextDetokenized}");
			}
		}
		[Fact]
		public void GetParallelCorpus()
		{
		}

		[Fact]
		public void GetParallelCorpusByBook()
		{
		}
		}
}
