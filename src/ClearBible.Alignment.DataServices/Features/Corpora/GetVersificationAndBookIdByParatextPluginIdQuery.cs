using ClearBible.Alignment.DataServices.Corpora;

namespace ClearBible.Alignment.DataServices.Features.Corpora
{
    public record GetVersificationAndBookIdByParatextPluginIdQuery : GetVersificationAndBookIdsBaseQuery
    {
        public GetVersificationAndBookIdByParatextPluginIdQuery(string paratextPluginId)
        {
            Id = paratextPluginId;
        }
    }
}
