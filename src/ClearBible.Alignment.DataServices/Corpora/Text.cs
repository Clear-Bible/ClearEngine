using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace ClearBible.Alignment.DataServices.Corpora
{
    internal class Text<T> : ScriptureText
        where T : GetTokensByBookIdBaseQuery,
        new()
    {
        private readonly object id_;
        private readonly IMediator mediator_;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parallelCorpusId">primary key of parallel corpus id db entity</param>
        /// <param name="bookId">book in three character SIL format</param>
        /// <param name-"isSource">if true, get source corpora, else target</param>
        /// <param name="versification"></param>
        //public Text(IMediator mediator, ParallelCorpusId parallelCorpusId, string bookId, bool isSource, ScrVers versification)
        //    : base(bookId, versification)
        //{
            //IMPLEMENTER'S NOTES:: needs to configure GetVersesInDocOrder() to ONLY return the text parallel related.
        //}

        public Text(object id, IMediator mediator, ScrVers versification, string bookId )
            : base(bookId, versification)
        {
            id_ = id;
            mediator_ = mediator;
        }
        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            var command = new T();
            command.Id = id_;
            command.BookId = Id; //Note that in ScriptureText Id is the book abbreviation bookId.

            var result = Task.Run(() => mediator_.Send(command)).GetAwaiter().GetResult();
            if (result.Success)
            {
                var verses = result.Data;

                if (verses == null)
                    throw new MediatorErrorEngineException("GetCorpusTokensByBookIdCommand returned null data");

                return verses
                    .SelectMany(verse => CreateRows(verse.chapter, verse.verse, "", verse.isSentenceStart) // text parameter is overridden by TokensTextRow and is therefore not needed here.
                        .Select(tr => new TokensTextRow(tr, verse.tokens.ToList()))); //MUST return TokensTextRow. 
            }
            else
            {
                throw new MediatorErrorEngineException(result.Message);
            }
        }
    }
}
