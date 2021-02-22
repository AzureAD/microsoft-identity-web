// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#define AzureSDK

using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;

namespace Microsoft.Identity.App.DeveloperCredentials
{
#if AzureSDK
    // Used to debug
    public class ProvisioningToolCredentials : DefaultAzureCredential
    {
        public ProvisioningToolCredentials(DefaultAzureCredentialOptions options) : base(options)
        {

        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            AccessToken token = base.GetToken(requestContext, cancellationToken);
            return token;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            ValueTask<AccessToken> token = base.GetTokenAsync(requestContext, cancellationToken);

            AccessToken t = token.Result;
            return token;
        }

    }
#endif

    public class DeveloperCredentialsReader
    {
        public async Task<TokenCredential> GetDeveloperCredentials(string? username, string? currentApplicationTenantId)
        {
#if AzureSDK
            string? tenandId = await GetTenantIdFromTenantName(currentApplicationTenantId);

            DefaultAzureCredentialOptions defaultAzureCredentialOptions = new DefaultAzureCredentialOptions()
            {
                SharedTokenCacheTenantId = currentApplicationTenantId,
                SharedTokenCacheUsername = username,
            };

            // Exclude managed identity as we want to create/update the app on behalf of the developer
            defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;

            // Excluding Visual Studio credentials as it does not have the Applications.ReadWrite.All scopes yet
            // (Need preauth)
            defaultAzureCredentialOptions.ExcludeVisualStudioCredential = true;

            if (!string.IsNullOrEmpty(username))
            {
                defaultAzureCredentialOptions.SharedTokenCacheUsername = username;
            }

            // SharedTokenCacheCredential does not allow for MSA accounts
            // 'SharedTokenCacheCredential authentication failed: A configuration issue is preventing authentication - check the error message from the server for details.
            // You can modify the configuration in the application registration portal. See https://aka.ms/msal-net-invalid-client for details.  
            // Original exception: AADSTS70002: The client does not exist or is not enabled for consumers. 
            // If you are the application developer, configure a new application through the App Registrations in the Azure Portal at 
            // https://go.microsoft.com/fwlink/?linkid=2083908.
            if (currentApplicationTenantId == null)
            {
                defaultAzureCredentialOptions.ExcludeSharedTokenCacheCredential = true;
            }

            // I have not tried
            defaultAzureCredentialOptions.ExcludeVisualStudioCodeCredential = false;
            defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = false;


            if (!string.IsNullOrWhiteSpace(tenandId))
            {
                defaultAzureCredentialOptions.SharedTokenCacheTenantId = tenandId;
                defaultAzureCredentialOptions.VisualStudioTenantId = tenandId;
                defaultAzureCredentialOptions.VisualStudioCodeTenantId = tenandId;
                defaultAzureCredentialOptions.InteractiveBrowserTenantId = tenandId;

                // At the moment, it's not possible to specify the tenant to the AzureCliCredential :-(
                defaultAzureCredentialOptions.ExcludeAzureCliCredential = true;
            }
            else
            {
                defaultAzureCredentialOptions.ExcludeAzureCliCredential = false;
            }

            DefaultAzureCredential credential = new ProvisioningToolCredentials(defaultAzureCredentialOptions);
            return credential;
#else
            TokenCredential tokenCredential = new MsalTokenCredential(
                currentApplicationTenantId,
                username);
            return tokenCredential;
#endif
        }

#if AzureSDK

        private static async Task<string?> GetTenantIdFromTenantName(string? currentApplicationTenantId)
        {
            string? tenandId = currentApplicationTenantId;

            // If the tenant Id is a domain name, we get the tenant ID GUID as the Azure SDK only accepts a GUID
            if (!string.IsNullOrWhiteSpace(currentApplicationTenantId) && !Guid.TryParse(currentApplicationTenantId, out _))
            {
                // Todo : support other clouds
                HttpClient client = new HttpClient();
                string json = await client.GetStringAsync($"https://login.microsoftonline.com/{currentApplicationTenantId}/v2.0/.well-known/openid-configuration");
                if (json != null)
                {
                    Metadata? metadata = JsonSerializer.Deserialize<Metadata>(json);
                    if (metadata != null && metadata.issuer != null)
                    {
                        tenandId = new UriBuilder(metadata.issuer).Path.Split('/').Skip(1).FirstOrDefault();
                    }
                }
            }

            return tenandId;
        }
#endif
    }
}
