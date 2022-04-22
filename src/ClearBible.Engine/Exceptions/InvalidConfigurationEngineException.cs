using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Exceptions
{
    public class InvalidConfigurationEngineException : EngineException
    {
        public InvalidConfigurationEngineException(
            string name = "",
            string value = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath,sourceLineNumber)
        {
        }

        public InvalidConfigurationEngineException(
            string message,
            string name = "",
            string value = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath, sourceLineNumber)
        {
        }

        public InvalidConfigurationEngineException(
            string message, 
            Exception inner,
            string name = "",
            string value = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, inner, new Dictionary<string, string> { { name, value } }, memberName, sourceFilePath, sourceLineNumber)
        {
        }
        public InvalidConfigurationEngineException(
            IDictionary<string, string>? nameValueMap,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(nameValueMap, memberName, sourceFilePath, sourceLineNumber)
                {
        }

        public InvalidConfigurationEngineException(
            string message,
            IDictionary<string, string>? nameValueMap,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, nameValueMap, memberName, sourceFilePath, sourceLineNumber)
        {
        }

        public InvalidConfigurationEngineException(
            string message,
            Exception inner,
            IDictionary<string, string>? nameValueMap,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(message, inner, nameValueMap, memberName, sourceFilePath, sourceLineNumber)
        {
        }
    }
}
