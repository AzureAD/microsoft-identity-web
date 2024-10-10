// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{

    /// <summary>
    /// This is a compilation test only. It is not meant to be run.
    /// </summary>
    public class GraphServiceClientTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS0649 // Field 'GraphServiceClientTests._authorizationHeaderProvider' is never assigned to, and will always have its default value null
        readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
#pragma warning restore CS0649 // Field 'GraphServiceClientTests._authorizationHeaderProvider' is never assigned to, and will always have its default value null
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable IDE0051 // Remove unused private members
        async Task TestAsync()
#pragma warning restore IDE0051 // Remove unused private members
        {
            GraphServiceClient graphServiceClient = new(new GraphAuthenticationProvider(_authorizationHeaderProvider, new GraphServiceClientOptions()));

            User? me = await graphServiceClient.Me.GetAsync(r =>
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

            MailFolderCollectionResponse? mailFolders = await graphServiceClient.Me.MailFolders.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
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

        [Fact]
        public async Task AuthenticateRequestAsync_NonGraphUri_DoesNotSetAuthZHeaderAsync()
        {
            // arrange
            RequestInformation request = new()
            {
                URI = new Uri("http://www.contoso.com/")
            };

            GraphAuthenticationProvider graphAuthenticationProvider = new(_authorizationHeaderProvider, new GraphServiceClientOptions());

            // act
            await graphAuthenticationProvider.AuthenticateRequestAsync(request);

            // assert
            Assert.False(request.Headers.ContainsKey("Authorization"));
        }
    }
}
