using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Envoke;

/// <summary>
/// Main service for intercepting and executing HTTP requests
/// </summary>
public class Envoke : IEnvoke
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Envoke(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Determines if the current execution is a background job (no HTTP context)
    /// This property checks if there's an active HTTP context available.
    /// When false, it indicates the code is running within an HTTP request pipeline.
    /// When true, it indicates the code is running in a background job or console application.
    /// </summary>
    /// <returns>True if running as background job, false if in HTTP context</returns>
    public bool IsBackgroundJob => _httpContextAccessor.HttpContext == null;

    /// <summary>
    /// Dispatches a method invocation as an HTTP request to a remote service.
    /// This method is typically called from a Castle.DynamicProxy interceptor to convert
    /// method calls into HTTP API requests. It automatically:
    /// - Extracts service name from the interface type (removes 'I' prefix)
    /// - Gets the service URL from configuration using the service name
    /// - Serializes method parameters into JSON request body
    /// - Copies HTTP headers from the current request context (if available)
    /// - Executes the HTTP request with configurable timeout
    /// - Deserializes the response back to the expected return type
    /// - Handles errors and exceptions according to options
    /// </summary>
    /// <param name="invocation">The Castle.DynamicProxy method invocation containing method metadata and arguments</param>
    /// <param name="options">Optional configuration for request behavior including timeout, headers, and error handling</param>
    /// <returns>A DispatchInvocation object containing request/response details, execution time, and success status</returns>
    /// <exception cref="EnvokeException">Thrown when the request fails and ThrowExceptionOnError is true (default)</exception>
    public async Task<DispatchInvocation> DispatchInvocationAsync(IInvocation invocation, DispatchInvocationOptions options = null)
    {
        var _ = new DispatchInvocation();

        _.ServiceName = invocation.Method.DeclaringType!.Name.TrimStart('I');
        _.MethodName = invocation.Method.Name;
        string serviceUrlKey = options != null ? options.ServiceUrlPrefix : "Endpoints";
        _.ServiceUrl = _configuration.GetRequiredSection(string.Join(":", serviceUrlKey, _.ServiceName)).Value;

        // Create body object from method parameters
        var arguments = invocation.Arguments;
        var parameters = invocation.Method.GetParameters();
        var bodyObject = new Dictionary<string, object>();
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramName = parameters[i].Name;
            var argumentValue = arguments[i];

            if (argumentValue != null)
                bodyObject[paramName] = argumentValue;
        }

        _.Request.Body = JsonSerializer.Serialize(bodyObject);


        string resource = string.Join("/", _.ServiceUrl, _.ServiceName, _.MethodName);
        // Create HTTP request
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, resource);
        httpRequest.Content = new StringContent(_.Request.Body, Encoding.UTF8, "application/json");

        if (!IsBackgroundJob)
        {
            foreach (var header in _httpContextAccessor.HttpContext.Request.Headers)
            {
                if (header.Key == "Content-Length")
                    continue;
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                _.Request.Headers.Add(header.Key, header.Value);
            }
        }

        if (options != null && options.Headers != null)
        {
            foreach (var header in options.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                _.Request.Headers.Add(header.Key, header.Value);
            }
        }

        // Create HttpClient with timeout
        using var httpClient = new HttpClient();

        if (options != null)
            httpClient.Timeout = options.RequestTimeout;

        var stopwatch = Stopwatch.StartNew();

        var httpResponse = await httpClient.SendAsync(httpRequest);

        stopwatch.Stop();

        _.ExecutionTime = stopwatch.ElapsedMilliseconds;
        _.IsSuccessful = httpResponse.IsSuccessStatusCode;
        _.StatusCode = httpResponse.StatusCode;
        _.ErrorException = null;


        if (httpResponse.Content != null)
        {
            _.Response.Body = await httpResponse.Content.ReadAsStringAsync();
        }

        if (_.IsSuccessful)
        {
            if (invocation.Method.ReturnType != typeof(void))
            {
                var returnValue = JsonSerializer.Deserialize(_.Response.Body, invocation.Method.ReturnType, options?.jsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

                invocation.ReturnValue = returnValue;
            }
            else
            {
                invocation.ReturnValue = invocation.Method.ReturnType.GetDefaultValue();
            }
        }
        else
        {
            var errorMessage = string.IsNullOrWhiteSpace(_.Response.Body)
                ? (httpResponse.ReasonPhrase ?? "Service call failed")
                : _.Response.Body;
            _.ErrorException = new EnvokeException(errorMessage);
        }

        var shouldThrow = options?.ThrowExceptionOnError ?? true;
        if (shouldThrow && !_.IsSuccessful)
            throw _.ErrorException;

        return _;
    }

    /// <summary>
    /// Intercepts HTTP requests in the ASP.NET Core pipeline and processes them for service method invocation.
    /// This method is designed to be used as middleware in the ASP.NET Core pipeline to handle incoming
    /// HTTP requests that represent service method calls. It:
    /// - Reads the incoming request body containing method parameters as JSON
    /// - Extracts parameter information from the controller action descriptor
    /// - Stores parameter values in HttpContext.Items for later use by EnvokeInvocationFilter
    /// - Captures request headers for logging and debugging
    /// - Measures execution time of the downstream pipeline
    /// - Captures the response body and status code
    /// This method works in conjunction with EnvokeInvocationFilter to complete the request processing.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing the incoming request and response</param>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <returns>An InterceptInvocation object containing request/response details and execution metrics</returns>
    public async Task<InterceptInvocation> InterceptInvocationAsync(HttpContext httpContext, RequestDelegate next)
    {
        var _ = new InterceptInvocation();

        var stopwatch = Stopwatch.StartNew();

        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8))
            _.Request.Body = await reader.ReadToEndAsync();

        JsonElement? jsonElement = !string.IsNullOrEmpty(_.Request.Body) ? JsonSerializer.Deserialize<JsonElement>(_.Request.Body) : null;

        if (jsonElement.HasValue)
        {
            var endpoint = httpContext.GetEndpoint();
            if (endpoint != null)
            {
                var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (actionDescriptor != null)
                {
                    var parameters = actionDescriptor.Parameters;
                    foreach (var parameter in parameters)
                    {
                        var parameterName = parameter.Name;
                        if (jsonElement.Value.TryGetProperty(parameterName, out var property))
                        {
                            var parameterType = parameter.ParameterType;
                            httpContext.Items[parameterName] = property.GetRawText();
                        }
                    }
                }
            }
        }

        _.Request.Headers = httpContext.Request.Headers.ToDictionary(x => x.Key, y => string.Join(", ", y.Value));

        using Stream originalBody = httpContext.Response.Body;
        try
        {
            using var memStream = new MemoryStream();

            httpContext.Response.Body = memStream;

            await next(httpContext);

            memStream.Position = 0;
            _.Response.Body = new StreamReader(memStream).ReadToEnd();

            memStream.Position = 0;
            await memStream.CopyToAsync(originalBody);

        }
        finally
        {
            httpContext.Response.Body = originalBody;
        }

        stopwatch.Stop();

        _.StatusCode = (HttpStatusCode)httpContext.Response.StatusCode;
        _.ExecutionTime = stopwatch.ElapsedMilliseconds;

        return _;
    }
}