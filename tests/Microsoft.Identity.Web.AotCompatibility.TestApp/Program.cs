// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

internal sealed class Program
{
    // The code in this program is expected to be trim and AOT compatible
    private static int Main()
    {
        var builder = WebApplication.CreateSlimBuilder();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApiAot(builder.Configuration.GetSection("AzureAd"), JwtBearerDefaults.AuthenticationScheme,  (o) => {});

        builder.Services.AddTokenAcquisition()
            .AddInMemoryTokenCaches();

        return 0;
    }
}
