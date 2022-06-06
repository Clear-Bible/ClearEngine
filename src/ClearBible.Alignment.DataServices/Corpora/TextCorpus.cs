using ClearBible.Alignment.DataServices.Features.Corpora;
using ClearBible.Engine.Exceptions;
using MediatR;

using SIL.Machine.Corpora;
using SIL.Scripture;


namespace ClearBible.Alignment.DataServices.Corpora
{
    public abstract class TextCorpus<T> : ScriptureTextCorpus
        where T : GetTokensByBookIdBaseQuery, new()
    {
        public object Id { get; set; }

        internal TextCorpus(object id, IMediator mediator, int versification, IEnumerable<string> bookAbbreviations)
        {
            Id = id;

            Versification = new ScrVers((ScrVersType)versification);

            foreach (var bookAbbreviation in bookAbbreviations)
            {
                AddText(new Text<T>(Id, mediator, Versification, bookAbbreviation));
            }
        }
        public override ScrVers Versification { get; }

    }
}
