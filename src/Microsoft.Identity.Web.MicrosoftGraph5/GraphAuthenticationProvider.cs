// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication provider for Microsoft Graph, based on IAuthorizationHeaderProvider. This is richer
    /// than <see cref="BaseBearerTokenAuthenticationProvider"/> which only support the bearer protocol.
    /// </summary>
    internal class GraphAuthenticationProvider : IAuthenticationProvider
    {
        const string ScopeKey = "scopes";
        private const string AuthorizationHeaderKey = "Authorization";
        private const string ClaimsKey = "claims";
        private const string AuthorizationHeaderProviderOptionsKey = "authorizationHeaderProviderOptions";

        IAuthorizationHeaderProvider _authorizationHeaderProvider;
        MicrosoftGraphOptions _defaultAuthenticationOptions;

        /// <summary>
        /// Constructor from the authorization header provider.
        /// </summary>
        /// <param name="authorizationHeaderProvider"></param>
        /// <param name="defaultAuthenticationOptions"></param>
        public GraphAuthenticationProvider(IAuthorizationHeaderProvider authorizationHeaderProvider,
            MicrosoftGraphOptions defaultAuthenticationOptions)
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _defaultAuthenticationOptions = defaultAuthenticationOptions;
        }

        /// <summary>
        /// Method that performs the authentication, and adds the right headers to the request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="additionalAuthenticationContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object>? additionalAuthenticationContext =
            null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Attempts to get the scopes
            string[] scopes = additionalAuthenticationContext is not null && additionalAuthenticationContext.ContainsKey(ScopeKey) ?
                   (string[])additionalAuthenticationContext[ScopeKey] : null;
            if (scopes == null)
            {
                scopes = _defaultAuthenticationOptions.Scopes.ToArray();
            }

            // Attempts to get the authorization header provider options
            MicrosoftGraphOptions authorizationHeaderProviderOptions = additionalAuthenticationContext is not null &&
                 additionalAuthenticationContext.ContainsKey(AuthorizationHeaderProviderOptionsKey) ?
                 (MicrosoftGraphOptions)additionalAuthenticationContext[AuthorizationHeaderProviderOptionsKey] :
                 new MicrosoftGraphOptions();

            if (additionalAuthenticationContext != null &&
                additionalAuthenticationContext.ContainsKey(ClaimsKey) &&
                request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                request.Headers.Remove(AuthorizationHeaderKey);
            }

            bool appToken = false;
            if (!request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                string authorizationHeader;

                if (appToken)
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(scopes.FirstOrDefault(),
                        authorizationHeaderProviderOptions);
                }
                else
                {
                    authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                         scopes,
                         authorizationHeaderProviderOptions);
                }
                request.Headers.Add(AuthorizationHeaderKey, authorizationHeader);
            }
        }



        async Task Test()
        {
            GraphServiceClient graphServiceClient = new GraphServiceClient(new GraphAuthenticationProvider(_authorizationHeaderProvider, new MicrosoftGraphOptions()));

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
