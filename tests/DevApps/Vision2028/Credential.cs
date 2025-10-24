// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Vision2028
{
    public class CredentialOptions : MicrosoftEntraApplicationOptions
    {
        public CredentialOptions()
        {
            Instance = "https://login.microsoftonline.com/";
            TenantId = "organizations";
            ClientCredentials = new CredentialDescription[]
            {
                // Managed certificates
                new CredentialDescription()
                {
                     SourceType = CredentialSource.ManagedCertificate
                },

                // FIC + Machine assigned Managed identity
                new CredentialDescription()
                {
                     SourceType = CredentialSource.SignedAssertionFromManagedIdentity
                },

                // Dummy secret (for creation of the MSAL object ... for the moment)
                new CredentialDescription()
                {
                     SourceType = CredentialSource.ClientSecret
                },


            };
            SendX5C = true;
        }
    }

    public class Mise
    {
        protected IServiceProvider? _serviceProvider;
        protected IServiceCollection _services;

        public Mise()
        {
            _services = new ServiceCollection();
            _services.AddTokenAcquisition(true);
            _services.AddInMemoryTokenCaches();
            _services.AddHttpClient();
            _services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
            });
        }

        public Credential NewCredential(CredentialOptions? credentialOptions = null)
        {
            return new Credential(credentialOptions ?? new CredentialOptions(), this);
        }

        public Credential NewCredential(Credential credential, CredentialOptions? credentialOptions)
        {
            Credential newCredential = new Credential(credentialOptions ?? new CredentialOptions(), this);
            newCredential.credentialToken = credential.credentialToken;
            if (!string.IsNullOrEmpty(newCredential.credentialToken))
            {
                newCredential.CredentialOptions.ClientCredentials = new CredentialDescription[0];
            }
            else
            {

            }
            return newCredential;
        }

        /// <summary>
        /// Echanges a credentials against another
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="acquireTokenOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Credential> ExchangeCredentialAsync(
            Credential credential,
            AcquireTokenOptions? acquireTokenOptions = null,
            CancellationToken cancellationToken = default)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = _services.BuildServiceProvider();
            }

            // Get the services from the service provider.
            ITokenAcquirerFactory tokenAcquirerFactory = _serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
            var authenticationSchemeInformationProvider = _serviceProvider.GetRequiredService<Microsoft.Identity.Abstractions.IAuthenticationSchemeInformationProvider>();
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> optionsMonitor =
                _serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            // Get the FIC token.
            string authenticationScheme = authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(acquireTokenOptions?.AuthenticationOptionsName);
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationScheme);

            AcquireTokenOptions? tokenAcquisitionOptions;
            if (credential.credentialToken != null)
            {
                tokenAcquisitionOptions = acquireTokenOptions != null ? acquireTokenOptions.Clone() : new AcquireTokenOptions();
                tokenAcquisitionOptions = tokenAcquisitionOptions.WithClientAssertion(credential.credentialToken);
            }
            else
            {
                tokenAcquisitionOptions = acquireTokenOptions?.Clone() ?? new AcquireTokenOptions();
            }
            tokenAcquisitionOptions.ExtraParameters ??= new Dictionary<string, object>();
            tokenAcquisitionOptions.ExtraParameters["IDWEB_FMI_MICROSOFT_IDENTITY_OPTIONS" /*Constants.MicrosoftIdentityOptionsParameter*/] = credential.CredentialOptions;


            // TODO: this is hardcoded for public cloud, we need to get it from the cloud instance.
            string scope = "api://AzureAdTokenExchange/.default";

            var token = await tokenAcquirer.GetTokenForAppAsync(scope, tokenAcquisitionOptions, cancellationToken);
            Credential newCredential = new Credential(token.AccessToken, null, credential.mise);
            newCredential.CredentialOptions = credential.CredentialOptions;
            return newCredential;
        }

        public async Task<AuthorizationHeaderInformation> GetAuthorizationHeaderAsync(Credential credential, DownstreamApiOptions options,
            CancellationToken cancellationToken = default)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = _services.BuildServiceProvider();
            }

            // Get the services from the service provider.
            ITokenAcquirerFactory tokenAcquirerFactory = _serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
            var authenticationSchemeInformationProvider = _serviceProvider.GetRequiredService<Microsoft.Identity.Abstractions.IAuthenticationSchemeInformationProvider>();
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> optionsMonitor =
                _serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            // Get the FIC token.
            IAuthorizationHeaderProvider authorizationHeaderProvider = _serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            DownstreamApiOptions downstreamApiOptions = options.Clone();
            AcquireTokenOptions? acquireTokenOptions = options?.AcquireTokenOptions;
            string authenticationScheme = authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(acquireTokenOptions?.AuthenticationOptionsName);

            AcquireTokenOptions? tokenAcquisitionOptions;
            if (credential.credentialToken != null)
            {
                tokenAcquisitionOptions = acquireTokenOptions != null ? acquireTokenOptions.Clone() : new AcquireTokenOptions();
                tokenAcquisitionOptions = tokenAcquisitionOptions.WithClientAssertion(credential.credentialToken);
            }
            else
            {
                tokenAcquisitionOptions = acquireTokenOptions?.Clone() ?? new AcquireTokenOptions();
            }
            tokenAcquisitionOptions.ExtraParameters ??= new Dictionary<string, object>();
            tokenAcquisitionOptions.ExtraParameters["IDWEB_FMI_MICROSOFT_IDENTITY_OPTIONS" /*Constants.MicrosoftIdentityOptionsParameter*/] = credential.CredentialOptions;
            downstreamApiOptions.AcquireTokenOptions = tokenAcquisitionOptions;

            string authorizationHeader;

            try
            {
                authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(downstreamApiOptions.Scopes.FirstOrDefault(), downstreamApiOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                throw;
            }
            return new AuthorizationHeaderInformation() {  AuthorizationHeaderValue = authorizationHeader};
        }

        public async Task<AuthorizationHeaderInformation> GetAuthorizationHeaderAsync(Credential credential, Action<DownstreamApiOptions> downstreamApiOptions,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public async Task<string> GetToken(Credential credential, DownstreamApiOptions options, CancellationToken cancellationToken = default)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = _services.BuildServiceProvider();
            }

            // Get the services from the service provider.
            ITokenAcquirerFactory tokenAcquirerFactory = _serviceProvider.GetRequiredService<ITokenAcquirerFactory>();
            var authenticationSchemeInformationProvider = _serviceProvider.GetRequiredService<Microsoft.Identity.Abstractions.IAuthenticationSchemeInformationProvider>();
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> optionsMonitor =
                _serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>();

            // Get the FIC token.
            AcquireTokenOptions? acquireTokenOptions = options?.AcquireTokenOptions;
            string authenticationScheme = authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(acquireTokenOptions?.AuthenticationOptionsName);
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(authenticationScheme);

            AcquireTokenOptions? tokenAcquisitionOptions;
            if (credential.credentialToken != null)
            {
                tokenAcquisitionOptions = acquireTokenOptions != null ? acquireTokenOptions.Clone() : new AcquireTokenOptions();
                tokenAcquisitionOptions = tokenAcquisitionOptions.WithClientAssertion(credential.credentialToken);
            }
            else
            {
                tokenAcquisitionOptions = acquireTokenOptions?.Clone() ?? new AcquireTokenOptions();
            }
            tokenAcquisitionOptions.ExtraParameters ??= new Dictionary<string, object>();
            tokenAcquisitionOptions.ExtraParameters["IDWEB_FMI_MICROSOFT_IDENTITY_OPTIONS" /*Constants.MicrosoftIdentityOptionsParameter*/] = credential.CredentialOptions;

            var token = await tokenAcquirer.GetTokenForAppAsync(options.Scopes.FirstOrDefault(), tokenAcquisitionOptions, cancellationToken);
            return token.AccessToken;
        }
    }

    public class Credential
    {
        internal Mise mise;
        internal CredentialOptions? CredentialOptions { get; set; }

        internal Credential(CredentialOptions credentialOptions, Mise mise)
        {
            this.mise = mise;
            this.CredentialOptions = credentialOptions;
        }

        internal Credential(string? credentialToken, string? credentialCertificate, Mise mise)
        {
            this.credentialToken = credentialToken;
            this.credentialCertificate = credentialCertificate;
            this.mise = mise;
        }

        internal string? credentialToken;
        private string? credentialCertificate;
    }

 


}
