using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Translation
{
    public class ManuscriptWordAlignmentException : ApplicationException
    {
        public ManuscriptWordAlignmentException()
        {
        }

        public ManuscriptWordAlignmentException(string? message) : base(message)
        {
        }

        public ManuscriptWordAlignmentException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ManuscriptWordAlignmentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
