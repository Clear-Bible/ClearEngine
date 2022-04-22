using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Exceptions
{
    public class InvalidStateEngineException : EngineException
    {
        public InvalidStateEngineException(
            string name,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath,sourceLineNumber)
        {
        }

        public InvalidStateEngineException(string message,
            string name,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath, sourceLineNumber)
        {
        }

        public InvalidStateEngineException(
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
