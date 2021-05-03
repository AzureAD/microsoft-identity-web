// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
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

        internal static void UpdateMergedOptionsFromMicrosoftIdentityOptions(MicrosoftIdentityOptions microsoftIdentityOptions, MergedOptions mergedOptions)
        {
            mergedOptions.AccessDeniedPath = microsoftIdentityOptions.AccessDeniedPath;
            mergedOptions.AllowWebApiToBeAuthorizedByACL = microsoftIdentityOptions.AllowWebApiToBeAuthorizedByACL;
            mergedOptions.AuthenticationMethod = microsoftIdentityOptions.AuthenticationMethod;
            mergedOptions.Authority ??= microsoftIdentityOptions.Authority;
            mergedOptions.Backchannel ??= microsoftIdentityOptions.Backchannel;
            mergedOptions.BackchannelHttpHandler ??= microsoftIdentityOptions.BackchannelHttpHandler;
            mergedOptions.BackchannelTimeout = microsoftIdentityOptions.BackchannelTimeout;
            mergedOptions.CallbackPath = microsoftIdentityOptions.CallbackPath;
            mergedOptions.ClaimsIssuer ??= microsoftIdentityOptions.ClaimsIssuer;
            mergedOptions.ClientCertificates ??= microsoftIdentityOptions.ClientCertificates;
            mergedOptions.ClientId ??= microsoftIdentityOptions.ClientId;
            mergedOptions.ClientSecret ??= microsoftIdentityOptions.ClientSecret;
            mergedOptions.Configuration ??= microsoftIdentityOptions.Configuration;
            mergedOptions.ConfigurationManager ??= microsoftIdentityOptions.ConfigurationManager;
            mergedOptions.CorrelationCookie ??= microsoftIdentityOptions.CorrelationCookie;
            mergedOptions.DataProtectionProvider ??= microsoftIdentityOptions.DataProtectionProvider;
            mergedOptions.DisableTelemetry = microsoftIdentityOptions.DisableTelemetry;
            mergedOptions.Domain ??= microsoftIdentityOptions.Domain;
            mergedOptions.EditProfilePolicyId ??= microsoftIdentityOptions.EditProfilePolicyId;
            mergedOptions.Events ??= microsoftIdentityOptions.Events;
            mergedOptions.Events ??= microsoftIdentityOptions.Events;
            mergedOptions.Events ??= microsoftIdentityOptions.Events;
            mergedOptions.EventsType ??= microsoftIdentityOptions.EventsType;
            mergedOptions.ForwardAuthenticate ??= microsoftIdentityOptions.ForwardAuthenticate;
            mergedOptions.ForwardChallenge ??= microsoftIdentityOptions.ForwardChallenge;
            mergedOptions.ForwardDefault ??= microsoftIdentityOptions.ForwardDefault;
            mergedOptions.ForwardDefaultSelector ??= microsoftIdentityOptions.ForwardDefaultSelector;
            mergedOptions.ForwardForbid ??= microsoftIdentityOptions.ForwardForbid;
            mergedOptions.ForwardSignIn ??= microsoftIdentityOptions.ForwardSignIn;
            mergedOptions.ForwardSignOut ??= microsoftIdentityOptions.ForwardSignOut;
            mergedOptions.GetClaimsFromUserInfoEndpoint = microsoftIdentityOptions.GetClaimsFromUserInfoEndpoint;
            mergedOptions.Instance ??= microsoftIdentityOptions.Instance;
            mergedOptions.LegacyCacheCompatibilityEnabled = microsoftIdentityOptions.LegacyCacheCompatibilityEnabled;
            mergedOptions.MaxAge = microsoftIdentityOptions.MaxAge;
            mergedOptions.MetadataAddress ??= microsoftIdentityOptions.MetadataAddress;
            mergedOptions.NonceCookie ??= microsoftIdentityOptions.NonceCookie;
            mergedOptions.Prompt ??= microsoftIdentityOptions.Prompt;
            mergedOptions.ProtocolValidator ??= microsoftIdentityOptions.ProtocolValidator;
            mergedOptions.RefreshOnIssuerKeyNotFound = microsoftIdentityOptions.RefreshOnIssuerKeyNotFound;
            mergedOptions.RemoteAuthenticationTimeout = microsoftIdentityOptions.RemoteAuthenticationTimeout;
            mergedOptions.RemoteSignOutPath = microsoftIdentityOptions.RemoteSignOutPath;
            mergedOptions.RequireHttpsMetadata = microsoftIdentityOptions.RequireHttpsMetadata;
            mergedOptions.ResetPasswordPolicyId ??= microsoftIdentityOptions.ResetPasswordPolicyId;
            mergedOptions.Resource ??= microsoftIdentityOptions.Resource;
            mergedOptions.ResponseMode ??= microsoftIdentityOptions.ResponseMode;
            mergedOptions.ResponseType ??= microsoftIdentityOptions.ResponseType;
            mergedOptions.ReturnUrlParameter ??= microsoftIdentityOptions.ReturnUrlParameter;
            mergedOptions.SaveTokens = microsoftIdentityOptions.SaveTokens;
            mergedOptions.SecurityTokenValidator ??= microsoftIdentityOptions.SecurityTokenValidator;
            mergedOptions.SendX5C = microsoftIdentityOptions.SendX5C;
            mergedOptions.SignedOutCallbackPath = microsoftIdentityOptions.SignedOutCallbackPath;
            mergedOptions.SignedOutRedirectUri ??= microsoftIdentityOptions.SignedOutRedirectUri;
            mergedOptions.SignInScheme ??= microsoftIdentityOptions.SignInScheme;
            mergedOptions.SignOutScheme ??= microsoftIdentityOptions.SignOutScheme;
            mergedOptions.SignUpSignInPolicyId ??= microsoftIdentityOptions.SignUpSignInPolicyId;
            mergedOptions.SkipUnrecognizedRequests = microsoftIdentityOptions.SkipUnrecognizedRequests;
            mergedOptions.StateDataFormat ??= microsoftIdentityOptions.StateDataFormat;
            mergedOptions.StringDataFormat ??= microsoftIdentityOptions.StringDataFormat;
            mergedOptions.TenantId ??= microsoftIdentityOptions.TenantId;
            mergedOptions.TokenDecryptionCertificates ??= microsoftIdentityOptions.TokenDecryptionCertificates;
            mergedOptions.TokenValidationParameters ??= microsoftIdentityOptions.TokenValidationParameters;
            mergedOptions.UsePkce = microsoftIdentityOptions.UsePkce;
            mergedOptions.UserAssignedManagedIdentityClientId ??= microsoftIdentityOptions.UserAssignedManagedIdentityClientId;
            mergedOptions.UseTokenLifetime = microsoftIdentityOptions.UseTokenLifetime;
        }

        internal static void UpdateMergedOptionsFromConfidentialClientApplicationOptions(ConfidentialClientApplicationOptions confidentialClientApplicationOptions, MergedOptions mergedOptions)
        {
            mergedOptions.AadAuthorityAudience = confidentialClientApplicationOptions.AadAuthorityAudience;
            mergedOptions.AzureCloudInstance = confidentialClientApplicationOptions.AzureCloudInstance;
            mergedOptions.AzureRegion ??= confidentialClientApplicationOptions.AzureRegion;
            mergedOptions.ClientCapabilities ??= confidentialClientApplicationOptions.ClientCapabilities;
            mergedOptions.ClientId ??= confidentialClientApplicationOptions.ClientId;
            mergedOptions.ClientName ??= confidentialClientApplicationOptions.ClientName;
            mergedOptions.ClientSecret ??= confidentialClientApplicationOptions.ClientSecret;
            mergedOptions.ClientVersion ??= confidentialClientApplicationOptions.ClientVersion;
            mergedOptions.EnablePiiLogging = confidentialClientApplicationOptions.EnablePiiLogging;
            mergedOptions.Instance ??= confidentialClientApplicationOptions.Instance;
            mergedOptions.IsDefaultPlatformLoggingEnabled = confidentialClientApplicationOptions.IsDefaultPlatformLoggingEnabled;
            mergedOptions.LegacyCacheCompatibilityEnabled = confidentialClientApplicationOptions.LegacyCacheCompatibilityEnabled;
            mergedOptions.LogLevel = confidentialClientApplicationOptions.LogLevel;
            mergedOptions.RedirectUri ??= confidentialClientApplicationOptions.RedirectUri;
            mergedOptions.TenantId ??= confidentialClientApplicationOptions.TenantId;
        }

        internal static void UpdateConfidentialClientApplicationOptionsFromMergedOptions(MergedOptions mergedOptions, ConfidentialClientApplicationOptions confidentialClientApplicationOptions)
        {
            confidentialClientApplicationOptions.AadAuthorityAudience = mergedOptions.AadAuthorityAudience;
            confidentialClientApplicationOptions.AzureCloudInstance = mergedOptions.AzureCloudInstance;
            confidentialClientApplicationOptions.AzureRegion ??= mergedOptions.AzureRegion;
            confidentialClientApplicationOptions.ClientCapabilities ??= mergedOptions.ClientCapabilities;
            confidentialClientApplicationOptions.ClientId ??= mergedOptions.ClientId;
            confidentialClientApplicationOptions.ClientName ??= mergedOptions.ClientName;
            confidentialClientApplicationOptions.ClientSecret ??= mergedOptions.ClientSecret;
            confidentialClientApplicationOptions.ClientVersion ??= mergedOptions.ClientVersion;
            confidentialClientApplicationOptions.EnablePiiLogging = mergedOptions.EnablePiiLogging;
            confidentialClientApplicationOptions.Instance ??= mergedOptions.Instance;
            confidentialClientApplicationOptions.IsDefaultPlatformLoggingEnabled = mergedOptions.IsDefaultPlatformLoggingEnabled;
            confidentialClientApplicationOptions.LegacyCacheCompatibilityEnabled = mergedOptions.LegacyCacheCompatibilityEnabled;
            confidentialClientApplicationOptions.LogLevel = mergedOptions.LogLevel;
            confidentialClientApplicationOptions.RedirectUri ??= mergedOptions.RedirectUri;
            confidentialClientApplicationOptions.TenantId ??= mergedOptions.TenantId;
        }

        public void PrepareAuthorityInstanceForMsal()
        {
            if (IsB2C && Instance.EndsWith("/tfp/"))
            {
                Instance = Instance.Replace("/tfp/", string.Empty).TrimEnd('/') + "/";
            }
            else
            {
                Instance = Instance.TrimEnd('/') + "/";
            }
        }
    }
}
