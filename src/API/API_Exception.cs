using System;
using System.Collections;
using System.Runtime.Serialization;

namespace ClearBible.Clear3.API

{

    public class ClientException : Exception
    {
        public ClientException(string message) : base(message)
        {
        }

        public ClientException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }

    public class ExcessiveMemoryUsageException : ClientException
    {
        public ExcessiveMemoryUsageException(string message): base(message)
        {
        }

        public ExcessiveMemoryUsageException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }

    public class InvalidInputException: ClientException
    {

        public InvalidInputException(string message): base(message)
        {
        }

        public InvalidInputException(string message, Exception inner) 
            : base(message, inner)
        {
        }

    }

}