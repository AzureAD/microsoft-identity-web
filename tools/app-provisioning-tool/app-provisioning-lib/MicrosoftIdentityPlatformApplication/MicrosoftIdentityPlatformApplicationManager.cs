﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Identity.App.AuthenticationParameters;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Beta;

namespace Microsoft.Identity.App.MicrosoftIdentityPlatformApplication
{
    public static class GraphServiceClientExtensions
    {
        public static string? GetTenantType(this Organization tenant)
        {
            return tenant.AdditionalData["tenantType"]?.ToString();
        }
    }
    public class MicrosoftIdentityPlatformApplicationManager
    {
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string ScopeType = "Scope";

        GraphServiceClient? _graphServiceClient;

        internal async Task<ApplicationParameters> CreateNewApp(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            // Get the tenant
            Organization? tenant = await GetTenant(graphServiceClient);
            bool isCiamTenant = tenant?.GetTenantType() == "CIAM";

            // Create the app.
            Application application = new Application()
            {
                DisplayName = applicationParameters.ApplicationDisplayName,
                SignInAudience = AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(applicationParameters.SignInAudience!, isCiamTenant),
                Description = applicationParameters.Description
            };

            if (applicationParameters.IsWebApi)
            {
                application.Api = new ApiApplication()
                {
                    RequestedAccessTokenVersion = 2,
                };
            }

            if (applicationParameters.IsWebApp)
            {
                AddWebAppPlatform(applicationParameters, application);
            }
            else if (applicationParameters.IsBlazorWasm)
            {
                // In .NET Core 3.1, Blazor uses MSAL.js 1.x (web redirect URIs)
                // whereas in .NET 5.0, Blazor uses MSAL.js 2.x (SPA redirect URIs)
                if (applicationParameters.TargetFramework == "net5.0")
                {
                    AddSpaPlatform(applicationParameters, application);
                }
                else
                {
                    AddWebAppPlatform(applicationParameters, application, true);
                }
            }

            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = await AddApiPermissions(
                applicationParameters,
                graphServiceClient,
                application).ConfigureAwait(false);

            Application? createdApplication = await graphServiceClient.Applications
                .PostAsync(application);

            // Add the current user as a owner.
            User? me = await graphServiceClient.Me.GetAsync();
            var requestBody = new ReferenceCreate
            {
                OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{me.Id}",
            };
            await graphServiceClient.Applications[createdApplication.Id].Owners.Ref.PostAsync(requestBody);


            // Creates a service principal (needed for B2C)
            ServicePrincipal servicePrincipal = new ServicePrincipal
            {
                AppId = createdApplication.AppId,
            };

            // B2C does not allow user consent, and therefore we need to explicity create
            // a service principal and permission grants. It's also useful for Blazorwasm hosted
            // applications. We create it always.
            var createdServicePrincipal = await graphServiceClient.ServicePrincipals
                .PostAsync(servicePrincipal).ConfigureAwait(false);

            // B2C and CIAM don't allow user consent, and therefore we need to explicity grant permissions
            if (applicationParameters.IsB2C || applicationParameters.IsCiam || isCiamTenant)
            {
                await AddAdminConsentToApiPermissions(
                    graphServiceClient,
                    createdServicePrincipal,
                    scopesPerResource);
            }

            // For web API, we need to know the appId of the created app to compute the Identifier URI, 
            // and therefore we need to do it after the app is created (updating the app)
            if (applicationParameters.IsWebApi
                && createdApplication.Api != null
                && (createdApplication.IdentifierUris == null || !createdApplication.IdentifierUris.Any()))
            {
                await ExposeScopes(graphServiceClient, createdApplication);

                // Blazorwasm hosted: add permission to server web API from client SPA
                if (applicationParameters.IsBlazorWasm)
                {
                    await AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
                        graphServiceClient,
                        createdApplication,
                        createdServicePrincipal,
                        applicationParameters.IsB2C || applicationParameters.IsCiam || isCiamTenant);
                }
            }

            // Re-reading the app to be sure to have everything.
            createdApplication = (await graphServiceClient.Applications
                .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{createdApplication.AppId}'")).Value.First();

            var effectiveApplicationParameters = GetEffectiveApplicationParameters(tenant!, createdApplication, applicationParameters);

            // Add password credentials
            if (applicationParameters.CallsMicrosoftGraph || applicationParameters.CallsDownstreamApi)
            {
                await AddPasswordCredentials(
                    graphServiceClient,
                    createdApplication,
                    effectiveApplicationParameters);
            }

            if (isCiamTenant)
            {
               // Disabled for the moment, as it requires the following permissions:
/*             // (Policy.ReadWrite.AuthenticationFlows, EventListener.Read.All, EventListener.ReadWrite.All, Application.Read.All, Application.ReadWrite.All) to access the resource."
 
                var flows = await graphServiceClient.Identity.AuthenticationEventsFlows.GetAsync();
                var flow = flows.Value.FirstOrDefault();
                if (flow != null)
                {
                    flow.Conditions.Applications.IncludeApplications.Add(new AuthenticationConditionApplication() { AppId = createdApplication.AppId });
                    await graphServiceClient.Identity.AuthenticationEventsFlows[flow.Id].PatchAsync(flow);
                }

                // See https://github.com/microsoft/entra-previews/blob/PP3/docs/API-reference-CIAM-user-flows.md#scenario-9-attach-an-application-to-a-user-flow
*/
            }


            return effectiveApplicationParameters;
        }

        private static async Task<Organization?> GetTenant(GraphServiceClient graphServiceClient)
        {
            Organization? tenant = null;
            try
            {
                tenant = (await graphServiceClient.Organization
                    .GetAsync()).Value?.FirstOrDefault();
            }
            catch (ServiceException ex)
            {
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
                else
                {
                    if (ex.Message.Contains("User was not found", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("not found in tenant", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("User was not found.\nUse both --tenant-id <tenant> --username <username@tenant>.\nAnd re-run the tool.");
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                Environment.Exit(1);
            }

            return tenant;
        }

        internal async Task UpdateApplication(TokenCredential tokenCredential, ApplicationParameters reconcialedApplicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var existingApplication = (await graphServiceClient.Applications
               .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{reconcialedApplicationParameters.ClientId}'"))?.Value.First();

            // Updates the redirect URIs
            var updatedApp = new Application
            {
                Web = existingApplication.Web
            };
            updatedApp.Web.RedirectUris = reconcialedApplicationParameters.WebRedirectUris;

            // TODO: update other fields. 
            // See https://github.com/jmprieur/app-provisonning-tool/issues/10
            await graphServiceClient.Applications[existingApplication.Id].
                PatchAsync(updatedApp).ConfigureAwait(false);

            if (existingApplication.RequiredResourceAccess != null
                && !reconcialedApplicationParameters.IsBlazorWasm
                && existingApplication.RequiredResourceAccess.Any()
                && (existingApplication.PasswordCredentials == null
                || !existingApplication.PasswordCredentials.Any()
                || !existingApplication.PasswordCredentials.Any(password => !string.IsNullOrEmpty(password.SecretText))))
            {
                await AddPasswordCredentials(
                    graphServiceClient,
                    existingApplication,
                    reconcialedApplicationParameters).ConfigureAwait(false);
            }
        }

        private async Task AddApiPermissionFromBlazorwasmHostedSpaToServerApi(
            GraphServiceClient graphServiceClient,
            Application createdApplication,
            ServicePrincipal createdServicePrincipal,
            bool isB2cOrCiam)
        {
            var requiredResourceAccess = new List<RequiredResourceAccess>();
            var resourcesAccessAndScopes = new List<ResourceAndScope>
                {
                    new ResourceAndScope($"api://{createdApplication.AppId}", "access_as_user")
                    {
                         ResourceServicePrincipalId = createdServicePrincipal.Id
                    }
                };

            await AddPermission(
                graphServiceClient,
                requiredResourceAccess,
                resourcesAccessAndScopes.GroupBy(r => r.Resource).First()).ConfigureAwait(false);

            Application applicationToUpdate = new Application
            {
                RequiredResourceAccess = requiredResourceAccess
            };

            await graphServiceClient.Applications[createdApplication.Id]
                .PatchAsync(applicationToUpdate).ConfigureAwait(false);

            if (isB2cOrCiam)
            {
                var oAuth2PermissionGrant = new OAuth2PermissionGrant
                {
                    ClientId = createdServicePrincipal.Id,
                    ConsentType = "AllPrincipals",
                    PrincipalId = null,
                    ResourceId = createdServicePrincipal.Id,
                    Scope = "access_as_user"
                };

                await graphServiceClient.Oauth2PermissionGrants
                    .PostAsync(oAuth2PermissionGrant).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Add a password credential to the app
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <param name="effectiveApplicationParameters"></param>
        /// <returns></returns>
        private static async Task AddPasswordCredentials(GraphServiceClient graphServiceClient, Application createdApplication, ApplicationParameters effectiveApplicationParameters)
        {
            var requestBody = new Microsoft.Graph.Beta.Applications.Item.AddPassword.AddPasswordPostRequestBody
            {
                PasswordCredential = new PasswordCredential
                {
                    DisplayName = "Password created by the provisioning tool"
                },
            };

            PasswordCredential? returnedPasswordCredential = await graphServiceClient.Applications[$"{createdApplication.Id}"]
                .AddPassword.PostAsync(requestBody);
            if (returnedPasswordCredential != null)
            {
                effectiveApplicationParameters.PasswordCredentials.Add(returnedPasswordCredential.SecretText);
            }
        }

        /// <summary>
        /// Expose scopes for the web API.
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="createdApplication"></param>
        /// <returns></returns>
        private static async Task ExposeScopes(GraphServiceClient graphServiceClient, Application createdApplication)
        {
            var updatedApp = new Application
            {
                IdentifierUris = new List<string>(new[] { $"api://{createdApplication.AppId}" }),
            };
            var scopes = createdApplication.Api.Oauth2PermissionScopes?.ToList() ?? new List<PermissionScope>();
            var newScope = new PermissionScope
            {
                Id = Guid.NewGuid(),
                AdminConsentDescription = "Allows the app to access the web API on behalf of the signed-in user",
                AdminConsentDisplayName = "Access the API on behalf of a user",
                Type = "User",
                IsEnabled = true,
                UserConsentDescription = "Allows this app to access the web API on your behalf",
                UserConsentDisplayName = "Access the API on your behalf",
                Value = "access_as_user",
            };
            scopes.Add(newScope);
            updatedApp.Api = new ApiApplication { Oauth2PermissionScopes = scopes };

            await graphServiceClient.Applications[createdApplication.Id]
                .PatchAsync(updatedApp).ConfigureAwait(false);
        }

        /// <summary>
        /// Admin consent to API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="scopesPerResource"></param>
        /// <returns></returns>
        private static async Task AddAdminConsentToApiPermissions(
            GraphServiceClient graphServiceClient,
            ServicePrincipal servicePrincipal,
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource)
        {
            // Changed: https://learn.microsoft.com/graph/permissions-grant-via-msgraph?tabs=csharp&pivots=grant-application-permissions#step-2-grant-an-app-role-to-a-client-service-principal

            // Consent to the scopes
            if (scopesPerResource != null)
            {
                foreach (var g in scopesPerResource)
                {
                    IEnumerable<ResourceAndScope> resourceAndScopes = g;

                    var oAuth2PermissionGrant = new OAuth2PermissionGrant
                    {
                        ClientId = servicePrincipal.Id,
                        ConsentType = "AllPrincipals",
                        PrincipalId = null,
                        ResourceId = resourceAndScopes.FirstOrDefault()?.ResourceServicePrincipalId,
                        Scope = string.Join(" ", resourceAndScopes.Select(r => r.Scope)),
                        StartTime = DateTimeOffset.UtcNow,
                        ExpiryTime = DateTimeOffset.MaxValue,
                    };

                    // TODO: See https://github.com/jmprieur/app-provisonning-tool/issues/9. 
                    // We need to process the case where the developer is not a tenant admin
                    try
                    {
                        var effectivePermissionGrant = await graphServiceClient.Oauth2PermissionGrants
                            .PostAsync(oAuth2PermissionGrant);
                    }
                    catch (ServiceException ex) when (ex.Message == "Permission entry already exists.")
                    {
                        // Nothing to do.
                    }
                }
            }
        }

        /// <summary>
        /// Add API permissions.
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="graphServiceClient"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task<IEnumerable<IGrouping<string, ResourceAndScope>>?> AddApiPermissions(
            ApplicationParameters applicationParameters,
            GraphServiceClient graphServiceClient,
            Application application)
        {
            // Case where the app calls a downstream API
            List<RequiredResourceAccess> apiRequests = new List<RequiredResourceAccess>();
            string? calledApiScopes = applicationParameters?.CalledApiScopes;
            IEnumerable<IGrouping<string, ResourceAndScope>>? scopesPerResource = null;
            if (!string.IsNullOrEmpty(calledApiScopes))
            {
                string[] scopes = calledApiScopes.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                scopesPerResource = scopes.Select(s => (!s.Contains('/', StringComparison.OrdinalIgnoreCase))
                // Microsoft Graph shortcut scopes (for instance "User.Read")
                ? new ResourceAndScope("https://graph.microsoft.com", s)
                // Proper AppIdUri/scope
                : new ResourceAndScope(s.Substring(0, s.LastIndexOf('/')), s[(s.LastIndexOf('/') + 1)..])
                ).GroupBy(r => r.Resource)
                .ToArray(); // We want to modify these elements to cache the service principal ID

                foreach (var g in scopesPerResource)
                {
                    await AddPermission(graphServiceClient, apiRequests, g);
                }
            }

            if (apiRequests.Any())
            {
                application.RequiredResourceAccess = apiRequests;
            }

            return scopesPerResource;
        }

        /// <summary>
        /// Adds a SPA redirect URI
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="application"></param>
        private static void AddSpaPlatform(ApplicationParameters applicationParameters, Application application)
        {
            application.Spa = new SpaApplication();
            application.Spa.RedirectUris = applicationParameters.WebRedirectUris;
        }

        /// <summary>
        /// Adds the Web redirect URIs (and required scopes in the case of B2C web apis)
        /// </summary>
        /// <param name="applicationParameters"></param>
        /// <param name="application"></param>
        /// <param name="withImplicitFlow">Should it add the implicit flow access token (for Blazor in netcore3.1)</param>
        private static void AddWebAppPlatform(ApplicationParameters applicationParameters, Application application, bool withImplicitFlow = false)
        {
            application.Web = new WebApplication();

            // IdToken
            if ((!applicationParameters.CallsDownstreamApi && !applicationParameters.CallsMicrosoftGraph)
                || withImplicitFlow)
            {
                application.Web.ImplicitGrantSettings = new ImplicitGrantSettings();
                application.Web.ImplicitGrantSettings.EnableIdTokenIssuance = true;
                if (applicationParameters.IsB2C || withImplicitFlow)
                {
                    application.Web.ImplicitGrantSettings.EnableAccessTokenIssuance = true;
                }
            }

            // Redirect URIs
            application.Web.RedirectUris = applicationParameters.WebRedirectUris;

            // Logout URI
            application.Web.LogoutUrl = applicationParameters.LogoutUrl;

            // Explicit usage of MicrosoftGraph openid and offline_access, in the case
            // of Azure AD B2C.
            if (applicationParameters.IsB2C && applicationParameters.IsWebApp || applicationParameters.IsBlazorWasm)
            {
                if (applicationParameters.CalledApiScopes == null)
                {
                    applicationParameters.CalledApiScopes = string.Empty;
                }
                if (!applicationParameters.CalledApiScopes.Contains("openid", StringComparison.OrdinalIgnoreCase))
                {
                    applicationParameters.CalledApiScopes += " openid";
                }
                if (!applicationParameters.CalledApiScopes.Contains("offline_access", StringComparison.OrdinalIgnoreCase))
                {
                    applicationParameters.CalledApiScopes += " offline_access";
                }
                applicationParameters.CalledApiScopes = applicationParameters.CalledApiScopes.Trim();
            }
        }


        /// <summary>
        /// Adds API permissions
        /// </summary>
        /// <param name="graphServiceClient"></param>
        /// <param name="apiRequests"></param>
        /// <param name="g"></param>
        /// <returns></returns>
        private async Task AddPermission(
            GraphServiceClient graphServiceClient,
            List<RequiredResourceAccess> apiRequests,
            IGrouping<string, ResourceAndScope> g)
        {

            var spsWithScopes = (await graphServiceClient.ServicePrincipals
                .GetAsync(options => options.QueryParameters.Filter = $"servicePrincipalNames/any(t: t eq '{g.Key}')"))?.Value;

            // Special case for B2C where the service principal does not contain the graph URL :(
            if (!spsWithScopes.Any() && g.Key == "https://graph.microsoft.com")
            {
                spsWithScopes = (await graphServiceClient.ServicePrincipals
                                .GetAsync(options => options.QueryParameters.Filter = $"AppId eq '{MicrosoftGraphAppId}'"))?.Value;
            }
            ServicePrincipal? spWithScopes = spsWithScopes.FirstOrDefault();

            if (spWithScopes == null)
            {
                throw new ArgumentException($"Service principal named {g.Key} not found.", nameof(g));
            }

            // Keep the service principal ID for later
            foreach (ResourceAndScope r in g)
            {
                r.ResourceServicePrincipalId = spWithScopes.Id;
            }

            IEnumerable<string> scopes = g.Select(r => r.Scope.ToLower(CultureInfo.InvariantCulture));
            IEnumerable<PermissionScope>? permissionScopes = null;
            IEnumerable<AppRole>? appRoles = null;

            if (scopes.Contains(".default"))
            {
            }
            else
            {
                permissionScopes = spWithScopes.PublishedPermissionScopes.Where(s => scopes.Contains(s.Value.ToLower(CultureInfo.InvariantCulture)));
                appRoles = spWithScopes.AppRoles.Where(s => scopes.Contains(s.Value.ToLower(CultureInfo.InvariantCulture)));
            }

            if (permissionScopes != null | appRoles != null)
            {
                var resourceAccess = new List<ResourceAccess>();
                if (permissionScopes != null)
                {
                    resourceAccess.AddRange(permissionScopes.Select(p =>
                     new ResourceAccess
                     {
                         Id = p.Id,
                         Type = ScopeType
                     }));
                };

                if (appRoles != null)
                {
                    resourceAccess.AddRange(appRoles.Select(p =>
                     new ResourceAccess
                     {
                         Id = p.Id,
                         Type = ScopeType
                     }));
                };

                RequiredResourceAccess requiredResourceAccess = new RequiredResourceAccess
                {
                    ResourceAppId = spWithScopes.AppId,
                    ResourceAccess = resourceAccess
                };
                apiRequests.Add(requiredResourceAccess);
            }
        }

        /// <summary>
        /// Computes the audience
        /// </summary>
        /// <param name="audience"></param>
        /// <returns></returns>
        private string MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(string audience)
        {
            return audience switch
            {
                "AzureADMyOrg" => "SingleOrg",
                "AzureADMultipleOrgs" => "MultiOrg",
                "AzureADandPersonalMicrosoftAccount" => "MultiOrgAndPersonal",
                "PersonalMicrosoftAccount" => "Personal",
                _ => "SingleOrg",
            };
        }

        private string AppParameterAudienceToMicrosoftIdentityPlatformAppAudience(string audience, bool isCiamTenant)
        {
            if (isCiamTenant)
            {
                return "AzureADMyOrg";
            }
            return audience switch
            {
                "SingleOrg" => "AzureADMyOrg",
                "MultiOrg" => "AzureADMultipleOrgs",
                "Personal" => "PersonalMicrosoftAccount",
                _ => "AzureADandPersonalMicrosoftAccount",
            };
        }

        internal async Task Unregister(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            var apps = await graphServiceClient.Applications
                .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{applicationParameters.ClientId}'");

            var readApplication = apps?.Value.FirstOrDefault();
            if (readApplication != null)
            {
                await graphServiceClient.Applications[$"{readApplication.Id}"]
                    .DeleteAsync();
            }
        }

        private GraphServiceClient GetGraphServiceClient(TokenCredential tokenCredential)
        {
            if (_graphServiceClient == null)
            {
                _graphServiceClient = new GraphServiceClient(new TokenCredentialAuthenticationProvider(tokenCredential));
            }
            return _graphServiceClient;
        }

        public async Task<ApplicationParameters?> ReadApplication(TokenCredential tokenCredential, ApplicationParameters applicationParameters)
        {
            var graphServiceClient = GetGraphServiceClient(tokenCredential);

            // Get the tenant
            Organization? tenant = await GetTenant(graphServiceClient);

            var apps = await graphServiceClient.Applications
                .GetAsync(options => options.QueryParameters.Filter = $"appId eq '{applicationParameters.ClientId}'");

            var readApplication = apps?.Value.FirstOrDefault();

            if (readApplication == null)
            {
                return null;
            }

            ApplicationParameters effectiveApplicationParameters = GetEffectiveApplicationParameters(
                tenant!,
                readApplication,
                applicationParameters);

            return effectiveApplicationParameters;

        }

        private ApplicationParameters GetEffectiveApplicationParameters(
            Organization tenant,
            Application application,
            ApplicationParameters originalApplicationParameters)
        {
            bool isCiam = (tenant.GetTenantType() == "CIAM");
            bool isB2C = (tenant.GetTenantType() == "AAD B2C");
            var effectiveApplicationParameters = new ApplicationParameters
            {
                ApplicationDisplayName = application.DisplayName,
                ClientId = application.AppId,
                EffectiveClientId = application.AppId,
                IsAAD = !isB2C,
                IsB2C = isB2C,
                IsCiam = isCiam,
                HasAuthentication = true,
                IsWebApi = application.Api != null
                        && (application.Api.Oauth2PermissionScopes != null && application.Api.Oauth2PermissionScopes.Any())
                        || (application.AppRoles != null && application.AppRoles.Any()),
                IsWebApp = application.Web != null,
                IsBlazorWasm = application.Spa != null,
                TenantId = tenant.Id,
                Domain = tenant.VerifiedDomains.FirstOrDefault(v => v.IsDefault.HasValue && v.IsDefault.Value)?.Name,
                CallsMicrosoftGraph = application.RequiredResourceAccess.Any(r => r.ResourceAppId == MicrosoftGraphAppId) && !isB2C,
                CallsDownstreamApi = application.RequiredResourceAccess.Any(r => r.ResourceAppId != MicrosoftGraphAppId),
                LogoutUrl = application.Web?.LogoutUrl,

                // Parameters that cannot be infered from the registered app
                SusiPolicy = originalApplicationParameters.SusiPolicy,
                SecretsId = originalApplicationParameters.SecretsId,
                TargetFramework = originalApplicationParameters.TargetFramework,
                MsalAuthenticationOptions = originalApplicationParameters.MsalAuthenticationOptions,
                CalledApiScopes = originalApplicationParameters.CalledApiScopes,
                AppIdUri = originalApplicationParameters.AppIdUri
            };

            if (application.Api != null && application.IdentifierUris.Any())
            {
                effectiveApplicationParameters.AppIdUri = application.IdentifierUris.FirstOrDefault();
            }

            // Todo: might be a bit more complex in some cases for the B2C case.
            // TODO: handle b2c custom domains & domains ending in b2c.login.*
            // TODO: introduce the Instance?
            effectiveApplicationParameters.Authority = isB2C
                 ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/{effectiveApplicationParameters.Domain}/{effectiveApplicationParameters.SusiPolicy}/"
                 : originalApplicationParameters.IsCiam ? $"https://{effectiveApplicationParameters.Domain1}.ciamlogin.com/"
                 : $"https://login.microsoftonline.com/{effectiveApplicationParameters.TenantId ?? effectiveApplicationParameters.Domain}/";
            effectiveApplicationParameters.Instance = isB2C
                ? $"https://{effectiveApplicationParameters.Domain1}.b2clogin.com/"
                : originalApplicationParameters.IsCiam ? $"https://{effectiveApplicationParameters.Domain1}.ciamlogin.com/"
                : originalApplicationParameters.Instance;

            effectiveApplicationParameters.PasswordCredentials.AddRange(application.PasswordCredentials.Select(p => p.Hint + "******************"));
            if (application.Spa != null && application.Spa.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(application.Spa.RedirectUris);
            }
            else if (application.Web != null && application.Web.RedirectUris != null)
            {
                effectiveApplicationParameters.WebRedirectUris.AddRange(application.Web.RedirectUris);
            }

            effectiveApplicationParameters.SignInAudience = MicrosoftIdentityPlatformAppAudienceToAppParameterAudience(effectiveApplicationParameters.SignInAudience!);
            return effectiveApplicationParameters;
        }
    }
}
