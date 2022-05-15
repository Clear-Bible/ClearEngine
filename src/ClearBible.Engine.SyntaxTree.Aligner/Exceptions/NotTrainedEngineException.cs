using ClearBible.Engine.Exceptions;

namespace ClearBible.Engine.SyntaxTree.Aligner.Exceptions
{
    public class NotTrainedEngineException : EngineException
    {
        public NotTrainedEngineException(
            string name,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath,sourceLineNumber)
        {
        }

        public NotTrainedEngineException(
            string message,
            string name,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath, sourceLineNumber)
        {
        }

        public NotTrainedEngineException(
            string message, 
            Exception inner,
            string name,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, inner, new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath, sourceLineNumber)
        {
        }
    }
}
