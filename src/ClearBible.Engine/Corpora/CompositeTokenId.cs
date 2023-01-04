using ClearBible.Engine.Exceptions;
using System.Collections;

namespace ClearBible.Engine.Corpora
{
    public class CompositeTokenId : TokenId, IEnumerable<TokenId>
    {
        public readonly string CompositeTokensIdDelimiter = "-";
        public IEnumerable<TokenId> TokenIds { get;}


        public CompositeTokenId(IEnumerable<Token> tokens) : base(0,0,0,0,0) //values are never accessed.
        {
            TokenIds = tokens.Select(t => t.TokenId);
        }
        public IEnumerator<TokenId> GetEnumerator()
        {
            return TokenIds.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public override bool Equals(object? obj)
        {
            return obj is CompositeTokenId compositeTokenId &&
                TokenIds
                    .OrderBy(tid => tid)
                    .SequenceEqual(compositeTokenId.TokenIds
                        .OrderBy(tid => tid));
        }
        public override int GetHashCode()
        {
            return TokenIds.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode()); 
            //see https://docs.microsoft.com/en-us/dotnet/api/system.object.gethashcode?view=net-6.0
        }
        public bool Equals(CompositeTokenId? other)
        {
            return Equals((object?)other);
        }

        public override string ToString()
        {
            return string.Join(CompositeTokensIdDelimiter, TokenIds.Select(t => t.ToString()));
        }

        public override int BookNumber
        {
            get
            {
                throw new EngineException("Cannot get from type CompositeTokenId. Retrieve from composed tokenIdss available through TokenIds property or by enumerating this.");
            }
        }
        public override int ChapterNumber
        {
            get
            {
                throw new EngineException("Cannot get from type CompositeTokenId. Retrieve from composed tokenIdss available through TokenIds property or by enumerating.");
            }
        }
        public override int VerseNumber
        {
            get
            {
                throw new EngineException("Cannot get from type CompositeTokenId. Retrieve from composed tokenIdss available through TokenIds property or by enumerating.");
            }
        }
        public override int WordNumber
        {
            get
            {
                throw new EngineException("Cannot get from type CompositeTokenId. Retrieve from composed tokenIdss available through TokenIds property or by enumerating.");
            }
        }
        public override int SubWordNumber
        {
            get
            {
                throw new EngineException("Cannot get from type CompositeTokenId. Retrieve from composed tokenIdss available through TokenIds property or by enumerating.");
            }
        }

        public override bool IsSiblingSubword(TokenId tokenId)
        {
            throw new EngineException("Cannot check before for a compositetokenid.");
        }
    }
}   
