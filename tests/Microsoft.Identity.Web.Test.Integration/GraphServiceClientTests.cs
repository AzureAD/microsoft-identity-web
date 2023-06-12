// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class GraphSeriveClientTests
    {
        readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        readonly GraphServiceClientOptions _defaultAuthenticationOptions;

        async Task Test()
        {
            GraphServiceClient graphServiceClient = new(new GraphAuthenticationProvider(_authorizationHeaderProvider, new GraphServiceClientOptions()));

            var me = await graphServiceClient.Me.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
                    o.Scopes = new string[] { "user.read" };
                    o.RequestAppToken = true;
                    o.ProtocolScheme = "Pop";
                    o.AcquireTokenOptions.Claims = "claims";
                    o.AcquireTokenOptions.PopPublicKey = "";
                    o.AcquireTokenOptions.CorrelationId = Guid.NewGuid();
                    o.AcquireTokenOptions.UserFlow = "susi";
                    o.AcquireTokenOptions.AuthenticationOptionsName = "JwtBearer";
                    o.AcquireTokenOptions.Tenant = "TenantId";
                });
            }
            );

            var mailFolders = await graphServiceClient.Me.MailFolders.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
                    o.HttpMethod = HttpMethod.Get;

                    // Specify scopes for the request
                    o.Scopes = new string[] { "Mail.Read" };

                    // Specify the ASP.NET Core authentication scheme if needed (in the case
                    // of multiple authentication schemes)
                    // o.AuthenticationOptionsName = JwtBearerDefaults.AuthenticationScheme;
                });
            });

            int? appsInTenant = await graphServiceClient.Applications.Count.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
                    // It's an app permission. Requires an app token
                    o.RequestAppToken = true;
                });
            });

        }
    }
}
