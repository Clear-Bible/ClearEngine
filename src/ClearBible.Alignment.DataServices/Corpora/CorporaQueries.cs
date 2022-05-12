using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using ClearDashboard.DAL.CQRS;
using MediatR;
using SIL.Machine.Corpora;
using static ClearBible.Alignment.DataServices.Corpora.CorpusUri;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public class CorporaQueries : ICorporaQueryable
    {
        private readonly IMediator mediator_;

        public CorporaQueries(IMediator mediator)
        {
            mediator_ = mediator;
        }

        private async Task<ScriptureTextCorpus?> GetCorpus(IRequest<RequestResult<ScriptureTextCorpus>> command)
        {
            var result = await mediator_.Send(command);
            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
            else
            {
                return null;
            }
        }
        public Task<ScriptureTextCorpus?> GetCorpus(CorpusUri corpusUri) => corpusUri.SourceType switch
        {
            SourceTypeEnum.ParatextDirectory => GetCorpus(new GetParatextCorpusByDirectoryPathQuery(corpusUri.Identifier)),
            SourceTypeEnum.ParatextPlugin => GetCorpus(new GetParatextCorpusByPluginIdQuery(corpusUri.Identifier.AsInt("corpusUri.Identifier"))),
            SourceTypeEnum.Database => GetCorpus(new GetCorpusByCorpusIdQuery(new CorpusId(corpusUri.Identifier))),
            _ => throw new InvalidParameterEngineException(message: "Mediator command not found for uri", name: "corpusUri", value: corpusUri.ToString()),
        };

        public async Task<IEnumerable<CorpusId>?> GetCorpusIds()
        {
            var result = await mediator_.Send(new GetCorpusIdsQuery());
            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
            else
            {
                return null;
            }
        }

        public async Task<EngineParallelTextCorpus?> GetParallelCorpus(ParallelCorpusId parallelCorpusId)
        {
            var result = await mediator_.Send(new GetParallelCorpusByParallelCorpusIdQuery(parallelCorpusId));
            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
            else
            {
                return null;
            }
        }

        public async Task<IEnumerable<ParallelCorpusId>?> GetParallelCorpusIds()
        {
            var result = await mediator_.Send(new GetParallelCorpusIdsQuery());
            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
            else
            {
                return null;
            }
        }

        public async Task<IEnumerable<EngineVerseMapping>?> GetVerseMappings(ParallelCorpusId parallelCorpusId)
        {
            var result = await mediator_.Send(new GetVerseMappingsByParallelCorpusIdQuery(parallelCorpusId));
            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else if (!result.Success)
            {
                throw new MediatorErrorEngineException(result.Message);
            }
            else
            {
                return null;
            }
        }
    }
}
