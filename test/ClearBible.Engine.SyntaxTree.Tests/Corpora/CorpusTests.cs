
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;
using ClearBible.Engine.SyntaxTree.Tokenization;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ClearBible.Engine.SyntaxTree.Tests.Corpora
{
    public class CorpusTests
    {
        protected readonly ITestOutputHelper output_;
        public CorpusTests(ITestOutputHelper output)
        {
            output_ = output;
        }

        [Fact]
        [Trait("Category", "Example")]
        public void Corpus_SytaxTrees_Read_OT()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                // now get the first 5 verses
                foreach (var tokensTextRow in sourceCorpus["GEN"].GetRows().Cast<TokensTextRow>().Take(5))
                {
                    output_.WriteLine(tokensTextRow.Ref.ToString());
                    output_.WriteLine($"Segments spaced    : {string.Join(" ", tokensTextRow.Segment)}");
                    output_.WriteLine($"TrainingText spaced: {string.Join(" ", tokensTextRow.Tokens.Select(t => t.TrainingText))}");
                    output_.WriteLine($"SurfaceText spaced : {string.Join(" ", tokensTextRow.Tokens.Select(t => t.SurfaceText))}");
                    output_.WriteLine($"Detokenized surfaceText: {new SyntaxTreeWordDetokenizer().Detokenize(tokensTextRow.Tokens)}");
                    output_.WriteLine("");
                }
                Assert.NotEmpty(sourceCorpus);
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }

        [Fact]
        [Trait("Category", "Example")]
        public void Corpus_SytaxTrees_Read_NT()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                // now get the first 5 verses
                foreach (var tokensTextRow in sourceCorpus["MAT"].GetRows().Cast<TokensTextRow>().Take(5))
                {
                    output_.WriteLine(tokensTextRow.Ref.ToString());
                    output_.WriteLine($"Segments spaced    : {string.Join(" ", tokensTextRow.Segment)}");
                    output_.WriteLine($"TrainingText spaced: {string.Join(" ", tokensTextRow.Tokens.Select(t => t.TrainingText))}");
                    output_.WriteLine($"SurfaceText spaced : {string.Join(" ", tokensTextRow.Tokens.Select(t => t.SurfaceText))}");
                    output_.WriteLine($"Detokenized surfaceText: {(new SyntaxTreeWordDetokenizer()).Detokenize(tokensTextRow.Tokens)}");
                    output_.WriteLine("");
                }
                Assert.NotEmpty(sourceCorpus);
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }


        [Fact]
        public void Corpus__SyntaxTrees_TokensSurfaceTrainingTextDifferent()
        {
            var syntaxTree = new SyntaxTrees();
            var corpus = new SyntaxTreeFileTextCorpus(syntaxTree).Take(5);
;
            Assert.NotEmpty(corpus);
            Assert.All(corpus, c => Assert.IsType<TokensTextRow>(c));
            var corpusList = corpus.ToList();

            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[0].SurfaceText);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText);
            Assert.NotEmpty(corpusList[0].Segment[0]);
            Assert.NotEqual(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText, ((TokensTextRow)corpusList[0]).Tokens[0].SurfaceText);
            Assert.Equal(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText, corpusList[0].Segment[0]);
        }
    }
}
