using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMKR.Shared.Logging.Models
{
    public class ResponseModel
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public long TotalRecord { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? Summary { get; set; }
        public object Data { get; set; }

        public ResponseModel()
        {
            IsSuccess = true;
            StatusCode = 200;
            Title = "Success";
            Message = string.Empty;
            ExceptionMessage = string.Empty;
            Summary = string.Empty;
            TotalRecord = 0;
            Data = new object();
        }
        public ResponseModel(Exception ex)
        {
            IsSuccess = false;
            StatusCode = 200;
            Title = "Success";
            Message = string.Empty;
            ExceptionMessage = ex.ToString();
            TotalRecord = 0;
            if (ex.InnerException != null)
            {
                Summary = ex.InnerException.ToString();
            }
            Data = new object();
        }
    }
    public class ResponseModel<T>
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public long TotalRecord { get; set; }
        public T? Data { get; set; }

        public ResponseModel(int statusCode, string? exceptionTitle = null, string? exceptionMsg = null, T? data = default, bool success = true, long totalRecord = 0)
        {
            IsSuccess = success;
            StatusCode = statusCode;
            Title = exceptionTitle;
            Message = exceptionMsg;
            Data = data;
            TotalRecord = totalRecord;
        }


    }
}
