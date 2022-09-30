
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;
using ClearBible.Engine.Tests.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Tokenization;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json;
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


        private IEnumerable<string> GetWordStrings(IEnumerable<List<Token>> tokenGroups)
        {
            return tokenGroups
                .Select(tg => tg
                    .Aggregate(string.Empty, (constructedString, token) => $"{constructedString}{token.SurfaceTextPrefix}{token.SurfaceText}{token.SurfaceTextSuffix}"));
        }
        private IEnumerable<List<Token>> GetTokensGroupedByWords(IEnumerable<Token> tokens)
        {
            List<Token> tokenWordGroup = new();

            foreach (var token in tokens
                .OrderBy(t => t.Position))
            {
                if (tokenWordGroup.LastOrDefault()?.TokenId.IsNextSubword(token.TokenId) ?? false)
                {
                    tokenWordGroup.Add(token);
                }
                else
                {
                    if (tokenWordGroup.Count() > 0)
                    {
                        yield return tokenWordGroup;
                    }
                    tokenWordGroup = new() { token };
                }
            }

            if (tokenWordGroup.Count() > 0)
            {
                yield return tokenWordGroup;
            }
        }


        [Fact]
        public void Corpus_SyntaxTrees_SyntaxTreeTokenPropertiesJson()
        {
            var syntaxTree = new SyntaxTrees();
            var corpus = new SyntaxTreeFileTextCorpus(syntaxTree);
            var sourceCorpusFirstVerse = corpus.First();
            var firstToken = ((TokensTextRow)sourceCorpusFirstVerse).Tokens.First();
            var firstTokenPropertiesJson = firstToken.PropertiesJson;
            Assert.NotNull(firstTokenPropertiesJson);
            Assert.IsType<SyntaxTreeToken>(firstToken);

            firstToken.PropertiesJson = "blah";
            //can't set because its a SyntaxTreeToken
            Assert.Equal(firstTokenPropertiesJson, firstToken.PropertiesJson);

            //Since it is a syntaxtree token it has English in it.
            JsonNode forecastNode = JsonNode.Parse(firstToken.PropertiesJson)!;
            var options = new JsonSerializerOptions { WriteIndented = true };
            output_.WriteLine(forecastNode!.ToJsonString(options));

            JsonNode englishNode = forecastNode!["English"]!;
            output_.WriteLine($"JSON={englishNode.ToJsonString()}");
            output_.WriteLine($"Type={englishNode.GetType()}");
        }
        [Fact]
        public void Corpus_SyntaxTrees_DetokenizeAndCompareAll()
        {
            var syntaxTree = new SyntaxTrees();
            var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

            var stringDetokenizer = new WhitespaceDetokenizer();
            var engineDetokenizer = new EngineStringDetokenizer(stringDetokenizer);

            foreach (var verseTokensTextRow in sourceCorpus.GetRows().Cast<TokensTextRow>())
            {
                var verseTokens = verseTokensTextRow.Tokens
                    .SelectMany(t =>
                    (t is CompositeToken) ?
                        ((CompositeToken)t).Tokens
                    :
                        new List<Token>() { t })
                    .OrderBy(t => t.Position);

                //group  verse's tokens by words
                var tokensGroupedByWords = GetTokensGroupedByWords(verseTokens);
                //put the words together
                var wordStrings = GetWordStrings(tokensGroupedByWords);
                //detokenize them into a string
                var versePutTogetherByWordStrings = stringDetokenizer.Detokenize(wordStrings) ?? wordStrings.Aggregate(string.Empty, (constructedString, str) => $"{constructedString}{str}");

                //Now use engine detokenizer to get the tokens with padding
                var verseTokensWithPadding = engineDetokenizer.Detokenize(verseTokens);
                //put the tokens together with padding
                var versePutTogetherByVerseTokensWithPadding = verseTokensWithPadding
                    .OrderBy(t => t.token.Position)
                    .Aggregate(string.Empty, (constructedString, tokenWithPadding) => $"{constructedString}{tokenWithPadding.paddingBefore}{tokenWithPadding.token}{tokenWithPadding.paddingAfter}");

                Assert.Equal(versePutTogetherByWordStrings, versePutTogetherByVerseTokensWithPadding);
            }
        }

        [Fact]
        [Trait("Category", "Example")]
        public void Corpus_SytaxTrees_Read_LEV()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree, Persistence.FileGetBookIds.LanguageCodeEnum.H);

                // now get the first 5 verses
                foreach (var tokensTextRow in sourceCorpus["LEV"].GetRows().Cast<TokensTextRow>().Take(5))
                {
                    TestHelpers.WriteTokensTextRow(output_, tokensTextRow, new EngineStringDetokenizer(new WhitespaceDetokenizer()));
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
        public void Corpus_SytaxTrees_Read_ACT()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree, Persistence.FileGetBookIds.LanguageCodeEnum.G);

                // now get the first 5 verses
                foreach (var tokensTextRow in sourceCorpus["ACT"].GetRows().Cast<TokensTextRow>().Take(5))
                {
                    TestHelpers.WriteTokensTextRow(output_, tokensTextRow, new EngineStringDetokenizer(new WhitespaceDetokenizer()));
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
        public void Corpus_SytaxTrees_Read_BookCounts()
        {
            var syntaxTree = new SyntaxTrees();
            var sourceCorpusAll = new SyntaxTreeFileTextCorpus(syntaxTree);

            var sourceCorpusGreek = new SyntaxTreeFileTextCorpus(syntaxTree, Persistence.FileGetBookIds.LanguageCodeEnum.G);

            var sourceCorpusHebrew = new SyntaxTreeFileTextCorpus(syntaxTree, Persistence.FileGetBookIds.LanguageCodeEnum.H);

            Assert.Equal(sourceCorpusAll.Texts.Count(), sourceCorpusGreek.Texts.Count() + sourceCorpusHebrew.Texts.Count());
            Assert.True(sourceCorpusGreek.Texts.Count() > 0);
            Assert.True(sourceCorpusHebrew.Texts.Count() > 0);
        }

        [Fact]
        public void Corpus_SytaxTrees_ByLanguage()
        {
            var syntaxTree = new SyntaxTrees();
            var sourceCorpusHebrew = new SyntaxTreeFileTextCorpus(syntaxTree, Persistence.FileGetBookIds.LanguageCodeEnum.H);

            Assert.NotEmpty(sourceCorpusHebrew);
            Assert.NotEmpty(sourceCorpusHebrew["GEN"].GetRows());
            try
            {
                sourceCorpusHebrew["MAT"].GetRows();
                Assert.True(false);
            }
            catch (KeyNotFoundException)
            {
                Assert.True(true);
            }

            var sourceCorpusGreek = new SyntaxTreeFileTextCorpus(syntaxTree, Persistence.FileGetBookIds.LanguageCodeEnum.G);

            Assert.NotEmpty(sourceCorpusGreek);
            try
            {
                sourceCorpusGreek["GEN"].GetRows();
                Assert.True(false);
            }
            catch (KeyNotFoundException)
            {
                Assert.True(true);
            }
            Assert.NotEmpty(sourceCorpusGreek["MAT"].GetRows());
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
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[9].SurfaceText);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[9].TrainingText);
            Assert.NotEmpty(corpusList[0].Segment[9]);
            Assert.NotEqual(((TokensTextRow)corpusList[0]).Tokens[9].TrainingText, ((TokensTextRow)corpusList[0]).Tokens[9].SurfaceText);
            Assert.Equal(((TokensTextRow)corpusList[0]).Tokens[9].TrainingText, corpusList[0].Segment[9]);
        }
    }
}
