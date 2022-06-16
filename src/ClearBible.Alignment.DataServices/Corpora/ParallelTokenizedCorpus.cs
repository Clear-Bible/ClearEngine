using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;


namespace ClearBible.Alignment.DataServices.Corpora
{
    public class ParallelTokenizedCorpus : EngineParallelTextCorpus
    {
        public ParallelTokenizedCorpusId ParallelTokenizedCorpusId { get; set; }

        public static async Task<IEnumerable<ParallelCorpusVersionId>?> GetAll(IMediator mediator)
        {
            var result = await mediator.Send(new GetAllParallelCorpusIdVersionIdsQuery());
            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        public static async Task<ParallelTokenizedCorpus?> Get(
            IMediator mediator,
            ParallelTokenizedCorpusId parallelTokenizedCorpusId)
        {
            var command = new GetParallelTokenizedCorpusByParallelTokenizedCorpusIdQuery(parallelTokenizedCorpusId);

            var result = await mediator.Send(command);
            if (result.Success)
            {
                var info =  result.Data;
                return new ParallelTokenizedCorpus(
                    await TokenizedTextCorpus.Get(mediator, info.sourceTokenizedCorpusId), 
                    await TokenizedTextCorpus.Get(mediator, info.targetTokenizedCorpusId), 
                    info.engineVerseMappings, parallelTokenizedCorpusId);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        internal ParallelTokenizedCorpus(
            TokenizedTextCorpus sourceTokenizedTextCorpus,
            TokenizedTextCorpus targetTokenizedTextCorpus,
            IEnumerable<EngineVerseMapping> engineVerseMappings,
            ParallelTokenizedCorpusId parallelTokenizedCorpusId)
            : base(sourceTokenizedTextCorpus, targetTokenizedTextCorpus, engineVerseMappings.ToList())
        {
            ParallelTokenizedCorpusId = parallelTokenizedCorpusId;
        }
    }
}
