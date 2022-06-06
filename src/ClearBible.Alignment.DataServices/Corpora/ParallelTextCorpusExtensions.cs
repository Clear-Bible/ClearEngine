using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Machine.Corpora;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public static class ParallelTextCorpusExtensions
    {
        public static async Task<ParallelCorpusFromDb?> Update(ParallelCorpusFromDb parallelCorpus, IMediator mediator)
        {
            var sourceCorpusIdVersionId = (CorpusIdVersionId) ((TextCorpus < GetTokensByCorpusIdAndBookIdQuery> ) parallelCorpus.SourceCorpus).Id;
            var targetCorpusIdVersionId = (CorpusIdVersionId) ((TextCorpus < GetTokensByCorpusIdAndBookIdQuery > ) parallelCorpus.TargetCorpus).Id;
            var engineVerseMappingList = parallelCorpus.EngineVerseMappingList ?? throw new InvalidStateEngineException(name:"EngineVerseMappingList", value: "null");

            var command = new UpdateParallelCorpusInfoCommand(sourceCorpusIdVersionId, targetCorpusIdVersionId, engineVerseMappingList, parallelCorpus.ParallelCorpusIdVersionId);

            var result = await mediator.Send(command);
            if (result.Success)
            {
                var info = result.Data;
                return new ParallelCorpusFromDb(
                    await TextCorpusFromDb.Get(mediator, info.sourceCorpusIdVersionId),
                    await TextCorpusFromDb.Get(mediator, info.sourceCorpusIdVersionId),
                    info.engineVerseMappings, 
                    info.parallelCorpusIdVersionId);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }

        public static async Task<ParallelCorpusFromDb?> Create(this EngineParallelTextCorpus engineParallelTextCorpus, IMediator mediator)
        {
            if (
                engineParallelTextCorpus.SourceCorpus.GetType() != typeof(TextCorpus<GetTokensByCorpusIdAndBookIdQuery>)
                ||
                engineParallelTextCorpus.TargetCorpus.GetType() != typeof(TextCorpus<GetTokensByCorpusIdAndBookIdQuery>))
            {
                throw new InvalidTypeEngineException(
                    name: "sourceOrTargetCorpus",
                    value: "Not TextCorpus<GetCorpusTokensByBookIdByCorpusIdCommand>",
                    message: "both SourceCorpus and TargetCorpus of engineParallelTextCorpus must be from the database (of type TextCorpus<GetTokensByCorpusIdAndBookIdQuery>");
            }

            if (engineParallelTextCorpus.GetType() == typeof(TextCorpus<GetTokensByCorpusIdAndBookIdQuery>))
            {
                throw new InvalidTypeEngineException(
                    name: "engineParallelTextCorpus",
                    value: "TextCorpus<GetCorpusTokensByBookIdByCorpusIdCommand>");
            }

            var command = new CreateParallelCorpusInfoCommand(
                (CorpusIdVersionId)((TextCorpus<GetTokensByCorpusIdAndBookIdQuery>)engineParallelTextCorpus.SourceCorpus).Id,
                (CorpusIdVersionId)((TextCorpus<GetTokensByCorpusIdAndBookIdQuery>)engineParallelTextCorpus.TargetCorpus).Id,
                engineParallelTextCorpus.EngineVerseMappingList ?? throw new InvalidParameterEngineException(name: "EngineVerseMappings", value: "null"));

            var result = await mediator.Send(command);
            if (result.Success)
            {
                var info = result.Data;
                return new ParallelCorpusFromDb(
                    await TextCorpusFromDb.Get(mediator, info.sourceCorpusIdVersionId),
                    await TextCorpusFromDb.Get(mediator, info.sourceCorpusIdVersionId),
                    info.engineVerseMappings, 
                    info.parallelCorpusIdVersionId);
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
