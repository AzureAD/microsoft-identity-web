// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Net.Http.Headers;

namespace Microsoft.Identity.Web.Sidecar;

public static class CacheControl
{
    private readonly static string s_cacheControlHeader = $"{CacheControlHeaderValue.NoCacheString}, {CacheControlHeaderValue.NoStoreString}, {CacheControlHeaderValue.MustRevalidateString}";


    public static void SetNoCachingMiddleware(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (context.Response.StatusCode is >= 200 and < 300)
                {
                    CacheControl.SetNoCaching(context.Response);
                }
                return Task.CompletedTask;
            });

            await next();
        });
    }

    private static void SetNoCaching(HttpResponse response)
    {
        // using Microsoft.Net.Http.Headers
        response.Headers[HeaderNames.CacheControl] = s_cacheControlHeader;
        response.Headers[HeaderNames.Expires] = "0";
        response.Headers[HeaderNames.Pragma] = CacheControlHeaderValue.NoCacheString;
    }
}
