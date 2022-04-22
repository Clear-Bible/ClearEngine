using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Exceptions
{
    public class EngineException : Exception
    {
        public static ResourceManager? resourceManager { get; set; } = null;
        public IDictionary<string, string>? NameValueMap { get; }
        public string MemberName { get; }
        public string SourceFilePath { get; }
        public int SourceLineNumber { get; }

        public EngineException(
            IDictionary<string, string>? nameValueMap = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            NameValueMap = nameValueMap;
            MemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }

        public EngineException(
            string message,
            IDictionary<string, string>? nameValueMap = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(resourceManager?.GetString(message) ?? message)
        {
            NameValueMap = nameValueMap;
            MemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }

        public EngineException(
            string message, 
            Exception inner,
            IDictionary<string, string>? nameValueMap = null,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
            : base(resourceManager?.GetString(message) ?? message, inner)
        {
            NameValueMap = nameValueMap;
            MemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }

        public override string ToString()
        {
            return 
@$"Base: {base.ToString()}

NameValueMap: {string.Join(" ", NameValueMap)}
MemberName: { MemberName}
SourceFilePath {SourceFilePath}
SourceLineNumber {SourceLineNumber}";
}
    }
}
