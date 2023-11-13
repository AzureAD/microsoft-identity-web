// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.App.DeveloperCredentials
{
    public class MsalTokenCredential : TokenCredential
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        private const string RedirectUri = "http://localhost";
#pragma warning restore S1075 // URIs should not be hardcoded

        public MsalTokenCredential(string? tenantId, string? username, string instance = "https://login.microsoftonline.com")
        {
            TenantId = tenantId ?? "organizations"; // MSA-passthrough
            Username = username;
            Instance = instance;
        }

        private IPublicClientApplication? App { get; set; }
        private string? TenantId { get; set; }
        private string Instance { get; set; }
        private string? Username { get; set; }

        // OSX Token Cache 
        private const string MacOSAccountName = "MSALCache";
        private const string MacOSServiceName = "Microsoft.Developer.IdentityService";

        // Linux Token Cache 
        private const string LinuxKeyRingSchema = "com.microsoft.identity.tokencache";
        private const string LinuxKeyRingCollection = "default";
        private const string LinuxKeyRingLabel = "MSAL token cache";
        private static KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
        private static KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "Microsoft Develoepr Tools");

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task<IPublicClientApplication> GetOrCreateApp()
        {
            if (App == null)
            {

                string cacheDir = Path.Combine(MsalCacheHelper.UserRootDirectory, @"AppData\Local\.IdentityService");

                // TODO: Review token cache config. 
                string clientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
                var storageProperties =
                     new StorageCreationPropertiesBuilder(
                         "msal.cache",
                         cacheDir)
                     .WithLinuxKeyring(
                         LinuxKeyRingSchema,
                         LinuxKeyRingCollection,
                         LinuxKeyRingLabel,
                         LinuxKeyRingAttr1,
                         LinuxKeyRingAttr2)
                     .WithMacKeyChain(
                         MacOSServiceName,
                         MacOSAccountName)
                     .Build();

                App = PublicClientApplicationBuilder.Create(clientId)
                  .WithRedirectUri(RedirectUri)
                  .Build();

                // This hooks up the cross-platform cache into MSAL
                var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
                cacheHelper.RegisterCache(App.UserTokenCache);
            }
            return App;
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var app = await GetOrCreateApp();
            AuthenticationResult? result = null;
            var accounts = await app.GetAccountsAsync()!;
            IAccount? account;

            if (!string.IsNullOrEmpty(Username))
            {
                account = accounts.FirstOrDefault(account => account.Username == Username);
            }
            else
            {
                account = accounts.FirstOrDefault();
            }
            try
            {
                result = await app.AcquireTokenSilent(requestContext.Scopes, account)
                    .WithAuthority(Instance, TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException ex)
            {
                if (account == null && !string.IsNullOrEmpty(Username))
                {
                    Console.WriteLine($"No valid tokens found in the cache.\nPlease sign-in to Visual Studio with this account:\n\n{Username}.\n\nAfter signing-in, re-run the tool.\n");
                }
                result = await app.AcquireTokenInteractive(requestContext.Scopes)
                    .WithAccount(account)
                    .WithClaims(ex.Claims)
                    .WithAuthority(Instance, TenantId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalServiceException ex)
            {
                if (ex.Message.Contains("AADSTS70002", StringComparison.OrdinalIgnoreCase)) // "The client does not exist or is not enabled for consumers"
                {
                    Console.WriteLine("An Azure AD tenant, and a user in that tenant, " +
                        "needs to be created for this account before an application can be created. See https://aka.ms/ms-identity-app/create-a-tenant. ");
                    Environment.Exit(1); // we want to exit here because this is probably an MSA without an AAD tenant.
                }

                Console.WriteLine("Error encountered with sign-in. See error message for details:\n{0} ",
                    ex.Message);
                Environment.Exit(1); // we want to exit here. Re-sign in will not resolve the issue.
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error encountered with sign-in. See error message for details:\n{0} ",
                    ex.Message);
                Environment.Exit(1);
            }
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }
    }
}
