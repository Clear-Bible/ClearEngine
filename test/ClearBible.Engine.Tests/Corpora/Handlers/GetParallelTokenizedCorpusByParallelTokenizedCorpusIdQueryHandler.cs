using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClearBible.Alignment.DataServices.Corpora;
using ClearBible.Engine.Corpora;
using ClearDashboard.DAL.CQRS;
using ClearBible.Alignment.DataServices.Features.Corpora;
using System;

namespace ClearBible.Engine.Tests.Corpora.Handlers
{
    public class GetParallelTokenizedCorpusByParallelTokenizedCorpusIdQueryHandler : IRequestHandler<
        GetParallelTokenizedCorpusByParallelTokenizedCorpusIdQuery,
        RequestResult<(TokenizedCorpusId sourceTokenizedCorpusId,
            TokenizedCorpusId targetTokenizedCorpusId, 
            IEnumerable<EngineVerseMapping> engineVerseMappings)>>
    {
        public Task<RequestResult<(TokenizedCorpusId sourceTokenizedCorpusId,
            TokenizedCorpusId targetTokenizedCorpusId,
            IEnumerable<EngineVerseMapping> engineVerseMappings)>>
            Handle(GetParallelTokenizedCorpusByParallelTokenizedCorpusIdQuery command, CancellationToken cancellationToken)
        {
            //DB Impl notes: use command.ParallelTokenizedCorpus to retrieve from ParallelTokenizedCorpus table and return
            //the TokenizedCorpusId for both and target and also the result of gathering all the VerseMappings under
            // parent parallelTokenizedCorpus.ParallelCorpusVersion to build an EngineVerseMapping list.

            return Task.FromResult(
                new RequestResult<(TokenizedCorpusId sourceTokenizedCorpusId,
            TokenizedCorpusId targetTokenizedCorpusId,
            IEnumerable<EngineVerseMapping> engineVerseMappings)>
                (result: (new TokenizedCorpusId(new Guid()), new TokenizedCorpusId(new Guid()), new List<EngineVerseMapping>()),
                success: true,
                message: "successful result from test"));
        }
    }
}
