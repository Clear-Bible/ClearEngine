using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;



namespace ClearBible.Engine.Tests.Corpora
{
    public class CorpusTests
    {
		protected readonly ITestOutputHelper output_;
		public CorpusTests(ITestOutputHelper output)
		{
			output_ = output;
		}

        [Fact]
        public void Corpus__ImportFromUsfm_TokensSurfaceTrainingTextDifferent()
        {

            //Import
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestDataHelpers.UsfmTestProjectPath)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>()
                .Transform<SetTrainingBySurfaceTokensTextRowProcessor>();

            Assert.NotEmpty(corpus);
            Assert.All(corpus, c => Assert.IsType<TokensTextRow>(c));
            var corpusList = corpus.ToList();

            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[0].SurfaceText);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText);
            Assert.NotEqual(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText, ((TokensTextRow)corpusList[0]).Tokens[0].SurfaceText);
        }

        [Fact]
        [Trait("Category", "Example")]
        public void Corpus_ImportFromUsfm__Read()
        {
            try
            {
                var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestDataHelpers.UsfmTestProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>()
                    .Transform<SetTrainingBySurfaceTokensTextRowProcessor>();

                // now get the first 5 verses
                foreach (var tokensTextRow in corpus.Cast<TokensTextRow>().Take(5))
                {
                    output_.WriteLine(tokensTextRow.Ref.ToString());
                    output_.WriteLine($"Segments spaced        : {string.Join(" ", tokensTextRow.Segment)}");
                    output_.WriteLine($"TrainingText spaced    : {string.Join(" ", tokensTextRow.Tokens.Select(t => t.TrainingText))}");
                    output_.WriteLine($"SurfaceText spaced     : {string.Join(" ", tokensTextRow.Tokens.Select(t => t.SurfaceText))}");
                    output_.WriteLine($"Detokenized surfaceText: {new LatinWordDetokenizer().Detokenize(tokensTextRow.Tokens.Select(t => t.SurfaceText).ToList())}");
                    output_.WriteLine("");
                }
                Assert.NotEmpty(corpus);
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }
    }
}
