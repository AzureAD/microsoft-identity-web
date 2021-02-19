// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace DotnetTool
{
    /// <summary>
    /// 
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Creates or updates an Azure AD / Azure AD B2C application, and updates the code, using
        /// your developer credentials (from Visual Studio, Azure CLI, Azure RM PowerShell, VS Code).
        /// Use this tool in folders containing applications created with the following command:
        /// 
        ///   <c>dotnet new template --auth authOption [--calls-graph] [--called-api-url URI --called-api-scopes scopes]</c>
        /// 
        /// where the template is in <c>webapp</c>, <c>mvc</c>, <c>webapi</c>, <c>blazorserver</c>, <c>blazorwasm</c>.
        /// See https://aka.ms/ms-identity-app-registration.
        /// </summary>
        /// <param name="tenantId">Azure AD or Azure AD B2C tenant in which to create/update the app. 
        /// - If specified, the tool will create the application in the specified tenant. 
        /// - Otherwise it will create the app in your home tenant.</param>
        /// <param name="username">Username to use to connect to the Azure AD or Azure AD B2C tenant.
        /// It's only needed when you are signed-in in Visual Studio, or Azure CLI with 
        /// several identities. In that case, the username param is used to disambiguate
        /// which  identity to use to create the app in the tenant.</param>
        /// <param name="clientId">Client ID of an existing application from which to update the code. This is
        /// used when you don't want to register a new app, but want to configure the code 
        /// from an existing application (which can also be updated by the tool if needed).
        /// You might want to also pass-in the <paramref name="clientSecret"/> if you know it.</param>
        /// <param name="unregister">Unregister the application, instead of registering it.</param>
        /// <param name="folder">When specified, will analyze the application code in the specified folder. 
        /// Otherwise analyzes the code in the current directory.</param>
        /// <param name="clientSecret">Client secret to use as a client credential.</param>
        /// <returns></returns>
        static public async Task Main(
            string? tenantId,
            string? username,
            string? clientId,
            string? folder,
            string? clientSecret,
            bool? unregister = false)
        {
            // Read options
            ProvisioningToolOptions provisioningToolOptions = new ProvisioningToolOptions
            {
                Username = username,
                ClientId = clientId,
                ClientSecret = clientSecret,
                TenantId = tenantId,
                Unregister = unregister.HasValue ? unregister.Value : false
            };

            if (folder != null)
            {
                provisioningToolOptions.CodeFolder = folder;
            }

            AppProvisioningTool appProvisionningTool = new AppProvisioningTool(provisioningToolOptions);
            await appProvisionningTool.Run();
        }

        private static void GenerateTests()
        {
            string parentFolder = @"C:\gh\microsoft-identity-web\ProjectTemplates\bin\Debug\tests";

            foreach (string subFolder in System.IO.Directory.GetDirectories(parentFolder))
            {
                foreach (string projectFolder in System.IO.Directory.GetDirectories(subFolder))
                {
                    System.Console.WriteLine($"[InlineData(@\"{System.IO.Path.GetFileName(subFolder)}\\{System.IO.Path.GetFileName(projectFolder)}\", {projectFolder.Contains("b2c")}, \"dotnet-WebApp\")]");
                }
            }
        }
    }
}
