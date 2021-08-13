// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory. It has both AAD and B2C configuration attributes.
    /// Merges the MicrosoftIdentityWebOptions and the ConfidentialClientApplicationOptions.
    /// </summary>
    internal class MergedOptions : MicrosoftIdentityOptions
    {
        private ConfidentialClientApplicationOptions? _confidentialClientApplicationOptions;

        public ConfidentialClientApplicationOptions ConfidentialClientApplicationOptions
        {
            get
            {
                if (_confidentialClientApplicationOptions == null)
                {
                    _confidentialClientApplicationOptions = new ConfidentialClientApplicationOptions();
                    UpdateConfidentialClientApplicationOptionsFromMergedOptions(this, _confidentialClientApplicationOptions);
                }

                return _confidentialClientApplicationOptions;
            }
        }

        // Properties of ConfidentialClientApplication which are not in MicrosoftIdentityOptions
        public AadAuthorityAudience AadAuthorityAudience { get; set; }
        public AzureCloudInstance AzureCloudInstance { get; set; }
        public string? AzureRegion { get; set; }
        public IEnumerable<string>? ClientCapabilities { get; set; }
        public string? ClientName { get; set; }
        public string? ClientVersion { get; set; }
        public string? Component { get; set; }
        public bool EnablePiiLogging { get; set; }
        public bool IsDefaultPlatformLoggingEnabled { get; set; }
        public LogLevel LogLevel { get; set; }
        public string? RedirectUri { get; set; }
        public bool EnableCacheSynchronization { get; set; }

        internal static void UpdateMergedOptionsFromMicrosoftIdentityOptions(MicrosoftIdentityOptions microsoftIdentityOptions, MergedOptions mergedOptions)
        {
            mergedOptions.AccessDeniedPath = microsoftIdentityOptions.AccessDeniedPath;
            mergedOptions.AllowWebApiToBeAuthorizedByACL = microsoftIdentityOptions.AllowWebApiToBeAuthorizedByACL;
            mergedOptions.AuthenticationMethod = microsoftIdentityOptions.AuthenticationMethod;
            if (string.IsNullOrEmpty(mergedOptions.Authority) && !string.IsNullOrEmpty(microsoftIdentityOptions.Authority))
            {
                mergedOptions.Authority = microsoftIdentityOptions.Authority;
            }

#if DOTNET_50_AND_ABOVE
            mergedOptions.AutomaticRefreshInterval = microsoftIdentityOptions.AutomaticRefreshInterval;
#endif
            mergedOptions.Backchannel ??= microsoftIdentityOptions.Backchannel;
            mergedOptions.BackchannelHttpHandler ??= microsoftIdentityOptions.BackchannelHttpHandler;
            mergedOptions.BackchannelTimeout = microsoftIdentityOptions.BackchannelTimeout;
            mergedOptions.CallbackPath = microsoftIdentityOptions.CallbackPath;
            if (string.IsNullOrEmpty(mergedOptions.ClaimsIssuer) && !string.IsNullOrEmpty(microsoftIdentityOptions.ClaimsIssuer))
            {
                mergedOptions.ClaimsIssuer = microsoftIdentityOptions.ClaimsIssuer;
            }

            mergedOptions.ClientCertificates ??= microsoftIdentityOptions.ClientCertificates;
            if (string.IsNullOrEmpty(mergedOptions.ClientId) && !string.IsNullOrEmpty(microsoftIdentityOptions.ClientId))
            {
                mergedOptions.ClientId = microsoftIdentityOptions.ClientId;
            }

            if (string.IsNullOrEmpty(mergedOptions.ClientSecret) && !string.IsNullOrEmpty(microsoftIdentityOptions.ClientSecret))
            {
                mergedOptions.ClientSecret = microsoftIdentityOptions.ClientSecret;
            }

            mergedOptions.Configuration ??= microsoftIdentityOptions.Configuration;
            mergedOptions.ConfigurationManager ??= microsoftIdentityOptions.ConfigurationManager;
            mergedOptions.CorrelationCookie = microsoftIdentityOptions.CorrelationCookie;
            mergedOptions.DataProtectionProvider ??= microsoftIdentityOptions.DataProtectionProvider;
            mergedOptions.DisableTelemetry = microsoftIdentityOptions.DisableTelemetry;

            if (string.IsNullOrEmpty(mergedOptions.Domain) && !string.IsNullOrEmpty(microsoftIdentityOptions.Domain))
            {
                mergedOptions.Domain = microsoftIdentityOptions.Domain;
            }

            if (string.IsNullOrEmpty(mergedOptions.EditProfilePolicyId) && !string.IsNullOrEmpty(microsoftIdentityOptions.EditProfilePolicyId))
            {
                mergedOptions.EditProfilePolicyId = microsoftIdentityOptions.EditProfilePolicyId;
            }

            mergedOptions.Events.OnAccessDenied += microsoftIdentityOptions.Events.OnAccessDenied;
            mergedOptions.Events.OnAuthenticationFailed += microsoftIdentityOptions.Events.OnAuthenticationFailed;
            mergedOptions.Events.OnAuthorizationCodeReceived += microsoftIdentityOptions.Events.OnAuthorizationCodeReceived;
            mergedOptions.Events.OnMessageReceived += microsoftIdentityOptions.Events.OnMessageReceived;
            mergedOptions.Events.OnRedirectToIdentityProvider += microsoftIdentityOptions.Events.OnRedirectToIdentityProvider;
            mergedOptions.Events.OnRedirectToIdentityProviderForSignOut += microsoftIdentityOptions.Events.OnRedirectToIdentityProviderForSignOut;
            mergedOptions.Events.OnRemoteFailure += microsoftIdentityOptions.Events.OnRemoteFailure;
            mergedOptions.Events.OnRemoteSignOut += microsoftIdentityOptions.Events.OnRemoteSignOut;
            mergedOptions.Events.OnSignedOutCallbackRedirect += microsoftIdentityOptions.Events.OnSignedOutCallbackRedirect;
            mergedOptions.Events.OnTicketReceived += microsoftIdentityOptions.Events.OnTicketReceived;
            mergedOptions.Events.OnTokenResponseReceived += microsoftIdentityOptions.Events.OnTokenResponseReceived;
            mergedOptions.Events.OnTokenValidated += microsoftIdentityOptions.Events.OnTokenValidated;
            mergedOptions.Events.OnUserInformationReceived += microsoftIdentityOptions.Events.OnUserInformationReceived;

            mergedOptions.EventsType ??= microsoftIdentityOptions.EventsType;
            if (string.IsNullOrEmpty(mergedOptions.ForwardAuthenticate) && !string.IsNullOrEmpty(microsoftIdentityOptions.ForwardAuthenticate))
            {
                mergedOptions.ForwardAuthenticate = microsoftIdentityOptions.ForwardAuthenticate;
            }

            if (string.IsNullOrEmpty(mergedOptions.ForwardChallenge) && !string.IsNullOrEmpty(microsoftIdentityOptions.ForwardChallenge))
            {
                mergedOptions.ForwardChallenge = microsoftIdentityOptions.ForwardChallenge;
            }

            if (string.IsNullOrEmpty(mergedOptions.ForwardDefault) && !string.IsNullOrEmpty(microsoftIdentityOptions.ForwardDefault))
            {
                mergedOptions.ForwardDefault = microsoftIdentityOptions.ForwardDefault;
            }

            mergedOptions.ForwardDefaultSelector ??= microsoftIdentityOptions.ForwardDefaultSelector;
            if (string.IsNullOrEmpty(mergedOptions.ForwardForbid) && !string.IsNullOrEmpty(microsoftIdentityOptions.ForwardForbid))
            {
                mergedOptions.ForwardForbid = microsoftIdentityOptions.ForwardForbid;
            }

            if (string.IsNullOrEmpty(mergedOptions.ForwardSignIn) && !string.IsNullOrEmpty(microsoftIdentityOptions.ForwardSignIn))
            {
                mergedOptions.ForwardSignIn = microsoftIdentityOptions.ForwardSignIn;
            }

            if (string.IsNullOrEmpty(mergedOptions.ForwardSignOut) && !string.IsNullOrEmpty(microsoftIdentityOptions.ForwardSignOut))
            {
                mergedOptions.ForwardSignOut = microsoftIdentityOptions.ForwardSignOut;
            }

            mergedOptions.GetClaimsFromUserInfoEndpoint = microsoftIdentityOptions.GetClaimsFromUserInfoEndpoint;
            if (string.IsNullOrEmpty(mergedOptions.Instance) && !string.IsNullOrEmpty(microsoftIdentityOptions.Instance))
            {
                mergedOptions.Instance = microsoftIdentityOptions.Instance;
            }

            mergedOptions.LegacyCacheCompatibilityEnabled = microsoftIdentityOptions.LegacyCacheCompatibilityEnabled;

#if DOTNET_50_AND_ABOVE
            mergedOptions.MapInboundClaims = microsoftIdentityOptions.MapInboundClaims;
#endif

            mergedOptions.MaxAge = microsoftIdentityOptions.MaxAge;
            if (string.IsNullOrEmpty(mergedOptions.MetadataAddress) && !string.IsNullOrEmpty(microsoftIdentityOptions.MetadataAddress))
            {
                mergedOptions.MetadataAddress = microsoftIdentityOptions.MetadataAddress;
            }

            mergedOptions.NonceCookie = microsoftIdentityOptions.NonceCookie;
            if (string.IsNullOrEmpty(mergedOptions.Prompt) && !string.IsNullOrEmpty(microsoftIdentityOptions.Prompt))
            {
                mergedOptions.Prompt = microsoftIdentityOptions.Prompt;
            }

            mergedOptions.ProtocolValidator ??= microsoftIdentityOptions.ProtocolValidator;

#if DOTNET_50_AND_ABOVE
            mergedOptions.RefreshInterval = microsoftIdentityOptions.RefreshInterval;
#endif

            mergedOptions.RefreshOnIssuerKeyNotFound = microsoftIdentityOptions.RefreshOnIssuerKeyNotFound;
            mergedOptions.RemoteAuthenticationTimeout = microsoftIdentityOptions.RemoteAuthenticationTimeout;
            mergedOptions.RemoteSignOutPath = microsoftIdentityOptions.RemoteSignOutPath;
            mergedOptions.RequireHttpsMetadata = microsoftIdentityOptions.RequireHttpsMetadata;
            if (string.IsNullOrEmpty(mergedOptions.ResetPasswordPolicyId) && !string.IsNullOrEmpty(microsoftIdentityOptions.ResetPasswordPolicyId))
            {
                mergedOptions.ResetPasswordPolicyId = microsoftIdentityOptions.ResetPasswordPolicyId;
            }

            if (string.IsNullOrEmpty(mergedOptions.Resource) && !string.IsNullOrEmpty(microsoftIdentityOptions.Resource))
            {
                mergedOptions.Resource = microsoftIdentityOptions.Resource;
            }

            if (string.IsNullOrEmpty(mergedOptions.ResponseMode) && !string.IsNullOrEmpty(microsoftIdentityOptions.ResponseMode))
            {
                mergedOptions.ResponseMode = microsoftIdentityOptions.ResponseMode;
            }

            mergedOptions.ResponseType = microsoftIdentityOptions.ResponseType;

            if (string.IsNullOrEmpty(mergedOptions.ReturnUrlParameter) && !string.IsNullOrEmpty(microsoftIdentityOptions.ReturnUrlParameter))
            {
                mergedOptions.ReturnUrlParameter = microsoftIdentityOptions.ReturnUrlParameter;
            }

            mergedOptions.SaveTokens = microsoftIdentityOptions.SaveTokens;
            mergedOptions.SecurityTokenValidator ??= microsoftIdentityOptions.SecurityTokenValidator;
            mergedOptions.SendX5C = microsoftIdentityOptions.SendX5C;
            mergedOptions.SignedOutCallbackPath = microsoftIdentityOptions.SignedOutCallbackPath;
            if (string.IsNullOrEmpty(mergedOptions.SignedOutRedirectUri) && !string.IsNullOrEmpty(microsoftIdentityOptions.SignedOutRedirectUri))
            {
                mergedOptions.SignedOutRedirectUri = microsoftIdentityOptions.SignedOutRedirectUri;
            }

            if (string.IsNullOrEmpty(mergedOptions.SignInScheme) && !string.IsNullOrEmpty(microsoftIdentityOptions.SignInScheme))
            {
                mergedOptions.SignInScheme = microsoftIdentityOptions.SignInScheme;
            }

            if (string.IsNullOrEmpty(mergedOptions.SignOutScheme) && !string.IsNullOrEmpty(microsoftIdentityOptions.SignOutScheme))
            {
                mergedOptions.SignOutScheme = microsoftIdentityOptions.SignOutScheme;
            }

            if (string.IsNullOrEmpty(mergedOptions.SignUpSignInPolicyId) && !string.IsNullOrEmpty(microsoftIdentityOptions.SignUpSignInPolicyId))
            {
                mergedOptions.SignUpSignInPolicyId = microsoftIdentityOptions.SignUpSignInPolicyId;
            }

            mergedOptions.SkipUnrecognizedRequests = microsoftIdentityOptions.SkipUnrecognizedRequests;
            mergedOptions.StateDataFormat ??= microsoftIdentityOptions.StateDataFormat;
            mergedOptions.StringDataFormat ??= microsoftIdentityOptions.StringDataFormat;
            if (string.IsNullOrEmpty(mergedOptions.TenantId) && !string.IsNullOrEmpty(microsoftIdentityOptions.TenantId))
            {
                mergedOptions.TenantId = microsoftIdentityOptions.TenantId;
            }

            mergedOptions.TokenDecryptionCertificates ??= microsoftIdentityOptions.TokenDecryptionCertificates;
            mergedOptions.TokenValidationParameters = microsoftIdentityOptions.TokenValidationParameters.Clone();
            mergedOptions.UsePkce = microsoftIdentityOptions.UsePkce;
            if (string.IsNullOrEmpty(mergedOptions.UserAssignedManagedIdentityClientId) && !string.IsNullOrEmpty(microsoftIdentityOptions.UserAssignedManagedIdentityClientId))
            {
                mergedOptions.UserAssignedManagedIdentityClientId = microsoftIdentityOptions.UserAssignedManagedIdentityClientId;
            }

            mergedOptions.UseTokenLifetime = microsoftIdentityOptions.UseTokenLifetime;
            mergedOptions._confidentialClientApplicationOptions = null;

            mergedOptions.Scope.Clear();

            foreach (var scope in microsoftIdentityOptions.Scope)
            {
                if (!string.IsNullOrWhiteSpace(scope) && !mergedOptions.Scope.Any(s => string.Equals(s, scope, StringComparison.OrdinalIgnoreCase)))
                {
                    mergedOptions.Scope.Add(scope);
                }
            }
        }

        internal static void UpdateMergedOptionsFromConfidentialClientApplicationOptions(ConfidentialClientApplicationOptions confidentialClientApplicationOptions, MergedOptions mergedOptions)
        {
            mergedOptions.AadAuthorityAudience = confidentialClientApplicationOptions.AadAuthorityAudience;
            mergedOptions.AzureCloudInstance = confidentialClientApplicationOptions.AzureCloudInstance;
            if (string.IsNullOrEmpty(mergedOptions.AzureRegion) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.AzureRegion))
            {
                mergedOptions.AzureRegion = confidentialClientApplicationOptions.AzureRegion;
            }

            mergedOptions.ClientCapabilities ??= confidentialClientApplicationOptions.ClientCapabilities;
            if (string.IsNullOrEmpty(mergedOptions.ClientId) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientId))
            {
                mergedOptions.ClientId = confidentialClientApplicationOptions.ClientId;
            }

            if (string.IsNullOrEmpty(mergedOptions.ClientName) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientName))
            {
                mergedOptions.ClientName = confidentialClientApplicationOptions.ClientName;
            }

            if (string.IsNullOrEmpty(mergedOptions.ClientSecret) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientSecret))
            {
                mergedOptions.ClientSecret = confidentialClientApplicationOptions.ClientSecret;
            }

            if (string.IsNullOrEmpty(mergedOptions.ClientVersion) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientVersion))
            {
                mergedOptions.ClientVersion = confidentialClientApplicationOptions.ClientVersion;
            }

            mergedOptions.EnablePiiLogging = confidentialClientApplicationOptions.EnablePiiLogging;
            if (string.IsNullOrEmpty(mergedOptions.Instance) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.Instance))
            {
                mergedOptions.Instance = confidentialClientApplicationOptions.Instance;
            }

            mergedOptions.IsDefaultPlatformLoggingEnabled = confidentialClientApplicationOptions.IsDefaultPlatformLoggingEnabled;
            // mergedOptions.LegacyCacheCompatibilityEnabled = confidentialClientApplicationOptions.LegacyCacheCompatibilityEnabled; // must be set through id web options
            mergedOptions.EnableCacheSynchronization = confidentialClientApplicationOptions.EnableCacheSynchronization;
            mergedOptions.LogLevel = confidentialClientApplicationOptions.LogLevel;
            if (string.IsNullOrEmpty(mergedOptions.RedirectUri) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.RedirectUri))
            {
                mergedOptions.RedirectUri = confidentialClientApplicationOptions.RedirectUri;
            }

            if (string.IsNullOrEmpty(mergedOptions.TenantId) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.TenantId))
            {
                mergedOptions.TenantId = confidentialClientApplicationOptions.TenantId;
            }

            mergedOptions._confidentialClientApplicationOptions = null;
        }

        internal static void UpdateConfidentialClientApplicationOptionsFromMergedOptions(MergedOptions mergedOptions, ConfidentialClientApplicationOptions confidentialClientApplicationOptions)
        {
            confidentialClientApplicationOptions.AadAuthorityAudience = mergedOptions.AadAuthorityAudience;
            confidentialClientApplicationOptions.AzureCloudInstance = mergedOptions.AzureCloudInstance;
            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.AzureRegion) && !string.IsNullOrEmpty(mergedOptions.AzureRegion))
            {
                confidentialClientApplicationOptions.AzureRegion = mergedOptions.AzureRegion;
            }

            confidentialClientApplicationOptions.ClientCapabilities ??= mergedOptions.ClientCapabilities;
            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientId) && !string.IsNullOrEmpty(mergedOptions.ClientId))
            {
                confidentialClientApplicationOptions.ClientId = mergedOptions.ClientId;
            }

            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientName) && !string.IsNullOrEmpty(mergedOptions.ClientName))
            {
                confidentialClientApplicationOptions.ClientName = mergedOptions.ClientName;
            }

            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientSecret) && !string.IsNullOrEmpty(mergedOptions.ClientSecret))
            {
                confidentialClientApplicationOptions.ClientSecret = mergedOptions.ClientSecret;
            }

            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientVersion) && !string.IsNullOrEmpty(mergedOptions.ClientVersion))
            {
                confidentialClientApplicationOptions.ClientVersion = mergedOptions.ClientVersion;
            }

            confidentialClientApplicationOptions.EnablePiiLogging = mergedOptions.EnablePiiLogging;
            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.Instance) && !string.IsNullOrEmpty(mergedOptions.Instance))
            {
                confidentialClientApplicationOptions.Instance = mergedOptions.Instance;
            }

            confidentialClientApplicationOptions.IsDefaultPlatformLoggingEnabled = mergedOptions.IsDefaultPlatformLoggingEnabled;
            confidentialClientApplicationOptions.LegacyCacheCompatibilityEnabled = mergedOptions.LegacyCacheCompatibilityEnabled;
            confidentialClientApplicationOptions.EnableCacheSynchronization = mergedOptions.EnableCacheSynchronization;
            confidentialClientApplicationOptions.LogLevel = mergedOptions.LogLevel;
            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.RedirectUri) && !string.IsNullOrEmpty(mergedOptions.RedirectUri))
            {
                confidentialClientApplicationOptions.RedirectUri = mergedOptions.RedirectUri;
            }

            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.TenantId) && !string.IsNullOrEmpty(mergedOptions.TenantId))
            {
                confidentialClientApplicationOptions.TenantId = mergedOptions.TenantId;
            }
        }

        internal static void UpdateMergedOptionsFromJwtBearerOptions(JwtBearerOptions jwtBearerOptions, MergedOptions mergedOptions)
        {
            if (string.IsNullOrEmpty(mergedOptions.Authority) && !string.IsNullOrEmpty(jwtBearerOptions.Authority))
            {
                mergedOptions.Authority = jwtBearerOptions.Authority;
            }
        }

        public void PrepareAuthorityInstanceForMsal()
        {
            if (IsB2C && Instance.EndsWith("/tfp/", StringComparison.OrdinalIgnoreCase))
            {
                Instance = Instance.Replace("/tfp/", string.Empty, StringComparison.OrdinalIgnoreCase).TrimEnd('/') + "/";
            }
            else
            {
                Instance = Instance.TrimEnd('/') + "/";
            }
        }
    }
}
