using System.Threading.Tasks;

namespace DotnetTool
{
    /// <summary>
    /// 
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Creates or updates an AzureAD/Azure AD B2C application, and updates the code, using
        /// the developer credentials (Visual Studio, Azure CLI, Azure RM PowerShell, VS Code)
        /// </summary>
        /// <param name="tenantId">Azure AD or Azure AD B2C tenant in which to create/update the app. 
        /// If specified, the tool will create the application in the specified tenant. 
        /// Otherwise it will create the app in your home tenant ID.</param>
        /// <param name="username">Username to use to connect to the Azure AD or Azure AD B2C tenant.
        /// It's only needed when you are signed-in in Visual Studio, or Azure CLI with several identities. 
        /// In that case, the username param is used to disambiguate which identity to use.</param>
        /// <param name="folder">When specified, will analyze the application code in the specified folder. 
        /// Otherwise analyzes the code in the current directory</param>
        /// <param name="clientId">Client ID of an existing application from which to update the code. This is
        /// used when you don't want to register a new app in AzureAD/AzureAD B2C, but want to configure the
        /// code from an existing application (which can also be updated by the tool).</param>
        /// <param name="clientSecret">Client secret to use as a client credential.</param>
        /// <param name="unregister">Unregister the application, instead of registering it.</param>
        /// <returns></returns>
        static public async Task Main(
            string? tenantId = null,
            string? username = null,
            string? clientId = null,
            bool? unregister = false,
            string? folder = null,
            string? clientSecret = null)
        {
            // Read options
            ProvisioningToolOptions provisioningToolOptions = new ProvisioningToolOptions
            {
                Username = username,
                ClientId = clientId,
                ClientSecret = clientSecret,
                TenantId = tenantId
            };
            if (folder!=null)
            {
                provisioningToolOptions.CodeFolder = folder;
            }

            AppProvisionningTool appProvisionningTool = new AppProvisionningTool(provisioningToolOptions);
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
