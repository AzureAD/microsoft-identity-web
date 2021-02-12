using Azure.Core;
using DotnetTool.AuthenticationParameters;
using DotnetTool.CodeReaderWriter;
using DotnetTool.DeveloperCredentials;
using DotnetTool.MicrosoftIdentityPlatformApplication;
using DotnetTool.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTool
{
    public class AppProvisionningTool
    {
        private ProvisioningToolOptions provisioningToolOptions { get; set; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        private ProjectDescriptionReader projectDescriptionReader { get; } = new ProjectDescriptionReader();

        public AppProvisionningTool(ProvisioningToolOptions provisioningToolOptions)
        {
            this.provisioningToolOptions = provisioningToolOptions;
        }

        public async Task Run()
        {
            // If needed, infer project type from code
            ProjectDescription projectDescription = projectDescriptionReader.GetProjectDescription(
                provisioningToolOptions.ProjectTypeIdentifier,
                provisioningToolOptions.CodeFolder);

            if (projectDescription == null)
            {
                Console.WriteLine("Could not determine the project type");
                return;
            }
            else
            {
                Console.WriteLine($"Detected {projectDescription.Identifier}");
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                provisioningToolOptions,
                projectDescription,
                projectDescriptionReader.projectDescriptions);

            if (!projectSettings.ApplicationParameters.HasAuthentication)
            {
                Console.WriteLine($"Authentication not enabled yet in this project. An app registration will " +
                                  $"be created, but the tool does not add yet the code (work in progress)");
            }

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                provisioningToolOptions,
                projectSettings.ApplicationParameters.TenantId ?? projectSettings.ApplicationParameters.Domain);

            if (provisioningToolOptions.Unregister)
            {
                await UnregisterApplication(tokenCredential, projectSettings.ApplicationParameters);
                return;
            }

            // Read or provision Microsoft identity platform application
            ApplicationParameters effectiveApplicationParameters = await ReadOrProvisionMicrosoftIdentityApplication(
                tokenCredential, 
                projectSettings.ApplicationParameters);

            // Reconciliate code configuration and app registration
            bool appNeedsUpdate = Reconciliate(
                projectSettings.ApplicationParameters, 
                effectiveApplicationParameters);

            // Update appp registration if needed
            Summary summary = new Summary();
            if (appNeedsUpdate)
            {
                await WriteApplicationRegistration(
                    summary,
                    effectiveApplicationParameters,
                    tokenCredential);
            }

            // Write code configuration if needed
            WriteProjectConfiguration(
                summary, 
                projectSettings,
                effectiveApplicationParameters);

            // Summarizes what happened
            WriteSummary(summary);
        }

        private void WriteSummary(Summary summary)
        {
            Console.WriteLine("Summary");
            foreach(Change change in summary.changes)
            {
                Console.WriteLine($"{change.Description}");
            }
        }

        private async Task WriteApplicationRegistration(Summary summary, ApplicationParameters reconcialedApplicationParameters, TokenCredential tokenCredential)
        {
            summary.changes.Add(new Change($"Writing the project AppId = {reconcialedApplicationParameters.ClientId}"));
            await MicrosoftIdentityPlatformApplicationManager.UpdateApplication(tokenCredential, reconcialedApplicationParameters);
        }

        private void WriteProjectConfiguration(Summary summary, ProjectAuthenticationSettings projectSettings, ApplicationParameters reconcialedApplicationParameters)
        {
            CodeWriter codeWriter = new CodeWriter();
            codeWriter.WriteConfiguration(summary, projectSettings.Replacements, reconcialedApplicationParameters);
        }

        private bool Reconciliate(ApplicationParameters applicationParameters, ApplicationParameters effectiveApplicationParameters)
        {
            // Redirect Uris that are needed by the code, but not yet registered 
            IEnumerable<string> missingRedirectUri = applicationParameters.WebRedirectUris.Except(effectiveApplicationParameters.WebRedirectUris);

            bool needUpdate = missingRedirectUri.Any();

            if (needUpdate)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(missingRedirectUri);
            }

            // TODO:
            // See also https://github.com/jmprieur/app-provisonning-tool/issues/10
            /*
                 string? audience = ComputeAudienceToSet(applicationParameters.SignInAudience, effectiveApplicationParameters.SignInAudience);
                IEnumerable<ApiPermission> missingApiPermission = null;
                IEnumerable<string> missingExposedScopes = null;
                bool needUpdate = missingRedirectUri != null || audience != null || missingApiPermission != null || missingExposedScopes != null;
            */
            return needUpdate;
        }

        private async Task<ApplicationParameters> ReadOrProvisionMicrosoftIdentityApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.ClientId))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters);
                if (currentApplicationParameters == null)
                {
                    Console.Write($"Couldn't find app {applicationParameters.ClientId} in tenant {applicationParameters.TenantId}");
                }
            }

            if (currentApplicationParameters == null && !provisioningToolOptions.Unregister)
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewApp(tokenCredential, applicationParameters);

                Console.Write($"Created app {currentApplicationParameters.ClientId}");
            }
            return currentApplicationParameters;
        }

        private ProjectAuthenticationSettings InferApplicationParameters(
            ProvisioningToolOptions provisioningToolOptions, 
            ProjectDescription projectDescription,
            IEnumerable<ProjectDescription> projectDescriptions)
        {
            CodeReader reader = new CodeReader();
            ProjectAuthenticationSettings projectSettings = reader.ReadFromFiles(provisioningToolOptions.CodeFolder, projectDescription, projectDescriptions);
            projectSettings.ApplicationParameters.DisplayName ??= Path.GetFileName(provisioningToolOptions.CodeFolder);
            projectSettings.ApplicationParameters.ClientId ??= provisioningToolOptions.ClientId;
            projectSettings.ApplicationParameters.TenantId ??= provisioningToolOptions.TenantId;
            return projectSettings;
        }


        private TokenCredential GetTokenCredential(ProvisioningToolOptions provisioningToolOptions, string? currentApplicationTenantId)
        {
            DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();
            return developerCredentialsReader.GetDeveloperCredentials(
                provisioningToolOptions.Username, 
                currentApplicationTenantId ?? provisioningToolOptions.TenantId);
        }

        private async Task UnregisterApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            await MicrosoftIdentityPlatformApplicationManager.Unregister(tokenCredential, applicationParameters);
        }
    }
}