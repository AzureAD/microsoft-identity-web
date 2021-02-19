// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Identity.App.AuthenticationParameters;
using Microsoft.Identity.App.CodeReaderWriter;
using Microsoft.Identity.App.DeveloperCredentials;
using Microsoft.Identity.App.MicrosoftIdentityPlatformApplication;
using Microsoft.Identity.App.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.App
{
    /// <summary>
    /// 
    /// </summary>
    public class AppProvisioningTool
    {
        private ProvisioningToolOptions ProvisioningToolOptions { get; set; }

        private MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager { get; } = new MicrosoftIdentityPlatformApplicationManager();

        private ProjectDescriptionReader ProjectDescriptionReader { get; } = new ProjectDescriptionReader();

        public AppProvisioningTool(ProvisioningToolOptions provisioningToolOptions)
        {
            ProvisioningToolOptions = provisioningToolOptions;
        }

        public async Task Run()
        {
            // If needed, infer project type from code
            ProjectDescription? projectDescription = ProjectDescriptionReader.GetProjectDescription(
                ProvisioningToolOptions.ProjectTypeIdentifier,
                ProvisioningToolOptions.CodeFolder);

            if (projectDescription == null)
            {
                Console.WriteLine("Could not determine the project type. ");
                return;
            }
            else
            {
                Console.WriteLine($"Detected {projectDescription.Identifier}. ");
            }

            ProjectAuthenticationSettings projectSettings = InferApplicationParameters(
                ProvisioningToolOptions,
                projectDescription,
                ProjectDescriptionReader.projectDescriptions);

            if (!projectSettings.ApplicationParameters.HasAuthentication)
            {
                Console.WriteLine($"Authentication not enabled yet in this project. An app registration will " +
                                  $"be created, but the tool does not add the code (work in progress). ");
            }

            // Get developer credentials
            TokenCredential tokenCredential = GetTokenCredential(
                ProvisioningToolOptions,
                projectSettings.ApplicationParameters.EffectiveTenantId ?? projectSettings.ApplicationParameters.EffectiveDomain);

            if (ProvisioningToolOptions.Unregister)
            {
                await UnregisterApplication(tokenCredential, projectSettings.ApplicationParameters);
                return;
            }

            // Read or provision Microsoft identity platform application
            ApplicationParameters? effectiveApplicationParameters = await ReadOrProvisionMicrosoftIdentityApplication(
                tokenCredential, 
                projectSettings.ApplicationParameters);

            Summary summary = new Summary();

            // Reconciliate code configuration and app registration
            if (effectiveApplicationParameters != null)
            {
                bool appNeedsUpdate = Reconciliate(
                    projectSettings.ApplicationParameters,
                    effectiveApplicationParameters);

                // Update appp registration if needed
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
            }

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
            // Redirect URIs that are needed by the code, but not yet registered 
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

        private async Task<ApplicationParameters?> ReadOrProvisionMicrosoftIdentityApplication(
            TokenCredential tokenCredential,
            ApplicationParameters applicationParameters)
        {
            ApplicationParameters? currentApplicationParameters = null;
            if (!string.IsNullOrEmpty(applicationParameters.EffectiveClientId))
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.ReadApplication(tokenCredential, applicationParameters);
                if (currentApplicationParameters == null)
                {
                    Console.Write($"Couldn't find app {applicationParameters.EffectiveClientId} in tenant {applicationParameters.EffectiveTenantId}. ");
                }
            }

            if (currentApplicationParameters == null && !ProvisioningToolOptions.Unregister)
            {
                currentApplicationParameters = await MicrosoftIdentityPlatformApplicationManager.CreateNewApp(tokenCredential, applicationParameters);

                Console.Write($"Created app {currentApplicationParameters.ClientId}. ");
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
