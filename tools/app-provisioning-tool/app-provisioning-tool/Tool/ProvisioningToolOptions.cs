using DotnetTool.DeveloperCredentials;

namespace DotnetTool
{
    public class ProvisioningToolOptions : IDeveloperCredentialsOptions
    {
        public string CodeFolder { get; set; } = System.IO.Directory.GetCurrentDirectory();
        /// <summary>
        /// Language/Framework for the project
        /// </summary>
        public string LanguageOrFramework { get; set; } = "dotnet";

        /// <summary>
        /// Type of project for instance webapp, webapi, blazorwasm-hosted, ...
        /// </summary>
        public string? ProjectType { get; set; }

        /// <summary>
        /// Identifier of a project type. This is the concatenation of the framework
        /// and the project type. This is the identifier of the extension describing 
        /// the authentication pieces of the project
        /// </summary>
        public string ProjectTypeIdentifier
        {
            get
            {
                return $"{LanguageOrFramework}-{ProjectType}";
            }
        }

        /// <summary>
        /// Identity (for instance joe@cotoso.com) that is allowed to
        /// provision the application in the tenant. Optional if you want
        /// to use the developer credentials (Visual Studio)
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Client secret for the app
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Client ID of the application (optional)
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Tenant ID of the application (optional if the user belongs to
        /// only one tenant Id)
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Display Help
        /// </summary>
        internal bool Help { get; set; }

        public bool Unregister { get; set; }

        /// <summary>
        /// Scopes for the called web API
        /// </summary>
        public string? CalledApiScopes { get; set; }

        /// <summary>
        /// Url for the called web API
        /// </summary>
        public string? CalledApiUrl { get; set; }

        /// <summary>
        /// Calls Microsoft Graph
        /// </summary>
        public bool CallsGraph { get; set; }
    }

    /// <summary>
    /// Extension methods for ProvisioningToolOptions
    /// </summary>
    public static class ProvisioningToolOptionsExtensions
    {
        /// <summary>
        /// Identifier of a project type. This is the concatenation of the framework
        /// and the project type. This is the identifier of the extension describing 
        /// the authentication pieces of the project
        /// </summary>
        public static string GetProjectTypeIdentifier(this ProvisioningToolOptions provisioningToolOptions)
        {
            return $"{provisioningToolOptions.LanguageOrFramework}-{provisioningToolOptions.ProjectType}";
        }
    }
}
