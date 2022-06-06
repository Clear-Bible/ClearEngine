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
		public void GetCorpus_FromExternal()
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
