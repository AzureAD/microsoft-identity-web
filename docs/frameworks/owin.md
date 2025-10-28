# OWIN Integration with Microsoft.Identity.Web

This guide explains how to use Microsoft.Identity.Web.OWIN package with ASP.NET MVC and Web API applications running on .NET Framework 4.7.2+.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuration](#configuration)
- [Startup Setup](#startup-setup)
- [Controller Integration](#controller-integration)
- [Calling Microsoft Graph](#calling-microsoft-graph)
- [Calling Downstream APIs](#calling-downstream-apis)
- [Sample Applications](#sample-applications)
- [Best Practices](#best-practices)

---

## Overview

The **Microsoft.Identity.Web.OWIN** package brings the power of Microsoft.Identity.Web to ASP.NET MVC and Web API applications using OWIN middleware.

### Why Use Microsoft.Identity.Web.OWIN?

| Feature | Benefit |
|---------|---------|
| **TokenAcquirerFactory** | Automatic token acquisition with caching |
| **Controller Extensions** | Easy access to `GraphServiceClient` and `IDownstreamApi` |
| **Distributed Token Cache** | Built-in support for SQL Server, Redis, Cosmos DB |
| **Automatic Token Refresh** | Handles token refresh transparently |
| **Incremental Consent** | Seamless consent flow integration |

### Supported Scenarios

- ✅ **ASP.NET MVC Web Applications** (.NET Framework 4.7.2+)
- ✅ **ASP.NET Web API** (.NET Framework 4.7.2+)
- ✅ **Hybrid Apps** (MVC + Web API)
- ✅ **Calling Microsoft Graph** from controllers
- ✅ **Calling Downstream APIs** with automatic authentication

---

## Installation

**Package Manager Console:**
```powershell
Install-Package Microsoft.Identity.Web.OWIN
```

**.NET CLI:**
```bash
dotnet add package Microsoft.Identity.Web.OWIN
```

**Dependencies automatically included:**
- Microsoft.Identity.Web.TokenAcquisition
- Microsoft.Identity.Web.TokenCache
- Microsoft.Owin
- System.Web

---

## Configuration

### Web.config

```xml
<configuration>
  <appSettings>
    <!-- Azure AD Configuration -->
    <add key="AzureAd:Instance" value="https://login.microsoftonline.com/" />
    <add key="AzureAd:TenantId" value="your-tenant-id" />
    <add key="AzureAd:ClientId" value="your-client-id" />
    <add key="AzureAd:ClientSecret" value="your-client-secret" />
    <add key="AzureAd:RedirectUri" value="https://localhost:44368/" />
    <add key="AzureAd:PostLogoutRedirectUri" value="https://localhost:44368/" />

    <!-- Microsoft Graph Configuration -->
    <add key="DownstreamApi:MicrosoftGraph:BaseUrl" value="https://graph.microsoft.com/v1.0" />
    <add key="DownstreamApi:MicrosoftGraph:Scopes" value="user.read" />

    <!-- Custom Downstream API Configuration -->
    <add key="DownstreamApi:TodoListService:BaseUrl" value="https://localhost:44351" />
    <add key="DownstreamApi:TodoListService:Scopes" value="api://todo-api-client-id/.default" />
  </appSettings>

  <connectionStrings>
    <!-- Optional: SQL Server Token Cache -->
    <add name="TokenCache"
         connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TokenCache;Integrated Security=True;" />
  </connectionStrings>
</configuration>
```

### appsettings.json (Alternative)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "https://localhost:44368/",
    "PostLogoutRedirectUri": "https://localhost:44368/"
  },
  "DownstreamApi": {
    "MicrosoftGraph": {
      "BaseUrl": "https://graph.microsoft.com/v1.0",
      "Scopes": "user.read"
    },
    "TodoListService": {
      "BaseUrl": "https://localhost:44351",
      "Scopes": "api://todo-api-client-id/.default"
    }
  }
}
```

---

## Startup Setup

### App_Start/Startup.Auth.cs

**Complete setup with Microsoft.Identity.Web.OWIN:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Configuration;
using System.Web;

namespace MyMvcApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            // Set default authentication type
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            // Configure cookie authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieName = "MyApp.Auth",
                ExpireTimeSpan = TimeSpan.FromHours(1),
                SlidingExpiration = true
            });

            // Configure OpenID Connect authentication
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ConfigurationManager.AppSettings["AzureAd:ClientId"],
                    Authority = $"https://login.microsoftonline.com/{ConfigurationManager.AppSettings["AzureAd:TenantId"]}",
                    RedirectUri = ConfigurationManager.AppSettings["AzureAd:RedirectUri"],
                    PostLogoutRedirectUri = ConfigurationManager.AppSettings["AzureAd:PostLogoutRedirectUri"],

                    Scope = "openid profile email offline_access",
                    ResponseType = "code id_token",

                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        NameClaimType = "preferred_username"
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Error?message=" + context.Exception.Message);
                            return Task.FromResult(0);
                        }
                    }
                });

            // Configure Microsoft Identity Web services
            var services = CreateOwinServiceCollection();

            // Add token acquisition
            services.AddTokenAcquisition();

            // Add Microsoft Graph support
            services.AddMicrosoftGraph();

            // Add downstream API support
            services.AddDownstreamApi("MicrosoftGraph", services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>().GetSection("DownstreamApi:MicrosoftGraph"));

            services.AddDownstreamApi("TodoListService", services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>().GetSection("DownstreamApi:TodoListService"));

            // Configure token cache (choose one option)
            ConfigureTokenCache(services);

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create and register token acquirer factory
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Build(serviceProvider);

            // Add OWIN token acquisition middleware
            app.Use<OwinTokenAcquisitionMiddleware>(tokenAcquirerFactory);
        }

        private IServiceCollection CreateOwinServiceCollection()
        {
            var services = new ServiceCollection();

            // Add configuration from appsettings.json and/or Web.config
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AzureAd:Instance"] = ConfigurationManager.AppSettings["AzureAd:Instance"],
                    ["AzureAd:TenantId"] = ConfigurationManager.AppSettings["AzureAd:TenantId"],
                    ["AzureAd:ClientId"] = ConfigurationManager.AppSettings["AzureAd:ClientId"],
                    ["AzureAd:ClientSecret"] = ConfigurationManager.AppSettings["AzureAd:ClientSecret"],
                    ["DownstreamApi:MicrosoftGraph:BaseUrl"] = ConfigurationManager.AppSettings["DownstreamApi:MicrosoftGraph:BaseUrl"],
                    ["DownstreamApi:MicrosoftGraph:Scopes"] = ConfigurationManager.AppSettings["DownstreamApi:MicrosoftGraph:Scopes"],
                })
                .Build();

            services.AddSingleton(configuration);

            return services;
        }

        private void ConfigureTokenCache(IServiceCollection services)
        {
            // Option 1: In-memory cache (development)
            services.AddDistributedTokenCaches(cacheServices =>
            {
                cacheServices.AddDistributedMemoryCache();
            });

            // Option 2: SQL Server cache (production)
            /*
            services.AddDistributedTokenCaches(cacheServices =>
            {
                cacheServices.AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = ConfigurationManager.ConnectionStrings["TokenCache"].ConnectionString;
                    options.SchemaName = "dbo";
                    options.TableName = "TokenCache";
                    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
                });
            });
            */

            // Option 3: Redis cache (production, high-scale)
            /*
            services.AddDistributedTokenCaches(cacheServices =>
            {
                cacheServices.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = ConfigurationManager.AppSettings["Redis:ConnectionString"];
                    options.InstanceName = "MyMvcApp_";
                });
            });
            */
        }
    }
}
```

---

## Controller Integration

### MVC Controllers

**Using controller extension methods:**

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Microsoft.Graph;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MyMvcApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // GET: Home/Index
        public async Task<ActionResult> Index()
        {
            try
            {
                // Access Microsoft Graph using extension method
                var graphClient = this.GetGraphServiceClient();
                var user = await graphClient.Me.GetAsync();

                ViewBag.UserName = user.DisplayName;
                ViewBag.Email = user.Mail ?? user.UserPrincipalName;
                ViewBag.JobTitle = user.JobTitle;

                return View();
            }
            catch (MsalUiRequiredException)
            {
                // Incremental consent required
                return new ChallengeResult();
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = ex.Message });
            }
        }

        // GET: Home/Profile
        public async Task<ActionResult> Profile()
        {
            var graphClient = this.GetGraphServiceClient();

            // Get user profile
            var user = await graphClient.Me
                .GetAsync(requestConfig => requestConfig.QueryParameters.Select = new[] { "displayName", "mail", "jobTitle", "department" });

            return View(user);
        }

        // GET: Home/Photo
        public async Task<ActionResult> Photo()
        {
            var graphClient = this.GetGraphServiceClient();

            try
            {
                // Get user photo
                var photoStream = await graphClient.Me.Photo.Content.GetAsync();
                return File(photoStream, "image/jpeg");
            }
            catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return File(Server.MapPath("~/Content/images/default-user.png"), "image/png");
            }
        }
    }
}
```

### Web API Controllers

**Using ApiController extension methods:**

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;
using System.Web.Http;

namespace MyWebApi.Controllers
{
    [Authorize]
    [RoutePrefix("api/todos")]
    public class TodoController : ApiController
    {
        // GET: api/todos
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetTodos()
        {
            try
            {
                // Call downstream API using extension method
                var downstreamApi = this.GetDownstreamApi();

                var todos = await downstreamApi.GetForUserAsync<List<TodoItem>>(
                    "TodoListService",
                    options =>
                    {
                        options.RelativePath = "api/todolist";
                    });

                return Ok(todos);
            }
            catch (MsalUiRequiredException)
            {
                return Unauthorized();
            }
            catch (HttpRequestException ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/todos
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateTodo([FromBody] TodoItem todo)
        {
            var downstreamApi = this.GetDownstreamApi();

            var createdTodo = await downstreamApi.PostForUserAsync<TodoItem, TodoItem>(
                "TodoListService",
                todo,
                options =>
                {
                    options.RelativePath = "api/todolist";
                });

            return Created($"api/todos/{createdTodo.Id}", createdTodo);
        }
    }
}
```

---

## Calling Microsoft Graph

### Setup Microsoft Graph Client

**Already configured in Startup.Auth.cs:**
```csharp
services.AddMicrosoftGraph();
```

### Using GraphServiceClient in Controllers

```csharp
[Authorize]
public class GraphController : Controller
{
    public async Task<ActionResult> MyProfile()
    {
        var graphClient = this.GetGraphServiceClient();
        var user = await graphClient.Me.GetAsync();

        return View(user);
    }

    public async Task<ActionResult> MyManager()
    {
        var graphClient = this.GetGraphServiceClient();
        var manager = await graphClient.Me.Manager.GetAsync();

        return View(manager);
    }

    public async Task<ActionResult> MyDirectReports()
    {
        var graphClient = this.GetGraphServiceClient();
        var directReports = await graphClient.Me.DirectReports.GetAsync();

        return View(directReports.Value);
    }

    public async Task<ActionResult> SendEmail([FromBody] EmailMessage message)
    {
        var graphClient = this.GetGraphServiceClient();

        var email = new Message
        {
            Subject = message.Subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = message.Body
            },
            ToRecipients = new[]
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = message.To
                    }
                }
            }
        };

        await graphClient.Me.SendMail.PostAsync(new SendMailPostRequestBody
        {
            Message = email
        });

        return RedirectToAction("Index");
    }
}
```

---

## Calling Downstream APIs

### Configure Downstream API

**In Startup.Auth.cs:**
```csharp
services.AddDownstreamApi("TodoListService", configuration.GetSection("DownstreamApi:TodoListService"));
```

**In Web.config:**
```xml
<add key="DownstreamApi:TodoListService:BaseUrl" value="https://localhost:44351" />
<add key="DownstreamApi:TodoListService:Scopes" value="api://todo-api-client-id/.default" />
```

### Using IDownstreamApi in Controllers

```csharp
[Authorize]
public class TodoController : Controller
{
    // GET all todos
    public async Task<ActionResult> Index()
    {
        var downstreamApi = this.GetDownstreamApi();

        var todos = await downstreamApi.GetForUserAsync<List<TodoItem>>(
            "TodoListService",
            options =>
            {
                options.RelativePath = "api/todolist";
            });

        return View(todos);
    }

    // GET specific todo
    public async Task<ActionResult> Details(int id)
    {
        var downstreamApi = this.GetDownstreamApi();

        var todo = await downstreamApi.GetForUserAsync<TodoItem>(
            "TodoListService",
            options =>
            {
                options.RelativePath = $"api/todolist/{id}";
            });

        return View(todo);
    }

    // POST new todo
    [HttpPost]
    public async Task<ActionResult> Create(TodoItem todo)
    {
        var downstreamApi = this.GetDownstreamApi();

        var createdTodo = await downstreamApi.PostForUserAsync<TodoItem, TodoItem>(
            "TodoListService",
            todo,
            options =>
            {
                options.RelativePath = "api/todolist";
            });

        return RedirectToAction("Index");
    }

    // PUT update todo
    [HttpPost]
    public async Task<ActionResult> Edit(int id, TodoItem todo)
    {
        var downstreamApi = this.GetDownstreamApi();

        await downstreamApi.CallApiForUserAsync(
            "TodoListService",
            options =>
            {
                options.HttpMethod = HttpMethod.Put;
                options.RelativePath = $"api/todolist/{id}";
                options.RequestBody = todo;
            });

        return RedirectToAction("Index");
    }

    // DELETE todo
    [HttpPost]
    public async Task<ActionResult> Delete(int id)
    {
        var downstreamApi = this.GetDownstreamApi();

        await downstreamApi.CallApiForUserAsync(
            "TodoListService",
            options =>
            {
                options.HttpMethod = HttpMethod.Delete;
                options.RelativePath = $"api/todolist/{id}";
            });

        return RedirectToAction("Index");
    }
}
```

---

## Sample Applications

### Official Microsoft Samples

| Sample | Description |
|--------|-------------|
| [ms-identity-aspnet-webapp-openidconnect](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect) | ASP.NET MVC app with Microsoft.Identity.Web.OWIN |
| Key Files | `App_Start/Startup.Auth.cs`, `Controllers/HomeController.cs` |

**Quick start:**
```bash
git clone https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect
cd ms-identity-aspnet-webapp-openidconnect
# Update Web.config with your Azure AD app registration
# Run in Visual Studio
```

---

## Best Practices

### ✅ Do's

**1. Use distributed cache in production:**
```csharp
// ✅ Production
services.AddDistributedTokenCaches(cacheServices =>
{
    cacheServices.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = ConfigurationManager.ConnectionStrings["TokenCache"].ConnectionString;
        options.SchemaName = "dbo";
        options.TableName = "TokenCache";
        options.DefaultSlidingExpiration = TimeSpan.FromMinutes(90);
    });
});
```

**2. Handle incremental consent gracefully:**
```csharp
try
{
    var graphClient = this.GetGraphServiceClient();
    var user = await graphClient.Me.GetAsync();
}
catch (MsalUiRequiredException)
{
    // User needs to consent to additional scopes
    return new ChallengeResult();
}
```

**3. Use correlation IDs for troubleshooting:**
```csharp
var downstreamApi = this.GetDownstreamApi();
var correlationId = Guid.NewGuid();

var result = await downstreamApi.GetForUserAsync<Todo>(
    "TodoListService",
    options =>
    {
        options.RelativePath = $"api/todolist/{id}";
        options.TokenAcquisitionOptions = new TokenAcquisitionOptions
        {
            CorrelationId = correlationId
        };
    });
```

**4. Implement proper error handling:**
```csharp
try
{
    // Call API
}
catch (MsalUiRequiredException)
{
    return new ChallengeResult();
}
catch (HttpRequestException ex)
{
    logger.Error($"API call failed: {ex.Message}");
    return View("Error");
}
```

### ❌ Don'ts

**1. Don't use in-memory cache for web farms:**
```csharp
// ❌ Wrong for load-balanced scenarios
services.AddDistributedTokenCaches(cacheServices =>
{
    cacheServices.AddDistributedMemoryCache();
});

// ✅ Correct
services.AddDistributedTokenCaches(cacheServices =>
{
    cacheServices.AddDistributedSqlServerCache(/* ... */);
});
```

**2. Don't hardcode configuration:**
```csharp
// ❌ Wrong
ClientId = "your-client-id-here"

// ✅ Correct
ClientId = ConfigurationManager.AppSettings["AzureAd:ClientId"]
```

**3. Don't ignore token expiration:**
```csharp
// ✅ Microsoft.Identity.Web.OWIN handles this automatically
// No manual token refresh needed!
```

---

## Troubleshooting

### Common Issues

**Issue 1: "Cannot find IAuthorizationHeaderProvider"**

**Solution:** Ensure `OwinTokenAcquirerFactory` is registered in `Startup.Auth.cs`:
```csharp
var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
tokenAcquirerFactory.Build(serviceProvider);
app.Use<OwinTokenAcquisitionMiddleware>(tokenAcquirerFactory);
```

**Issue 2: "Cannot find GraphServiceClient"**

**Solution:** Add `AddMicrosoftGraph()` in `Startup.Auth.cs`:
```csharp
services.AddMicrosoftGraph();
```

**Issue 3: Token cache not persisting**

**Solution:** Verify distributed cache configuration:
```csharp
services.AddDistributedTokenCaches(cacheServices =>
{
    cacheServices.AddDistributedSqlServerCache(options =>
    {
        // Ensure connection string is correct
        options.ConnectionString = ConfigurationManager.ConnectionStrings["TokenCache"].ConnectionString;
    });
});
```

---

## Additional Resources

- [Microsoft.Identity.Web.OWIN on GitHub](https://github.com/AzureAD/microsoft-identity-web)
- [OWIN Integration Wiki](https://github.com/AzureAD/microsoft-identity-web/wiki/OWIN)
- [Sample: ASP.NET MVC with OWIN](https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect)
- [Token Cache Serialization](token-cache-serialization.md)

---

**Last Updated:** October 27, 2025
**Microsoft.Identity.Web Version:** 3.14.1+
**Supported Frameworks:** ASP.NET MVC, ASP.NET Web API (.NET Framework 4.7.2+)
