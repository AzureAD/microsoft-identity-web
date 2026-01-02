// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web;

namespace MtlsPopSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Learn more about configuring OpenAPI at https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
            builder.Services.AddEndpointsApiExplorer();

            // Add standard JWT Bearer authentication
            builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

            // Add custom MTLS_POP authentication handler
            builder.Services.AddAuthentication()
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, MtlsPopAuthenticationHandler>(
                    MtlsPopAuthenticationHandler.ProtocolScheme,
                    options => {});

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
