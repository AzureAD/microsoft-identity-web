using Microsoft.Identity.Web;

namespace WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthentication()
            .AddMicrosoftIdentityWebApi(builder.Configuration);

        builder.Services.AddAuthorization(options =>
            options.AddPolicy("Auth", policyBuilder => policyBuilder.RequireAuthenticatedUser()));

        var app = builder.Build();

        app.UseDeveloperExceptionPage();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/", () => "Hello World!").RequireAuthorization("Auth");

        app.Run();
    }
}

