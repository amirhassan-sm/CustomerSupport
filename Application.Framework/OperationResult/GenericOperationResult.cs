using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Framework.OperationResult
{
    public class GenericOperationResult<TItems>
    {
        public int? RecordId { get; private set; }


        public bool Success { get; private set; } = false;

        public string Message { get; private set; }
        public List<string> Errors { get; private set; } = new List<string>();
        public string? ErrorCode { get; set; }

        public HttpStatusCode? statusCode { get; set; }



        public TItems? Item { get; private set; } = default(TItems);


        public DateTime OperationDate { get; private set; } = DateTime.UtcNow;

        private GenericOperationResult(int? RecordId, string messege, bool success,
            List<string> Errors, TItems? item = default, string? errorCode = null, HttpStatusCode? statusCode = null)
        {
            this.RecordId = RecordId;
            this.Message = messege;
            this.Success = success;
            this.Errors = Errors;

            this.Item = item;
            this.ErrorCode = errorCode;
            this.ErrorCode = errorCode;
            this.statusCode = statusCode;

        }


        public static GenericOperationResult<TItems> ToSuccess(int RecordId, string messege, TItems items)
        {
            return new GenericOperationResult<TItems>(RecordId, messege, true, new List<string>(), items);

        }
        public static GenericOperationResult<TItems> ToSuccess(string messege, TItems items)
        {
            return new GenericOperationResult<TItems>(null, messege, true, new List<string>(), items);





        }


        public static GenericOperationResult<TItems> ToFail(int RecordId, string messege, List<string> Errors)
        {

            return new GenericOperationResult<TItems>(RecordId, messege, false, Errors);

        }
        public static GenericOperationResult<TItems> ToFail(string messege, List<string> Errors)
        {

            return new GenericOperationResult<TItems>(null, messege, false, Errors);

        }
        public static GenericOperationResult<TItems> ToFail(int RecordId, string messege, List<string> Errors, string? errorCode)
        {

            return new GenericOperationResult<TItems>(RecordId, messege, false, Errors, default, errorCode);

        }
        public static GenericOperationResult<TItems> ToFail(string messege, List<string> Errors, string? errorCode)
        {

            return new GenericOperationResult<TItems>(null, messege, false, Errors, default, errorCode);

        }
        public static GenericOperationResult<TItems>
            ToFail(int RecordId, string messege, List<string> Errors, string? errorCode, HttpStatusCode statusCode)
        {

            return new GenericOperationResult<TItems>(RecordId, messege, false, Errors, default, errorCode, statusCode);

        }
        public static GenericOperationResult<TItems>
          ToFail(string messege, List<string> Errors, string? errorCode, HttpStatusCode statusCode)
        {

            return new GenericOperationResult<TItems>(null, messege, false, Errors, default, errorCode, statusCode);

        }
    }
}
