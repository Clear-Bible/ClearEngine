using ClearDashboard.DAL.CQRS;
using MediatR;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    public abstract record GetVersificationAndBookIdsBaseQuery : IRequest<RequestResult<(ScrVers? versification, IEnumerable<string> bookAbbreviations)>>
    {
        public object Id { get; set; } = new object(); //derived classes must ensure this is set.
    }
}
