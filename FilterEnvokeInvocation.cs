using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Envoke
{
    /// <summary>
    /// ASP.NET Core action filter that processes JSON request bodies and maps parameters to action method arguments.
    /// This filter works in conjunction with Envoke.InterceptInvocationAsync middleware to complete the
    /// request processing pipeline. It:
    /// - Processes incoming JSON requests with application/json content type
    /// - Deserializes JSON parameters stored in HttpContext.Items by the middleware
    /// - Maps the deserialized parameters to the corresponding action method arguments
    /// - Handles JSON deserialization errors gracefully with proper error responses
    /// - Allows non-JSON requests to pass through unchanged
    /// This filter should be applied to controller actions that expect JSON parameter mapping.
    /// </summary>
    public class EnvokeInvocationFilter : IAsyncActionFilter
    {
        /// <summary>
        /// Executes the action filter to process JSON parameters before the action method is called.
        /// </summary>
        /// <param name="context">The action executing context containing the HTTP request and action metadata</param>
        /// <param name="next">The delegate to execute the next filter or action method</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (request.ContentType != null && request.ContentType.Contains("application/json"))
            {
                try
                {
                    foreach (var parameter in context.ActionDescriptor.Parameters)
                    {
                        var parameterName = parameter.Name;

                        foreach (var item in context.HttpContext.Items)
                        {
                            if (parameterName.Equals(item.Key) && item.Value != null)
                            {
                                var parameterType = parameter.ParameterType;
                                var parameterValue = JsonSerializer.Deserialize((string)item.Value, parameterType);
                                context.ActionArguments[parameterName] = parameterValue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result = new BadRequestObjectResult(new { error = "Invalid JSON format", details = ex.Message });
                    return;
                }
            }

            await next();
        }
    }

	/// <summary>
	/// Attribute that applies the EnvokeInvocationFilter to controller classes or action methods.
	/// This attribute can be used to enable JSON parameter mapping for specific controllers or actions.
	/// When applied, it ensures that incoming JSON requests are properly processed and parameters
	/// are mapped to method arguments automatically.
	/// 
	/// Usage examples:
	/// - Apply to entire controller: [EnvokeInvocationFilter] on controller class
	/// - Apply to specific action: [EnvokeInvocationFilter] on action method
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public sealed class EnvokeInvocationFilterAttribute : TypeFilterAttribute
	{
		/// <summary>
		/// Initializes a new instance of the EnvokeInvocationFilterAttribute.
		/// </summary>
		public EnvokeInvocationFilterAttribute() : base(typeof(EnvokeInvocationFilter))
		{
		}
	}
}
