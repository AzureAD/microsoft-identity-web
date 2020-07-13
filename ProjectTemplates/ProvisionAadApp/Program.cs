using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace ProvisionAadApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            DefaultAzureCredentialOptions defaultAzureCredentialOptions 
                = GetAzureCredentialOptionsFromConfiguration(args);

            string path = args.FirstOrDefault(a => !a.StartsWith("--"));
            if (string.IsNullOrWhiteSpace(path))
            {
                path = System.IO.Directory.GetCurrentDirectory();
            }

            // Get the configuration files
            string launchSettingsPath = Path.Combine(path, "Properties", "launchSettings.json");
            string appsettingsPath = Path.Combine(path, "appsettings.json");

            // Infer information
            string redirectUris = GetRedirectUri(launchSettingsPath);
            MicrosoftIdentityOptions appSettings = GetAppSettings(appsettingsPath);

            // Provision the application
            DefaultAzureCredential credential = new DefaultAzureCredential(defaultAzureCredentialOptions);
            await ProvisionAadApp(path, credential, redirectUris);
        }

        /// <summary>
        /// Get the credentials from the command line
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static DefaultAzureCredentialOptions GetAzureCredentialOptionsFromConfiguration(string[] args)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>
            {
                {"--tenant-id", "SharedTokenCacheTenantId"},
                {"--app-owner", "SharedTokenCacheUsername"}
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(args, mapping);
            IConfiguration configuration = configurationBuilder.Build();
            DefaultAzureCredentialOptions defaultAzureCredentialOptions = ConfigurationBinder.Get<DefaultAzureCredentialOptions>(configuration);
            defaultAzureCredentialOptions.ExcludeManagedIdentityCredential = true;
            defaultAzureCredentialOptions.ExcludeInteractiveBrowserCredential = false;
            return defaultAzureCredentialOptions;
        }

        private static MicrosoftIdentityOptions GetAppSettings(string appsettingsPath)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(appsettingsPath);
            IConfiguration configuration = configurationBuilder.Build();
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
            configuration.Bind("AzureAD", options);
            return options;
        }

        /// <summary>
        /// The redirect URIs should be https:// redirect URIs located in the
        /// launchSettings file
        /// </summary>
        /// <param name="launchSettingsPath"></param>
        /// <returns></returns>
        /// <remarks>For the moment, all the launchSettings.json define
        /// the same redirect URIs. Hard coding them. We can incrementally
        /// improve this in the future</remarks>
        private static string GetRedirectUri(string launchSettingsPath)
        {
            return "https://localhost:44300;https://localhost:5001";
        }

        static GraphServiceClient graphServiceClient;

        private async static Task ProvisionAadApp(string path, DefaultAzureCredential credential, string redirectUris)
        {
            if (graphServiceClient == null)
            {
                graphServiceClient = new GraphServiceClient(new TokenCredentialCredentialProvider(credential,
                                                            new string[] { "Application.ReadWrite.All" })
                                                           );
            }

            Application application = new Application()
            {
                DisplayName = Path.GetFileName(System.IO.Directory.GetCurrentDirectory()),
                SignInAudience = "AzureADandPersonalMicrosoftAccount",
                Tags = new [] { "{WindowsAzureActiveDirectoryIntegratedApp}" },
/*
                PasswordCredentials = new[] { new PasswordCredential() { } },
                RequiredResourceAccess = new[] { new RequiredResourceAccess()
                {
                } },
*/
                Web = new WebApplication()
                {
                    ImplicitGrantSettings = new ImplicitGrantSettings { EnableIdTokenIssuance = true },
                    RedirectUris = redirectUris.Split(';').Select(r => r+"/signin-oidc"),
                    LogoutUrl = redirectUris.Split(';').FirstOrDefault() + "/signout-callback-oidc"
                },
            };
            var app = await graphServiceClient.Applications
                                              .Request()
                                              .AddAsync(application);

            string appUrl = $"https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/{application.AppId}/isMSAApp/";
            Console.WriteLine(appUrl);
        }
    }
}
