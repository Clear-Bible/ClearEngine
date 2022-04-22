using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Exceptions
{
    public class InvalidTreeEngineException : EngineException
    {
       public InvalidTreeEngineException(
            IDictionary<string, string>? nameValueMap = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(nameValueMap, memberName, sourceFilePath, sourceLineNumber)
        {
        }

        public InvalidTreeEngineException(string message,
            IDictionary<string, string>? nameValueMap = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, nameValueMap, memberName, sourceFilePath, sourceLineNumber)
        {
        }

        public InvalidTreeEngineException(string message, 
            Exception inner,
            IDictionary<string, string>? nameValueMap = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, inner, nameValueMap, memberName, sourceFilePath, sourceLineNumber)
        {
        }
    }
}
