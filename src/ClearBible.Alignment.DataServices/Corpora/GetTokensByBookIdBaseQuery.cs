using ClearBible.Engine.Corpora;
using ClearDashboard.DAL.CQRS;
using MediatR;


namespace ClearBible.Alignment.DataServices.Corpora
{
    public abstract record GetTokensByBookIdBaseQuery : IRequest<RequestResult<IEnumerable<(string chapter, string verse, IEnumerable<Token> tokens, bool isSentenceStart)>>>
    {
        public GetTokensByBookIdBaseQuery(object id, string bookId)
        {
            Id = id;
            BookId = bookId;
        }

        public GetTokensByBookIdBaseQuery() // required by TextCorpus generic new() constraint.
        {
            Id = new object();
            BookId = "";
        }

        public object Id { get; set; }
        public string BookId { get; set; }
    }
}
