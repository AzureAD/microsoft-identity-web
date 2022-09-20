using System.Reflection;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client;

namespace GlueItConsoleApp
{
    public class GetAuthResult
    {
        public async Task<AuthenticationResult> GetAuthenticationResultAsync(
            string clientId,
            string tenantId,
            string[] webApiScopes)
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                .WithTenantId(tenantId)
                .WithDefaultRedirectUri()
                .Build();

            var storageProperties =
                       new StorageCreationPropertiesBuilder(
                           "GlueItConsoleApp",
                           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                       .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(pca.UserTokenCache);

            var accounts = await pca.GetAccountsAsync();
            IAccount firstAccount = accounts.FirstOrDefault();
            try
            {
                return await pca.AcquireTokenSilent(
                      webApiScopes,
                      firstAccount)
                      .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                return await pca.AcquireTokenInteractive(
                    webApiScopes)
                    .WithAccount(firstAccount)
                    .WithClaims(ex.Claims)
                    .ExecuteAsync();
            }
        }
    }
}
