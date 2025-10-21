using System;
using System.Net;

namespace Envoke
{
    /// <summary>
    /// Custom exception class for Envoke-related errors.
    /// This exception is thrown when HTTP requests fail or other Envoke operations encounter errors.
    /// It provides additional context such as HTTP status codes and custom stack traces.
    /// </summary>
    public sealed class EnvokeException : Exception
    {
        private readonly string _customMessage;
        private readonly string _customStackTrace;
        /// <summary>
        /// Gets the HTTP status code associated with this exception, if applicable.
        /// This property is set when the exception is related to an HTTP request failure.
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the EnvokeException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public EnvokeException(string message)
            : base(message)
        {
            _customMessage = message;
            _customStackTrace = base.StackTrace ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the EnvokeException class with a specified error message and custom stack trace.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="stackTrace">A custom stack trace for the exception</param>
        public EnvokeException(string message, string stackTrace)
            : base(message)
        {
            _customMessage = message;
            _customStackTrace = stackTrace ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the EnvokeException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public EnvokeException(string message, Exception innerException)
            : base(message, innerException)
        {
            _customMessage = message;
            _customStackTrace = base.StackTrace ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the EnvokeException class with a specified error message, inner exception, and HTTP status code.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        /// <param name="statusCode">The HTTP status code associated with this exception</param>
        public EnvokeException(string message, Exception innerException, HttpStatusCode? statusCode)
            : base(message, innerException)
        {
            _customMessage = message;
            _customStackTrace = base.StackTrace ?? string.Empty;
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the custom error message for this exception.
        /// </summary>
        public override string Message => _customMessage;

        /// <summary>
        /// Gets the custom stack trace for this exception.
        /// </summary>
        public override string StackTrace => _customStackTrace;
    }
}
