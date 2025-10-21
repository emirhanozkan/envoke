# Envoke

**RESTful API Client Proxy for .NET with Castle.DynamicProxy Integration**

Envoke is a powerful .NET library that enables you to create RESTful API client proxies using Castle.DynamicProxy. It automatically converts method calls into HTTP requests and handles responses seamlessly, making it easy to consume REST APIs as if they were local service interfaces.

## Features

- **Automatic HTTP Request Generation**: Convert method calls to HTTP requests automatically
- **Castle.DynamicProxy Integration**: Use familiar interface-based programming patterns
- **ASP.NET Core Middleware Support**: Built-in middleware for handling incoming requests
- **Parameter Mapping**: Automatic JSON serialization/deserialization of method parameters
- **Request/Response Interception**: Full visibility into HTTP request/response details
- **Error Handling**: Comprehensive error handling with custom exceptions
- **Background Job Support**: Works in both HTTP and background job contexts
- **Configurable Timeouts**: Customizable request timeouts and retry policies
- **Header Management**: Automatic header copying and custom header injection

## Installation

```bash
dotnet add package Envoke
```

## Quick Start

### 1. Define Your Service Interface

```csharp
public interface IUserService
{
    Task<User> CreateUserAsync(string name, string email, int age);
    Task<User> GetUserAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(int id);
}
```

### 2. Configure Services

```csharp
// In your Program.cs or Startup.cs

// Option 1: Generic extension method for any service interfaces
public static void AddServicesAsRestfulApiClient<TBaseInterface>(this IServiceCollection services, 
    Func<Assembly, bool> assemblyFilter = null)
{
    var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
        .Where(assemblyFilter ?? (asm => true)) // Default: all assemblies
        .SelectMany(assembly => assembly.DefinedTypes
            .Where(x => x.IsInterface && 
                       x.AsType() != typeof(TBaseInterface) && 
                       x.GetInterfaces().Any(i => i == typeof(TBaseInterface)))
            .Select(x => x.AsType()))
        .ToList();

    foreach (var serviceType in serviceTypes)
        services.AddTransient(serviceType, provider =>
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new ServiceInterceptor(provider);
            return proxyGenerator.CreateInterfaceProxyWithoutTarget(serviceType, interceptor);
        });
}

// Option 2: Manual registration for specific interfaces
public static void AddRestfulApiClient<TInterface>(this IServiceCollection services)
    where TInterface : class
{
    services.AddTransient<TInterface>(provider =>
    {
        var proxyGenerator = new ProxyGenerator();
        var interceptor = new ServiceInterceptor(provider);
        return proxyGenerator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
    });
}

// Option 3: Register multiple interfaces explicitly
public static void AddRestfulApiClients(this IServiceCollection services, params Type[] serviceTypes)
{
    foreach (var serviceType in serviceTypes)
    {
        services.AddTransient(serviceType, provider =>
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new ServiceInterceptor(provider);
            return proxyGenerator.CreateInterfaceProxyWithoutTarget(serviceType, interceptor);
        });
    }
}

// Usage examples:

// Option 1: Auto-discover all interfaces that inherit from IServiceBase
services.AddServicesAsRestfulApiClient<IServiceBase>(asm => asm.FullName.Contains("MyProject.Services"));

// Option 2: Register individual services
services.AddRestfulApiClient<IUserService>();
services.AddRestfulApiClient<IOrderService>();

// Option 3: Register multiple services at once
services.AddRestfulApiClients(typeof(IUserService), typeof(IOrderService), typeof(IPaymentService));

// Register Envoke services
services.AddEnvoke();
services.AddHttpContextAccessor(); // Required for ASP.NET Core
```

### 3. Create Service Interceptor

```csharp
public class ServiceInterceptor : IInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Intercept(IInvocation invocation)
    {
        var envoke = _serviceProvider.GetRequiredService<IEnvoke>();
        
        // Dispatch the method call as an HTTP request
        var result = envoke.DispatchInvocationAsync(invocation).GetAwaiter().GetResult();
        
        // The return value is automatically set by Envoke
        // based on the method's return type
    }
}
```

### 4. Configure Service URLs

```json
{
  "Endpoints": {
    "UserService": "https://api.example.com",
    "OrderService": "https://orders.example.com",
    "PaymentService": "https://payments.example.com"
  }
}
```

### 5. Use Your Service

```csharp
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // This method call will be automatically converted to:
        // POST https://api.example.com/UserService/CreateUserAsync
        // Body: {"name": "John", "email": "john@example.com", "age": 30}
        var user = await _userService.CreateUserAsync(request.Name, request.Email, request.Age);
        return Ok(user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        // This method call will be automatically converted to:
        // POST https://api.example.com/UserService/GetUserAsync
        // Body: {"id": 123}
        var user = await _userService.GetUserAsync(id);
        return Ok(user);
    }
}
```

## Service Registration Approaches

Envoke provides multiple ways to register your service interfaces, depending on your project structure and preferences:

### Approach 1: Auto-Discovery with Base Interface
**Best for**: Projects with a common base interface for all services

```csharp
// Define a base interface
public interface IServiceBase { }

// Your service interfaces inherit from it
public interface IUserService : IServiceBase { }
public interface IOrderService : IServiceBase { }

// Auto-discover and register all services
services.AddServicesAsRestfulApiClient<IServiceBase>(
    asm => asm.FullName.Contains("MyProject.Services")
);
```

### Approach 2: Individual Registration
**Best for**: Small projects or when you want explicit control

```csharp
services.AddRestfulApiClient<IUserService>();
services.AddRestfulApiClient<IOrderService>();
services.AddRestfulApiClient<IPaymentService>();
```

### Approach 3: Batch Registration
**Best for**: Medium projects with known service sets

```csharp
services.AddRestfulApiClients(
    typeof(IUserService), 
    typeof(IOrderService), 
    typeof(IPaymentService)
);
```

### Approach 4: Custom Assembly Filtering
**Best for**: Complex projects with specific assembly naming conventions

```csharp
// Filter by assembly name patterns
services.AddServicesAsRestfulApiClient<IServiceBase>(
    asm => asm.FullName.Contains("MyProject") && 
           !asm.FullName.Contains("Tests")
);

// Filter by namespace
services.AddServicesAsRestfulApiClient<IServiceBase>(
    asm => asm.GetTypes().Any(t => t.Namespace?.StartsWith("MyProject.Services") == true)
);
```

### Approach 5: Attribute-Based Discovery
**Best for**: Projects using attributes for service identification

```csharp
// Define a marker attribute
[AttributeUsage(AttributeTargets.Interface)]
public class RestfulApiServiceAttribute : Attribute { }

// Apply to your interfaces
[RestfulApiService]
public interface IUserService { }

// Auto-discover by attribute
public static void AddRestfulApiServicesByAttribute(this IServiceCollection services)
{
    var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(asm => asm.DefinedTypes)
        .Where(x => x.IsInterface && x.GetCustomAttribute<RestfulApiServiceAttribute>() != null)
        .Select(x => x.AsType());

    foreach (var serviceType in serviceTypes)
    {
        services.AddTransient(serviceType, provider =>
        {
            var proxyGenerator = new ProxyGenerator();
            var interceptor = new ServiceInterceptor(provider);
            return proxyGenerator.CreateInterfaceProxyWithoutTarget(serviceType, interceptor);
        });
    }
}
```

## Advanced Configuration

### Custom Request Options

```csharp
public class ServiceInterceptor : IInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public void Intercept(IInvocation invocation)
    {
        var envoke = _serviceProvider.GetRequiredService<IEnvoke>();
        
        var options = new DispatchInvocationOptions
        {
            ServiceUrlPrefix = "CustomEndpoints", // Use different config section
            RequestTimeout = TimeSpan.FromSeconds(30),
            ThrowExceptionOnError = false, // Handle errors manually
            Headers = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "CustomValue",
                ["Authorization"] = "Bearer your-token"
            }
        };
        
        var result = envoke.DispatchInvocationAsync(invocation, options).GetAwaiter().GetResult();
        
        if (!result.IsSuccessful)
        {
            // Handle error manually
            throw new ServiceException($"Service call failed: {result.ErrorException?.Message}");
        }
    }
}
```

### ASP.NET Core Middleware Integration

For handling incoming requests that represent service method calls:

```csharp
// In Program.cs
app.UseMiddleware<ServiceMiddleware>();

// Create middleware
public class ServiceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnvoke _envoke;

    public ServiceMiddleware(RequestDelegate next, IEnvoke envoke)
    {
        _next = next;
        _envoke = envoke;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Intercept the request and process it
        var result = await _envoke.InterceptInvocationAsync(context, _next);
        
        // Log or process the result as needed
        Console.WriteLine($"Request processed in {result.ExecutionTime}ms with status {result.StatusCode}");
    }
}
```

### Action Filter for Parameter Mapping

Use the built-in action filter to automatically map JSON parameters to method arguments:

```csharp
[ApiController]
[Route("api/[controller]")]
[EnvokeInvocationFilter] // Apply to entire controller
public class UserController : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(string name, string email, int age)
    {
        // Parameters are automatically mapped from JSON request body
        // JSON: {"name": "John", "email": "john@example.com", "age": 30}
        return Ok($"Created user: {name}, {email}, {age}");
    }
}

// Or apply to specific actions
[HttpPost("update")]
[EnvokeInvocationFilter]
public async Task<IActionResult> UpdateUser(int id, string name, string email)
{
    return Ok($"Updated user {id}: {name}, {email}");
}
```

## Architecture Overview

Envoke follows a three-layer architecture:

### 1. DispatchInvocationAsync (Client Side)
- **Purpose**: Converts method calls into HTTP requests
- **Usage**: Called from Castle.DynamicProxy interceptors
- **Process**:
  - Extracts service name from interface type
  - Gets service URL from configuration
  - Serializes method parameters to JSON
  - Executes HTTP request
  - Deserializes response to return type

### 2. InterceptInvocationAsync (Server Side)
- **Purpose**: Processes incoming HTTP requests in ASP.NET Core pipeline
- **Usage**: Used as middleware (`app.UseMiddleware<ServiceMiddleware>()`)
- **Process**:
  - Reads request body containing method parameters
  - Extracts parameter information from controller metadata
  - Stores parameters in HttpContext.Items for filter processing
  - Measures execution time and captures response

### 3. EnvokeInvocationFilter (Server Side)
- **Purpose**: Maps JSON parameters to method arguments
- **Usage**: Applied as attribute `[EnvokeInvocationFilter]`
- **Process**:
  - Processes JSON requests with application/json content type
  - Deserializes parameters from HttpContext.Items
  - Maps parameters to action method arguments
  - Handles deserialization errors gracefully

## Error Handling

Envoke provides comprehensive error handling:

```csharp
try
{
    var user = await _userService.CreateUserAsync("John", "john@example.com", 30);
}
catch (EnvokeException ex)
{
    // Handle Envoke-specific errors
    Console.WriteLine($"Service call failed: {ex.Message}");
    Console.WriteLine($"HTTP Status: {ex.StatusCode}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
catch (Exception ex)
{
    // Handle other errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Background Job Support

Envoke automatically detects when running in background jobs (no HTTP context) and adjusts behavior accordingly:

```csharp
public class BackgroundService : IHostedService
{
    private readonly IUserService _userService;

    public BackgroundService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // This will work in background jobs
        // Headers from HTTP context won't be copied
        var users = await _userService.GetAllUsersAsync();
    }
}
```

## Configuration Reference

### DispatchInvocationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceUrlPrefix` | `string` | "Endpoints" | Configuration section prefix for service URLs |
| `RequestTimeout` | `TimeSpan` | 100 seconds | HTTP request timeout |
| `Headers` | `Dictionary<string, string>` | `null` | Additional headers to include in requests |
| `jsonSerializerOptions` | `JsonSerializerOptions` | Web defaults with camelCase | JSON serialization options |
| `ThrowExceptionOnError` | `bool` | `true` | Whether to throw exceptions on HTTP errors |

### Configuration Structure

#### Development (Local Docker)
```json
{
  "Endpoints": {
    "UserService": "http://host.docker.internal:7001",
    "OrderService": "http://host.docker.internal:7002", 
    "PaymentService": "http://host.docker.internal:7003"
  }
}
```

#### Production
```json
{
  "Endpoints": {
    "UserService": "https://user-service.example.com:7001",
    "OrderService": "https://order-service.example.com:7001",
    "PaymentService": "https://payment-service.example.com:7001"
  }
}
```

#### Environment-Specific Configuration

You can use different configuration files for different environments:

**appsettings.Development.json** (Local Docker):
```json
{
  "Endpoints": {
    "UserService": "http://host.docker.internal:7001",
    "OrderService": "http://host.docker.internal:7002",
    "PaymentService": "http://host.docker.internal:7003"
  }
}
```

**appsettings.Production.json** (Production):
```json
{
  "Endpoints": {
    "UserService": "https://user-service.example.com:7001",
    "OrderService": "https://order-service.example.com:7001", 
    "PaymentService": "https://payment-service.example.com:7001"
  }
}
```

**appsettings.Staging.json** (Staging):
```json
{
  "Endpoints": {
    "UserService": "https://user-service-staging.example.com:7001",
    "OrderService": "https://order-service-staging.example.com:7001",
    "PaymentService": "https://payment-service-staging.example.com:7001"
  }
}
```

#### Docker Compose Example

For local development with Docker Compose, you can also use environment variables:

**docker-compose.yml**:
```yaml
version: '3.8'
services:
  api-gateway:
    build: .
    environment:
      - Endpoints__UserService=http://user-service:7001
      - Endpoints__OrderService=http://order-service:7001
      - Endpoints__PaymentService=http://payment-service:7001
    depends_on:
      - user-service
      - order-service
      - payment-service
  
  user-service:
    image: user-service:latest
    ports:
      - "7001:7001"
  
  order-service:
    image: order-service:latest
    ports:
      - "7002:7001"
      
  payment-service:
    image: payment-service:latest
    ports:
      - "7003:7001"
```

#### Programmatic Configuration

You can also configure endpoints programmatically based on environment:

```csharp
// In Program.cs or Startup.cs
public static void ConfigureServiceEndpoints(this IServiceCollection services, IConfiguration configuration)
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    
    if (environment == "Development")
    {
        // Local Docker development
        services.Configure<Dictionary<string, string>>("Endpoints", options =>
        {
            options["UserService"] = "http://host.docker.internal:7001";
            options["OrderService"] = "http://host.docker.internal:7002";
            options["PaymentService"] = "http://host.docker.internal:7003";
        });
    }
    else
    {
        // Production or staging - use configuration
        services.Configure<Dictionary<string, string>>("Endpoints", 
            configuration.GetSection("Endpoints").Get<Dictionary<string, string>>());
    }
}
```

## Requirements

- .NET 7.0 or later
- Castle.Core (for Castle.DynamicProxy)
- Microsoft.AspNetCore.Http.Abstractions
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.