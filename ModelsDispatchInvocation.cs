using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace Envoke
{
    /// <summary>
    /// Configuration options for dispatching HTTP requests from method invocations.
    /// This class provides customizable settings for controlling the behavior of
    /// HTTP requests generated from Castle.DynamicProxy method calls.
    /// </summary>
    public class DispatchInvocationOptions
    {
        /// <summary>
        /// Gets or sets the configuration section prefix for service URLs.
        /// Default: "Endpoints". The service URL will be looked up as "Endpoints:ServiceName".
        /// </summary>
        public string ServiceUrlPrefix { get; set; } = "Endpoints";

        /// <summary>
        /// Gets or sets the timeout for HTTP requests.
        /// Default: 100 seconds.
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(100);

        /// <summary>
        /// Gets or sets additional headers to include in HTTP requests.
        /// These headers will be added to the request in addition to any headers
        /// copied from the current HTTP context.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = null;

        /// <summary>
        /// Gets or sets the JSON serializer options for request/response serialization.
        /// Default: Uses web defaults with camelCase property naming.
        /// </summary>
        public JsonSerializerOptions jsonSerializerOptions { get; set; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Gets or sets whether to throw an exception when the HTTP request fails.
        /// Default: true. When false, errors are captured in the DispatchInvocation.ErrorException property.
        /// </summary>
        public bool ThrowExceptionOnError { get; set; } = true;
    }

    /// <summary>
    /// Contains the complete details of a dispatched HTTP request and its response.
    /// This class is returned by DispatchInvocationAsync and provides comprehensive
    /// information about the HTTP request execution, including timing, success status,
    /// and error details.
    /// </summary>
    public class DispatchInvocation
    {
        /// <summary>
        /// Gets or sets the name of the service that was called (interface name without 'I' prefix).
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the name of the method that was invoked.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the base URL of the service endpoint.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets whether the HTTP request was successful (status code 200-299).
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code returned by the service.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the execution time of the HTTP request in milliseconds.
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during the request, if any.
        /// </summary>
        public Exception ErrorException { get; set; }

        /// <summary>
        /// Gets or sets the details of the HTTP request that was sent.
        /// </summary>
        public DispatchInvocationHttpRequest Request { get; set; } = new DispatchInvocationHttpRequest();

        /// <summary>
        /// Gets or sets the details of the HTTP response that was received.
        /// </summary>
        public DispatchInvocationHttpResponse Response { get; set; } = new DispatchInvocationHttpResponse();
    }

    /// <summary>
    /// Contains the details of an HTTP request that was dispatched.
    /// </summary>
    public class DispatchInvocationHttpRequest
    {
        /// <summary>
        /// Gets or sets the JSON body of the HTTP request.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the headers that were sent with the HTTP request.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Contains the details of an HTTP response that was received.
    /// </summary>
    public class DispatchInvocationHttpResponse
    {
        /// <summary>
        /// Gets or sets the body of the HTTP response.
        /// </summary>
        public string Body { get; set; }
    }
}
