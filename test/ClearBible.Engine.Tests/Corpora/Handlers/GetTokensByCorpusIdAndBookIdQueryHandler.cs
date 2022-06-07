using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

using ClearDashboard.DAL.CQRS;
using ClearBible.Engine.Corpora;
using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Tokenization;

using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetTokensByCorpusIdAndBookIdQueryHandler : IRequestHandler<
        GetTokensByCorpusIdAndBookIdQuery,
        RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>>
    {
        public Task<RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>>
            Handle(GetTokensByCorpusIdAndBookIdQuery command, CancellationToken cancellationToken)
        {
            var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, TestDataHelpers.UsfmTestProjectPath)
                .Tokenize<LatinWordTokenizer>()
                .Transform<IntoTokensTextRowProcessor>();

            var chapterVerseTokens = corpus.GetRows(new List<string>() { command.BookId })
                .GroupBy(r => ((VerseRef)r.Ref).ChapterNum)
                .OrderBy(g => g.Key)
                .SelectMany(g => g
                    .GroupBy(sg => ((VerseRef)sg.Ref).VerseNum)
                    .OrderBy(sg => sg.Key)
                    .Select(sg => new
                    {
                        Chapter = g.Key,
                        Verse = sg.Key,
                        Tokens = sg
                            .SelectMany(v => ((TokensTextRow)v).Tokens),
                        IsSentenceStart = sg
                            .First().IsSentenceStart
                    })
                )
                .Select(cvts => (cvts.Chapter.ToString(), cvts.Verse.ToString(), cvts.Tokens, cvts.IsSentenceStart));


            return Task.FromResult(
                new RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>
                (result: chapterVerseTokens,
                success: true,
                message: "successful result from test"));
        }
    }
}
