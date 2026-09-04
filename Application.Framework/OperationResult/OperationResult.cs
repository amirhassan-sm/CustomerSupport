using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Framework.OperationResult
{
    public class OperationResult
    {
        public int? RecordId { get; private set; }


        public bool Success { get; private set; } = false;

        public string Message { get; private set; }

        public List<string> Errors { get; private set; } = new List<string>();
        public string? ErrorCode { get; private set; }
        public HttpStatusCode? statusCode { get; private set; }




        public DateTime OperationDate { get; private set; } = DateTime.UtcNow;

        private OperationResult(int? RecordId, string messege, bool success, List<string> Errors, string? errorCode = null
            , HttpStatusCode? statusCode = null)
        {
            this.RecordId = RecordId;
            this.Message = messege;
            this.Success = success;
            this.Errors = Errors;
            this.ErrorCode = errorCode;
            this.statusCode = statusCode;


        }


        public static OperationResult ToSuccess(int RecordId, string messege)
        {
            return new OperationResult(RecordId, messege, true, new List<string>());

        }

        public static OperationResult ToSuccess(string messege)
        {
            return new OperationResult(null, messege, true, new List<string>());

        }

        public static OperationResult ToFail(int RecordId, string messege, List<string> Errors)
        {

            return new OperationResult(RecordId, messege, false, Errors);

        }
        public static OperationResult ToFail(string messege, List<string> Errors)
        {

            return new OperationResult(null, messege, false, Errors);

        }
        public static OperationResult ToFail(string messege, List<string> Errors, string errorCode)
        {

            return new OperationResult(null, messege, false, Errors, errorCode);

        }
        public static OperationResult ToFail(int RecordId, string messege, List<string> Errors, string errorCode)
        {

            return new OperationResult(RecordId, messege, false, Errors, errorCode);

        }
        public static OperationResult ToFail(int RecordId, string messege, List<string> Errors, string errorCode, HttpStatusCode statusCode)
        {
            return new OperationResult(RecordId, messege, false, Errors, errorCode, statusCode);
        }
        public static OperationResult ToFail(string messege, List<string> Errors, string errorCode, HttpStatusCode statusCode)
        {
            return new OperationResult(null, messege, false, Errors, errorCode, statusCode);
        }
    }
}
