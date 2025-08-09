// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
	/// <summary>
	/// Azure SDK token credential for tokens based on the <see cref="IAuthorizationHeaderProvider"/>
    /// service.
	/// </summary>
	public class MicrosoftIdentityTokenCredential : TokenCredential
	{
		private readonly ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly Abstractions.IAuthenticationSchemeInformationProvider _authenticationSchemeInformationProvider;

        /// <summary>
        /// Constructor from an ITokenAcquisition service.
        /// </summary>
        /// <param name="tokenAcquirerFactory">Token acquisition factory</param>
        /// <param name="authenticationSchemeInformationProvider">Host for the token acquisition</param>
        public MicrosoftIdentityTokenCredential(ITokenAcquirerFactory tokenAcquirerFactory, Abstractions.IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider)
		{
			_tokenAcquirerFactory = tokenAcquirerFactory ?? throw new System.ArgumentNullException(nameof(tokenAcquirerFactory));
            _authenticationSchemeInformationProvider = authenticationSchemeInformationProvider ?? throw new System.ArgumentNullException(nameof(authenticationSchemeInformationProvider));
        }

        /// <summary>
        /// Options used to configure the token acquisition behavior.
        /// </summary>
		public AuthorizationHeaderProviderOptions Options { get; } = new();

		/// <inheritdoc/>
		public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
		{
			return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
		}

		/// <inheritdoc/>
		public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
		{
            ITokenAcquirer tokenAcquirer = _tokenAcquirerFactory.GetTokenAcquirer(_authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(Options.AcquireTokenOptions.AuthenticationOptionsName));
            AcquireTokenResult result;
            if (Options.RequestAppToken)
            {
                result = await tokenAcquirer.GetTokenForAppAsync(requestContext.Scopes.First(), Options.AcquireTokenOptions, cancellationToken: cancellationToken);
            }
            else
            {
                result = await tokenAcquirer.GetTokenForUserAsync(requestContext.Scopes, Options.AcquireTokenOptions, cancellationToken: cancellationToken);
            }
            return new AccessToken(result.AccessToken!, result.ExpiresOn);
        }
	}
}
