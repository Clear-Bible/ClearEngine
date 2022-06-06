using ClearDashboard.DAL.CQRS;
using MediatR;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public abstract record GetVersificationAndBookIdsBaseQuery : IRequest<RequestResult<(int versification, List<string> bookAbbreviations)>>
    {
        public object Id { get; set; } = new object(); //derived classes must ensure this is set.
    }
}
