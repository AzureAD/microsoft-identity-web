using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Identity.Web
{
    internal class TokenAcquirer : ITokenAcquirer
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly string _authenticationScheme;

        public TokenAcquirer(ITokenAcquisition tokenAcquisition, string authenticationScheme)
        {
            _tokenAcquisition = tokenAcquisition;
            _authenticationScheme = authenticationScheme;
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForUserAsync(
            IEnumerable<string> scopes,
            AcquireTokenOptions? tokenAcquisitionOptions,
            ClaimsPrincipal? user,
            CancellationToken cancellationToken)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(
                scopes,
                tokenAcquisitionOptions?.AuthenticationScheme ?? _authenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                tokenAcquisitionOptions?.UserFlow,
                user,
                (tokenAcquisitionOptions == null) ? null : new TokenAcquisitionOptions()
                {
                    AuthenticationScheme = tokenAcquisitionOptions?.AuthenticationScheme ?? _authenticationScheme,
                    CancellationToken = cancellationToken,
                    Claims = tokenAcquisitionOptions!.Claims,
                    CorrelationId = tokenAcquisitionOptions!.CorrelationId,
                    ExtraQueryParameters = tokenAcquisitionOptions.ExtraQueryParameters,
                    ForceRefresh = tokenAcquisitionOptions.ForceRefresh,
                    LongRunningWebApiSessionKey = tokenAcquisitionOptions.LongRunningWebApiSessionKey,
                    Tenant = tokenAcquisitionOptions.Tenant,
                    UserFlow = tokenAcquisitionOptions.UserFlow,
                    PopPublicKey = tokenAcquisitionOptions.PopPublicKey,
                }).ConfigureAwait(false);

            return new AcquireTokenResult(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId);
        }

        async Task<AcquireTokenResult> ITokenAcquirer.GetTokenForAppAsync(string scope, AcquireTokenOptions? tokenAcquisitionOptions, CancellationToken cancellationToken)
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                scope,
                tokenAcquisitionOptions?.AuthenticationScheme ?? _authenticationScheme,
                tokenAcquisitionOptions?.Tenant,
                (tokenAcquisitionOptions == null) ? null : new TokenAcquisitionOptions()
                {
                    AuthenticationScheme = tokenAcquisitionOptions?.AuthenticationScheme ?? _authenticationScheme,
                    CancellationToken = cancellationToken,
                    Claims = tokenAcquisitionOptions!.Claims,
                    CorrelationId = tokenAcquisitionOptions.CorrelationId,
                    ExtraQueryParameters = tokenAcquisitionOptions.ExtraQueryParameters,
                    ForceRefresh = tokenAcquisitionOptions.ForceRefresh,
                    LongRunningWebApiSessionKey = tokenAcquisitionOptions.LongRunningWebApiSessionKey,
                    Tenant = tokenAcquisitionOptions.Tenant,
                    UserFlow = tokenAcquisitionOptions.UserFlow,
                    PopPublicKey = tokenAcquisitionOptions.PopPublicKey,
                }).ConfigureAwait(false);

            return new AcquireTokenResult(
                result.AccessToken,
                result.ExpiresOn,
                result.TenantId,
                result.IdToken,
                result.Scopes,
                result.CorrelationId);
        }
    }
}
