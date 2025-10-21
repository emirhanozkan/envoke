using System.Collections.Generic;
using System.Net;

namespace Envoke
{
    /// <summary>
    /// Contains the complete details of an intercepted HTTP request and its response.
    /// This class is returned by InterceptInvocationAsync and provides comprehensive
    /// information about the HTTP request processing in the ASP.NET Core pipeline,
    /// including timing, status code, and request/response details.
    /// </summary>
    public class InterceptInvocation
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the execution time of the request processing in milliseconds.
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the details of the HTTP request that was intercepted.
        /// </summary>
        public InterceptInvocationHttpRequest Request { get; set; } = new InterceptInvocationHttpRequest();

        /// <summary>
        /// Gets or sets the details of the HTTP response that was generated.
        /// </summary>
        public InterceptInvocationHttpResponse Response { get; set; } = new InterceptInvocationHttpResponse();
    }

    /// <summary>
    /// Contains the details of an HTTP request that was intercepted.
    /// </summary>
    public class InterceptInvocationHttpRequest
    {
        /// <summary>
        /// Gets or sets the body of the HTTP request.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the headers of the HTTP request.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Contains the details of an HTTP response that was generated.
    /// </summary>
    public class InterceptInvocationHttpResponse
    {
        /// <summary>
        /// Gets or sets the body of the HTTP response.
        /// </summary>
        public string Body { get; set; }
    }
}
