

namespace ClearBible.Engine.Corpora
{
    public interface IExtendedPropertiesSource
    {
        string? GetExtendedPropertiesObjectForToken(TokenId tokenId);
    }
}
