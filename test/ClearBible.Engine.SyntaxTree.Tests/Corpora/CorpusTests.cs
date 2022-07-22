
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;
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
        public void SyntaxTrees_Read()
        {
            try
            {
                var syntaxTree = new SyntaxTrees();
                var sourceCorpus = new SyntaxTreeFileTextCorpus(syntaxTree);

                // now get the first 5 verses
                foreach (var tokensTextRow in sourceCorpus.Cast<TokensTextRow>().Take(5))
                {
                    var sourceVerseText = string.Join(" ", tokensTextRow.Segment);
                    output_.WriteLine($"Source: {sourceVerseText}");
                }
            }
            catch (EngineException eex)
            {
                output_.WriteLine(eex.ToString());
                throw eex;
            }
        }
    }
}
