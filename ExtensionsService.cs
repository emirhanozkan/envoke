using Microsoft.Extensions.DependencyInjection;

namespace Envoke;

/// <summary>
/// Extension methods for registering Envoke services with dependency injection.
/// These methods provide convenient ways to configure Envoke in ASP.NET Core applications.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds Envoke services to the dependency injection container.
    /// This method registers the IEnvoke service and its implementation.
    /// 
    /// Note: This method does not register IHttpContextAccessor. If you are using
    /// Envoke in an ASP.NET Core application, you should also call services.AddHttpContextAccessor()
    /// separately, or ensure that the Microsoft.AspNetCore.Http package is referenced.
    /// </summary>
    /// <param name="services">The service collection to add Envoke services to</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddEnvoke(this IServiceCollection services)
    {
        // Note: AddHttpContextAccessor() should be called separately in the consuming application
        // or the consuming application should add the Microsoft.AspNetCore.Http package
        //services.AddHttpContextAccessor();
        services.AddTransient<IEnvoke, Envoke>();
        return services;
    }
}
