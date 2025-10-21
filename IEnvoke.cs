using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Envoke
{
    /// <summary>
    /// Main interface for Envoke HTTP request interception and dispatching.
    /// This interface defines the contract for converting method invocations into HTTP requests
    /// and intercepting HTTP requests in the ASP.NET Core pipeline.
    /// </summary>
    public interface IEnvoke
    {
        /// <summary>
        /// Gets a value indicating whether the current execution is running as a background job.
        /// Returns true when there is no active HTTP context (e.g., in background services or console applications).
        /// Returns false when running within an HTTP request pipeline.
        /// </summary>
        bool IsBackgroundJob { get; }
        /// <summary>
        /// Dispatches a method invocation as an HTTP request to a remote service.
        /// This method converts Castle.DynamicProxy method calls into HTTP API requests.
        /// </summary>
        /// <param name="invocation">The method invocation to dispatch</param>
        /// <param name="options">Optional configuration for the request</param>
        /// <returns>A task that represents the asynchronous operation and contains dispatch details</returns>
        Task<DispatchInvocation> DispatchInvocationAsync(IInvocation invocation, DispatchInvocationOptions options = null);
        /// <summary>
        /// Intercepts HTTP requests in the ASP.NET Core pipeline for service method invocation.
        /// This method processes incoming HTTP requests and prepares them for parameter mapping.
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the request and response</param>
        /// <param name="next">The next middleware in the pipeline</param>
        /// <returns>A task that represents the asynchronous operation and contains interception details</returns>
        Task<InterceptInvocation> InterceptInvocationAsync(HttpContext httpContext, RequestDelegate next);
    }
}
