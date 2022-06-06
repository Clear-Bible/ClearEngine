using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;


namespace ClearBible.Alignment.DataServices.Corpora
{
    public class ParallelCorpusFromDb : EngineParallelTextCorpus
    {
        public ParallelCorpusIdVersionId ParallelCorpusIdVersionId { get; set; }

        public static async Task<IEnumerable<ParallelCorpusIdVersionId>?> GetAll(IMediator mediator)
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

        public static async Task<ParallelCorpusFromDb?> Get(
            IMediator mediator,
            ParallelCorpusIdVersionId parallelCorpusIdVersionId)
        {
            var command = new GetParallelCorpusInfoByParallelCorpusIdQuery(parallelCorpusIdVersionId);

            var result = await mediator.Send(command);
            if (result.Success)
            {
                var info =  result.Data;
                return new ParallelCorpusFromDb(
                    await TextCorpusFromDb.Get(mediator, info.sourceCorpusIdVersionId), 
                    await TextCorpusFromDb.Get(mediator, info.sourceCorpusIdVersionId), 
                    info.engineVerseMappings, parallelCorpusIdVersionId);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        internal ParallelCorpusFromDb(
            TextCorpusFromDb sourceTextCorpusFromDb,
            TextCorpusFromDb targetTextCorpusFromDb,
            IEnumerable<EngineVerseMapping> engineVerseMappings,
            ParallelCorpusIdVersionId parallelCorpusVersionId)
            : base(sourceTextCorpusFromDb, targetTextCorpusFromDb, engineVerseMappings.ToList())
        {
            ParallelCorpusIdVersionId = parallelCorpusVersionId;
        }
    }
}
