using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Engine.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetParallelCorpusInfoByParallelCorpusIdQueryHandler : IRequestHandler<
        GetParallelCorpusInfoByParallelCorpusIdQuery,
        RequestResult<(CorpusIdVersionId sourceCorpusIdVersionId, 
            CorpusIdVersionId targetCorpusIdVersionId, 
            IEnumerable<EngineVerseMapping> engineVerseMappings)>>
    {
        public Task<RequestResult<(CorpusIdVersionId sourceCorpusIdVersionId,
            CorpusIdVersionId targetCorpusIdVersionId,
            IEnumerable<EngineVerseMapping> engineVerseMappings)>>
            Handle(GetParallelCorpusInfoByParallelCorpusIdQuery command, CancellationToken cancellationToken)
        {

            return Task.FromResult(
                new RequestResult<(CorpusIdVersionId sourceCorpusIdVersionId,
            CorpusIdVersionId targetCorpusIdVersionId,
            IEnumerable<EngineVerseMapping> engineVerseMappings)>
                (result: (new CorpusIdVersionId(3, 4), new CorpusIdVersionId(5, 6), new List<EngineVerseMapping>()),
                success: true,
                message: "successful result from test"));
        }
    }
}
