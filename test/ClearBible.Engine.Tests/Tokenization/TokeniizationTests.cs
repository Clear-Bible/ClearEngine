using ClearBible.Engine.Corpora;
using ClearBible.Engine.Tests.Corpora;
using ClearBible.Engine.Tokenization;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;



namespace ClearBible.Engine.Tests.Tokenization
{
    public class TokeniizationTests
    {
        protected readonly ITestOutputHelper output_;
        public TokeniizationTests(ITestOutputHelper output)
        {
            output_ = output;
        }

        [Fact]
        public void Tokenization__ZwspWordTokenizerSpaceSegmentsDontException()
        {
            Assert.True(true);
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestHelpers.UsfmTestProjectPath)
            .Tokenize<ZwspWordTokenizer>()
            .Transform<IntoTokensTextRowProcessor>()
            .ToList();
            Assert.True(true); //didn't exception
        }
    }
}
