using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;
using System;
using System.Collections.Generic;
using System.IO;
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
                .Transform<SetTrainingBySurfaceLowercase>();

            Assert.NotEmpty(corpus);
            Assert.All(corpus, c => Assert.IsType<TokensTextRow>(c));
            var corpusList = corpus.ToList();

            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[0].SurfaceText);
            Assert.NotEmpty(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText);
            Assert.NotEqual(((TokensTextRow)corpusList[0]).Tokens[0].TrainingText, ((TokensTextRow)corpusList[0]).Tokens[0].SurfaceText);
        }

        [Fact]
        public void Corpus_TokenPropertiesJson()
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestHelpers.UsfmTestProjectPath)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>();
            var sourceCorpusFirstVerse = corpus.First();
            var firstToken = ((TokensTextRow)sourceCorpusFirstVerse).Tokens.First();
            Assert.Null(firstToken.ExtendedProperties);
            Assert.IsType<Token>(firstToken);

            //firstToken.PropertiesJson = "blah";
            //can set because its a Token
            //Assert.Equal("blah", firstToken.PropertiesJson);
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
                    .Transform<SetTrainingBySurfaceLowercase>();

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
                .Transform<SetTrainingBySurfaceLowercase>()
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

        [Fact]
        [Trait("Category", "Example")]
        public void Corpus__GetByVerseRange()
        {
            /*
             *  Should result in the following mapping for MAT 1:
             *  (note in verse mappings a mapping is always corpusverse=originalverse, 
             *  and that SIL.Scripture.Versification changes from corpusversification -> originalversification -> newversification)
             *  
             *  Corpus       Original  
             *     2            1       
             *     3            2       
             *     1            3       
             * 
             */
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
            string customVersificationAddition = "&MAT 1:2 = MAT 1:1\nMAT 1:3 = MAT 1:2\nMAT 1:1 = MAT 1:3\n";
            ScrVers versification;
            using (var reader = new StringReader(customVersificationAddition))
            {
                versification = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            }

            var texts = new List<IText>() 
            {
                new MemoryText("MAT", new[]
                {
                    TextRow(new VerseRef("MAT 1:1", versification), "source MAT chapter one, verse one."),
                    TextRow(new VerseRef("MAT 1:2", versification), "source MAT chapter one, verse two."),
                    TextRow(new VerseRef("MAT 1:3", versification), "source MAT chapter one, verse three."),
                    TextRow(new VerseRef("MAT 1:4", versification), "source MAT chapter one, verse four."),
                    TextRow(new VerseRef("MAT 1:5", versification), "source MAT chapter one, verse five."),
                    TextRow(new VerseRef("MAT 1:6", versification), "source MAT chapter one, verse six.")
                }),
                new MemoryText("MRK", new[]
                {
                    TextRow(new VerseRef("MRK 1:1", versification), "source MRK chapter one, verse one."),
                    TextRow(new VerseRef("MRK 1:2", versification), "source MRK chapter one, verse two."),
                    TextRow(new VerseRef("MRK 1:3", versification), "source MRK chapter one, verse three."),
                    TextRow(new VerseRef("MRK 1:4", versification), "source MRK chapter one, verse four."),
                    TextRow(new VerseRef("MRK 1:5", versification), "source MRK chapter one, verse five."),
                    TextRow(new VerseRef("MRK 1:6", versification), "source MRK chapter one, verse six.")
                })
            };

            var corpus = new TestScriptureTextCorpus(texts, versification); //versification is custom

            //now change from custom versification to original. First parameter is interpreted as in corpus versification
            var textRowsAndIndex3_1_1 = corpus.GetByVerseRange(new VerseRef("MAT 1:3"), 1, 1, ScrVers.Original);
            Assert.Equal(3, textRowsAndIndex3_1_1.textRows.Count());
            Assert.Equal(1, textRowsAndIndex3_1_1.indexOfVerse);

            var list = textRowsAndIndex3_1_1.textRows.ToList();
            Assert.True(list[0].Ref.Equals(new VerseRef("MAT 1:3", versification)));
            Assert.True(list[1].Ref.Equals(new VerseRef("MAT 1:1", versification)));
            Assert.True(list[2].Ref.Equals(new VerseRef("MAT 1:4", versification)));

            var textRowsAndIndex3_1_3 = corpus.GetByVerseRange(new VerseRef("MAT 1:3"), 1, 3, ScrVers.Original);
            Assert.Equal(5, textRowsAndIndex3_1_3.textRows.Count());
            Assert.Equal(1, textRowsAndIndex3_1_3.indexOfVerse);

            var textRowsAndIndex3_4_4 = corpus.GetByVerseRange(new VerseRef("MAT 1:3"), 4, 4, ScrVers.Original);
            Assert.Equal(6, textRowsAndIndex3_4_4.textRows.Count());
            Assert.Equal(2, textRowsAndIndex3_4_4.indexOfVerse);

            var textRowsAndIndex3_3_14 = corpus.GetByVerseRange(new VerseRef("MAT 1:3"), 3, 14, ScrVers.Original);
            Assert.Equal(6, textRowsAndIndex3_3_14.textRows.Count());
            Assert.Equal(2, textRowsAndIndex3_3_14.indexOfVerse);

            list = textRowsAndIndex3_3_14.textRows.ToList();
            Assert.True(list[0].Ref.Equals(new VerseRef("MAT 1:2", versification)));
            Assert.True(list[1].Ref.Equals(new VerseRef("MAT 1:3", versification)));
            Assert.True(list[2].Ref.Equals(new VerseRef("MAT 1:1", versification)));
            Assert.True(list[3].Ref.Equals(new VerseRef("MAT 1:4", versification)));
            Assert.True(list[4].Ref.Equals(new VerseRef("MAT 1:5", versification)));
        }

        [Fact]
        public void ExtractScripture()
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestHelpers.UsfmTestProjectPath);

            var lines = corpus.ExtractScripture().ToList();
            Assert.Equal(41899, lines.Count);

            (string text, VerseRef origRef, VerseRef? corpusRef) = lines[0];
            Assert.Equal("", text);
            Assert.Equal(new VerseRef("GEN 1:1", ScrVers.Original), origRef);
            Assert.False(corpusRef.HasValue);

            (text, origRef, corpusRef) = lines[23213];
            Assert.Equal("Chapter one, verse one.", text);
            Assert.Equal(new VerseRef("MAT 1:1", ScrVers.Original), origRef);
            Assert.True(corpusRef.HasValue);
            Assert.Equal(new VerseRef("MAT 1:1", corpus.Versification), corpusRef);

            (text, origRef, corpusRef) = lines[23240];
            Assert.Equal("<range>", text);
            Assert.Equal(new VerseRef("MAT 2:3", ScrVers.Original), origRef);
            Assert.True(corpusRef.HasValue);
            Assert.Equal(new VerseRef("MAT 2:3", corpus.Versification), corpusRef);
        }
        class TestScriptureTextCorpus : ScriptureTextCorpus
        {
            public TestScriptureTextCorpus(IEnumerable<IText> texts, ScrVers versification)
            {
                foreach(var text in texts)
                {
                    AddText(text);
                }
                Versification = versification;
            }
        }
        private static TextRow TextRow(VerseRef vref, string text = "", bool isSentenceStart = true,
             bool isInRange = false, bool isRangeStart = false)
        {
            return new TextRow(vref)
            {
                Segment = new[] { text },
                IsSentenceStart = isSentenceStart,
                IsInRange = isInRange,
                IsRangeStart = isRangeStart,
                IsEmpty = text.Length == 0
            };
        }
    }
}
