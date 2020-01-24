using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace bootCommon
{

    public class ExceptionWithCode : Exception
    {
        readonly HttpStatusCode _code;
        readonly Dictionary<String, String> _additionalInformation;
        readonly string _reason;
        public ExceptionWithCode(String message, 
            HttpStatusCode code = HttpStatusCode.BadRequest,
            string reason = "",
            Exception innerException = null, Dictionary<String, String> additionalInformation = null)
            : base(message, innerException)
        {
            _code = code;
            _reason = reason;
            _additionalInformation = additionalInformation;
        }

        public HttpStatusCode errCode { get { return _code; } }

        public string Reason { get { return _reason; } }

        public Dictionary<String, String> additionalInformation { get { return _additionalInformation; } }
    }

    public class ErrorMessage
    {

        readonly string _message;
        readonly ErrorMessage _innerError = null;
		readonly Exception _exception = null;
		readonly Guid _errorId;
		readonly HttpStatusCode _httpStatus = HttpStatusCode.InternalServerError;

		public ErrorMessage(String message = "Something went wrong")
        {
            _message = message;
			_errorId = Guid.NewGuid();

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="onlyTopLevel">if true ony log top level exception</param>
        public ErrorMessage(Exception ex, bool onlyTopLevel = true)
            : this(ex.Message)
        {
			_exception = ex;

			var witcode = _exception as ExceptionWithCode;
			if (null != witcode)
			{
				Reason = witcode.Reason;
				additionalInformation = witcode.additionalInformation;
				_httpStatus = witcode.errCode;
			}

			if (onlyTopLevel)
			{
				if (null == witcode)
				{
					_message = $"The server encountered an internal error, please try again later (Error ID: {_errorId})";
				}

			}
			else
			{
				if (null != ex.InnerException)
					_innerError = new ErrorMessage(ex.InnerException);
			}

		}

		[JsonIgnore]
		public HttpStatusCode statusCode { get { return _httpStatus; } } 

		public Guid errorId { get { return _errorId; } set { } }

		[JsonIgnore]
		public Exception theException { get { return _exception; } }

        /// <summary>
        /// Detailed information abut what went wrong
        /// </summary>
        public String Message { get { return _message; } set { } }

        //[JsonConverter(typeof(StringEnumConverter))]
        //public FailureReason? Reason { get; set; }

        public Dictionary<String, String> additionalInformation { get; set; }

        public ErrorMessage innerError { get { return _innerError; } set { } }

        public string Reason { get; set; }

		public static ErrorMessage userDomainErrorMessageFromExcption(Exception exception, ILogger logger)
		{
			//handle known excpetions
			if (exception is System.AggregateException && null != exception.InnerException)
			{
				exception = exception.InnerException;
			}

            /*
			if (exception is commonInterfaces.ConflictException)
			{
				exception = new ExceptionWithCode(message: "Midair collission. Someone else changed your document while you were saving it.", innerException: exception);
			}
            */

			var error = new ErrorMessage(exception);

			logger.LogError(error.theException ?? new Exception("unknown error"), $"Error Id:{error.errorId}");

			return error;

		}

		public static ErrorMessage SetStatusGetResult(HttpContext httpContext, Exception exception, ILogger logger)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			var error = userDomainErrorMessageFromExcption(exception, logger);
			httpContext.Response.StatusCode =(int) error.statusCode;
			return error;

			
        }

    }

	
}
