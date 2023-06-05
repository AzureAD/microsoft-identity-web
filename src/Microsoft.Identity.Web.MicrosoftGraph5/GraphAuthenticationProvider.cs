// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using static Microsoft.Identity.Web.MicrosoftGraph5.GraphAuthenticationProvider;

namespace Microsoft.Identity.Web.MicrosoftGraph5
{
    internal class GraphAuthenticationProvider : IAuthenticationProvider
    {
        IAuthorizationHeaderProvider _authorizationHeaderProvider;
        const string ScopeKey = "scopes";
        private const string AuthorizationHeaderKey = "Authorization";
        private const string ClaimsKey = "claims";
        private const string AuthorizationHeaderProviderOptionsKey = "authorizationHeaderProviderOptions";

        public GraphAuthenticationProvider(IAuthorizationHeaderProvider authorizationHeaderProvider) 
        {
            _authorizationHeaderProvider = authorizationHeaderProvider;
        }

        public async Task AuthenticateRequestAsync(
            RequestInformation request, 
            Dictionary<string, object>? additionalAuthenticationContext = 
            null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string[] scopes = additionalAuthenticationContext?.ContainsKey(ScopeKey) == true ?
                additionalAuthenticationContext[ScopeKey] as string[] :
                Array.Empty<string>();

            AuthorizationHeaderProviderOptions authorizationHeaderProviderOptions= additionalAuthenticationContext?.ContainsKey(AuthorizationHeaderProviderOptionsKey) == true?
                additionalAuthenticationContext[AuthorizationHeaderProviderOptionsKey] as AuthorizationHeaderProviderOptions:
                new AuthorizationHeaderProviderOptions();

            if (additionalAuthenticationContext != null &&
                additionalAuthenticationContext.ContainsKey(ClaimsKey) &&
                request.Headers.ContainsKey(AuthorizationHeaderKey))
                request.Headers.Remove(AuthorizationHeaderKey);

            if (!request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                string authorizationHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                    scopes,
                    authorizationHeaderProviderOptions);
                request.Headers.Add(AuthorizationHeaderKey, authorizationHeader);
            }
        }


        public class AuthorizationOptions : DownstreamApiOptions, IRequestOption
        {
            public AuthorizationOptions(DownstreamApiOptions options) : base(options) { }
            public AuthorizationOptions() { }
        }

        void Test()
        {
            GraphServiceClient graphServiceClient = new GraphServiceClient(new GraphAuthenticationProvider(_authorizationHeaderProvider));
            
            graphServiceClient.Me.GetAsync(r =>
                { 
                    r.Options.WithAuthenticationOptions(o => {
                        o.RequestAppToken = true;
                        o.ProtocolScheme = "Pop";
                        o.AcquireTokenOptions.Claims = "claims";
                        o.AcquireTokenOptions.PopPublicKey "";
                        o.AcquireTokenOptions.CorrelationId = Guid.NewGuid();
                        o.AcquireTokenOptions.UserFlow = "susi";
                        o.AcquireTokenOptions.AuthenticationOptionsName = "Test";
                        o.AcquireTokenOptions.Tenant = "TenantId";
                    }) ; 
                }
            ); 
        }


    }


    public static class XExtension
    {
        public static IList<IRequestOption> WithAuthenticationOptions(this IList<IRequestOption> options, AuthorizationHeaderProviderOptions optionsValue)
        {
            options.Add(new AuthorizationOptions(optionsValue));
            return options;
        }

        public static IList<IRequestOption> WithAuthenticationOptions(this IList<IRequestOption> options, Action<AuthorizationHeaderProviderOptions> optionsValue)
        {
            AuthorizationOptions authorizationOptions = new AuthorizationOptions();
            optionsValue(authorizationOptions);
            options.Add(authorizationOptions);
            return options;
        }
    }
}
