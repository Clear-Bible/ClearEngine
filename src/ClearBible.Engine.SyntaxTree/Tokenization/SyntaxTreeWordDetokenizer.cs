using ClearBible.Engine.Corpora;
using SIL.Machine.Tokenization;

using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.SyntaxTree.Tokenization
{
	public class SyntaxTreeWordDetokenizer : IDetokenizer<string, Token>
	{
		public string Detokenize(IEnumerable<Token> tokens)
		{
			return tokens
				.OrderBy(t => t.TokenId.ToString())
				.GroupBy(t => t.TokenId.WordNumber)
				.Select(g => g
					.Aggregate(string.Empty, (constructedString, token) => $"{constructedString}{token.SurfaceText}")) //words put together without spaces.
				.Aggregate(string.Empty, (constructedString, wordString) => $"{constructedString} {wordString}");//put words together separated with spaces into a verse.
		}
	}
}
