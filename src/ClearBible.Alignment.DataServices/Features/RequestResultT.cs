using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Alignment.DataServices.Features
{
    public class RequestResult<T> : Result<T>
    {
        public RequestResult()
        {
        }
        public RequestResult(T? result = default(T), bool success = true, string message = "Success") : base(result, success, message)
        {
        }
    }
    public abstract class Result<T>
    {
        protected Result() 
        { 
            Message = "Success"; 
        }
        protected Result(T? result = default(T), bool success = true, string message = "Success")
        {
            Success = success;
            Message = message;
            Data = result;
        }
        public T? Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
