# Add authentication to an existing web app

1. Add an `"AzureAd"` section in the appsettings.json:

   ```json
   {
    "AzureAd" :
    {
     "Instance" : "https://login.microsoftonline.com",
     "TenantId" : "GUID"
     "ClientId" : "your application ID from the Entra ID app registration"
    }
   }
   ```

1. Add the nuget Microsoft.Identity.Web NuGet package
1. Add the following usings at the top of the file

   ```csharp
   using Microsoft.AspNetCore.Authentication.OpenIdConnect;
   using Microsoft.Identity.Web;
   ```

1. In the Program.cs file, after `var app = builder.Build();`, add:
   ```csharp
   builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
          .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
   ```

1. Replace `services.AddRazorPages()` by:

   ```csharp
     services.AddRazorPages().AddMvcOptions(options =>
     {
      var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
     }).AddMicrosoftIdentityUI();
    }
   ```

1. After `app.UseAuthentication()`, use:

   ```csharp
     app.UseAuthentication();
     app.UseAuthorization();

     // More code
     app.UseEndpoints(endpoints =>
     {
      endpoints.MapRazorPages();  // If Razor pages
      endpoints.MapControllers(); // Needs to be added
     });
    }
   ```

1. In the controllers, add an `[Authorize]` attribute.
