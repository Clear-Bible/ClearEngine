using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System.Collections.Generic;
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
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestHelpers.UsfmTestProjectPath)
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
                var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestHelpers.UsfmTestProjectPath)
                    .Tokenize<LatinWordTokenizer>()
                    .Transform<IntoTokensTextRowProcessor>()
                    .Transform<SetTrainingBySurfaceTokensTextRowProcessor>();

                // now get the first 5 verses
                foreach (var tokensTextRow in corpus.Cast<TokensTextRow>().Take(5))
                {
                    TestHelpers.WriteTokensTextRow(output_, tokensTextRow, new EngineStringDetokenizer(new LatinWordDetokenizer()));
                }
                Assert.NotEmpty(corpus);
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }

        [Fact]
        public void Corpus_CompositeTokens()
        {
            var corpus = new DictionaryTextCorpus(
                new MemoryText("text1", new[]
                {
                    TestHelpers.CreateTextRow(new VerseRef(1,1,1), "Source segment Jacob 1", isSentenceStart: true),
                    TestHelpers.CreateTextRow(new VerseRef(1,1,2), "source segment Ruth 2.", isSentenceStart: false),
                    TestHelpers.CreateTextRow(new VerseRef(1,1,3), "Source segment Aaron 3.", isSentenceStart: true)
                }))
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>()
                .Transform<SetTrainingBySurfaceTokensTextRowProcessor>()
                .ToList(); //so it only tokenizes and transforms once.

            output_.WriteLine("Texts without composite:");
            foreach (var tokensTextRow in corpus.Cast<TokensTextRow>())
            {
                TestHelpers.WriteTokensTextRow(output_, tokensTextRow, new EngineStringDetokenizer(new LatinWordDetokenizer()));
            }

            //build new tokens list for first verse that includes a composite token
            var tokens = corpus
                .Select(tr => (TokensTextRow)tr)
                .First()
                .Tokens;

            var tokenIds = tokens
                .Select(t => t.TokenId)
                .ToList();

            var tokensWithComposite = new List<Token>()
            {
                new CompositeToken(new List<Token>() { tokens[0], tokens[1], tokens[3] }),
                tokens[2]
            };

            //set tokens of first verse with new tokens list with composite token
            corpus
                .Select(tr => (TokensTextRow)tr)
                .First()
                .Tokens = tokensWithComposite;

            output_.WriteLine("Texts with composite:");
            foreach (var tokensTextRow in corpus.Cast<TokensTextRow>())
            {
                TestHelpers.WriteTokensTextRow(output_, tokensTextRow, new EngineStringDetokenizer(new LatinWordDetokenizer()));
            }
        }
    }
}
