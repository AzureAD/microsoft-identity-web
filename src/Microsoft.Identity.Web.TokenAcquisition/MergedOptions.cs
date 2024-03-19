// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Abstractions;
#if !NETSTANDARD2_0 && !NET462 && !NET472
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
#endif
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory. It has both AAD and B2C configuration attributes.
    /// Merges the MicrosoftIdentityWebOptions and the ConfidentialClientApplicationOptions.
    /// </summary>
    internal sealed class MergedOptions : MicrosoftIdentityApplicationOptions
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
        internal bool MergedWithCca { get; set; }
        // This is for supporting for CIAM authorities including custom url domains, see https://github.com/AzureAD/microsoft-identity-web/issues/2690
        internal bool PreserveAuthority { get; set; }
        public string? CallbackPath { get; set; }

        internal static void UpdateMergedOptionsFromMicrosoftIdentityOptions(MicrosoftIdentityOptions microsoftIdentityOptions, MergedOptions mergedOptions)
        {
            if (string.IsNullOrEmpty(mergedOptions.ResetPasswordPolicyId) && !string.IsNullOrEmpty(microsoftIdentityOptions.ResetPasswordPolicyId))
            {
                mergedOptions.ResetPasswordPolicyId = microsoftIdentityOptions.ResetPasswordPolicyId;
            }

            mergedOptions.SendX5C |= microsoftIdentityOptions.SendX5C;
            mergedOptions.WithSpaAuthCode |= microsoftIdentityOptions.WithSpaAuthCode;
            
            // Non ASP.NET Core specific
            if (string.IsNullOrEmpty(mergedOptions.Instance) && !string.IsNullOrEmpty(microsoftIdentityOptions.Instance))
            {
                mergedOptions.Instance = microsoftIdentityOptions.Instance;
            }

            if (microsoftIdentityOptions.ResetPasswordPath != Constants.ResetPasswordPath)
            {
                mergedOptions.ResetPasswordPath = microsoftIdentityOptions.ResetPasswordPath;
            }

            if (microsoftIdentityOptions.ErrorPath != Constants.ErrorPath)
            {
                mergedOptions.ErrorPath = microsoftIdentityOptions.ErrorPath;
            }

            mergedOptions.AllowWebApiToBeAuthorizedByACL |= microsoftIdentityOptions.AllowWebApiToBeAuthorizedByACL;

            if (string.IsNullOrEmpty(mergedOptions.Authority) && !string.IsNullOrEmpty(microsoftIdentityOptions.Authority))
            {
                mergedOptions.Authority = microsoftIdentityOptions.Authority;
            }

            mergedOptions.ClientCredentials ??= microsoftIdentityOptions.ClientCredentials;

            if (mergedOptions.ClientCredentials == null || !mergedOptions.ClientCredentials.Any())
            {
                mergedOptions.ClientCredentials = ComputeFromLegacyClientCredentials(microsoftIdentityOptions).ToList();
            }

            mergedOptions.TokenDecryptionCredentials ??= microsoftIdentityOptions.TokenDecryptionCredentials;

            if (mergedOptions.TokenDecryptionCredentials == null || !mergedOptions.TokenDecryptionCredentials.Any())
            {
                mergedOptions.TokenDecryptionCredentials = ComputeFromLegacyTokenDecryptCredentials(microsoftIdentityOptions);
            }

            if (string.IsNullOrEmpty(mergedOptions.ClientId) && !string.IsNullOrEmpty(microsoftIdentityOptions.ClientId))
            {
                mergedOptions.ClientId = microsoftIdentityOptions.ClientId;
            }

            if (string.IsNullOrEmpty(mergedOptions.Domain) && !string.IsNullOrEmpty(microsoftIdentityOptions.Domain))
            {
                mergedOptions.Domain = microsoftIdentityOptions.Domain;
            }

            if (string.IsNullOrEmpty(mergedOptions.EditProfilePolicyId) && !string.IsNullOrEmpty(microsoftIdentityOptions.EditProfilePolicyId))
            {
                mergedOptions.EditProfilePolicyId = microsoftIdentityOptions.EditProfilePolicyId;
            }

            if (string.IsNullOrEmpty(mergedOptions.SignUpSignInPolicyId) && !string.IsNullOrEmpty(microsoftIdentityOptions.SignUpSignInPolicyId))
            {
                mergedOptions.SignUpSignInPolicyId = microsoftIdentityOptions.SignUpSignInPolicyId;
            }

            if (string.IsNullOrEmpty(mergedOptions.TenantId) && !string.IsNullOrEmpty(microsoftIdentityOptions.TenantId))
            {
                mergedOptions.TenantId = microsoftIdentityOptions.TenantId;
            }

            mergedOptions._confidentialClientApplicationOptions = null;

            if ((mergedOptions.ExtraQueryParameters == null || !mergedOptions.ExtraQueryParameters.Any()) && microsoftIdentityOptions.ExtraQueryParameters != null)
            {
                mergedOptions.ExtraQueryParameters = microsoftIdentityOptions.ExtraQueryParameters;
            }
        }

        internal static void UpdateMergedOptionsFromConfidentialClientApplicationOptions(ConfidentialClientApplicationOptions confidentialClientApplicationOptions, MergedOptions mergedOptions)
        {
            mergedOptions.MergedWithCca = true;
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

            if (string.IsNullOrEmpty(mergedOptions.ClientVersion) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientVersion))
            {
                mergedOptions.ClientVersion = confidentialClientApplicationOptions.ClientVersion;
            }

            mergedOptions.EnablePiiLogging = confidentialClientApplicationOptions.EnablePiiLogging;
            if (string.IsNullOrEmpty(mergedOptions.Instance) && !string.IsNullOrEmpty(confidentialClientApplicationOptions.Instance))
            {
                mergedOptions.Instance = confidentialClientApplicationOptions.Instance;
            }

            mergedOptions.IsDefaultPlatformLoggingEnabled |= confidentialClientApplicationOptions.IsDefaultPlatformLoggingEnabled;
            // mergedOptions.LegacyCacheCompatibilityEnabled |= confidentialClientApplicationOptions.LegacyCacheCompatibilityEnabled; // must be set through id web options
            mergedOptions.EnableCacheSynchronization |= confidentialClientApplicationOptions.EnableCacheSynchronization;
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

            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.ClientVersion) && !string.IsNullOrEmpty(mergedOptions.ClientVersion))
            {
                confidentialClientApplicationOptions.ClientVersion = mergedOptions.ClientVersion;
            }

            confidentialClientApplicationOptions.EnablePiiLogging = mergedOptions.EnablePiiLogging;

            ParseAuthorityIfNecessary(mergedOptions);

            if (string.IsNullOrEmpty(confidentialClientApplicationOptions.Instance) && !string.IsNullOrEmpty(mergedOptions.Instance))
            {
                confidentialClientApplicationOptions.Instance = mergedOptions.Instance;
            }

            confidentialClientApplicationOptions.IsDefaultPlatformLoggingEnabled = mergedOptions.IsDefaultPlatformLoggingEnabled;
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

        internal static void ParseAuthorityIfNecessary(MergedOptions mergedOptions)
        {
            if (string.IsNullOrEmpty(mergedOptions.TenantId) && string.IsNullOrEmpty(mergedOptions.Instance) && !string.IsNullOrEmpty(mergedOptions.Authority))
            {
                string authority = mergedOptions.Authority!.TrimEnd('/');
                int indexTenant = authority.LastIndexOf('/');
                if (indexTenant >= 0)
                {
                    // In CIAM and B2C, customers will use "authority", not Instance and TenantId
                    mergedOptions.Instance = mergedOptions.PreserveAuthority ? mergedOptions.Authority : authority.Substring(0, indexTenant);
                    mergedOptions.TenantId = mergedOptions.PreserveAuthority ? null : authority.Substring(indexTenant + 1);
                }
            }
        }

#if !NETSTANDARD2_0 && !NET462 && !NET472
        internal static void UpdateMergedOptionsFromJwtBearerOptions(JwtBearerOptions jwtBearerOptions, MergedOptions mergedOptions)
        {
            if (string.IsNullOrEmpty(mergedOptions.Authority) && !string.IsNullOrEmpty(jwtBearerOptions.Authority))
            {
                mergedOptions.Authority = jwtBearerOptions.Authority;
            }
        }
#endif

        public void PrepareAuthorityInstanceForMsal()
        {
            if (!string.IsNullOrEmpty(DefaultUserFlow) && Instance.EndsWith("/tfp/", StringComparison.OrdinalIgnoreCase))
            {
#if !NETSTANDARD2_0 && !NET462 && !NET472
                Instance = Instance.Replace("/tfp/", string.Empty, StringComparison.OrdinalIgnoreCase).TrimEnd('/') + "/";
#else
                Instance = Instance.Replace("/tfp/", string.Empty).TrimEnd('/') + "/";
#endif
            }
            else
            {
                Instance = Instance.TrimEnd('/') + "/";
            }
        }

        public bool IsB2C { get => !string.IsNullOrEmpty(DefaultUserFlow); }

        public static void UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(MicrosoftIdentityApplicationOptions microsoftIdentityApplicationOptions, MergedOptions mergedOptions)
        {
            mergedOptions.AllowWebApiToBeAuthorizedByACL |= microsoftIdentityApplicationOptions.AllowWebApiToBeAuthorizedByACL;
            if (string.IsNullOrEmpty(mergedOptions.Authority) && microsoftIdentityApplicationOptions.Authority != "//v2.0" && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Authority))
            {
                mergedOptions.Authority = microsoftIdentityApplicationOptions.Authority;
            }

            if (string.IsNullOrEmpty(mergedOptions.AzureRegion) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.AzureRegion))
            {
                mergedOptions.AzureRegion = microsoftIdentityApplicationOptions.AzureRegion;
            }

            mergedOptions.ClientCapabilities ??= microsoftIdentityApplicationOptions.ClientCapabilities;
            if (string.IsNullOrEmpty(mergedOptions.ClientId) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.ClientId))
            {
                mergedOptions.ClientId = microsoftIdentityApplicationOptions.ClientId;
            }

            if (string.IsNullOrEmpty(mergedOptions.Domain) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Domain))
            {
                mergedOptions.Domain = microsoftIdentityApplicationOptions.Domain;
            }

            if (string.IsNullOrEmpty(mergedOptions.EditProfilePolicyId) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.EditProfilePolicyId))
            {
                mergedOptions.EditProfilePolicyId = microsoftIdentityApplicationOptions.EditProfilePolicyId;
            }

            mergedOptions.EnablePiiLogging |= microsoftIdentityApplicationOptions.EnablePiiLogging;
            if (microsoftIdentityApplicationOptions.ErrorPath != Constants.ErrorPath)
            {
                mergedOptions.ErrorPath = microsoftIdentityApplicationOptions.ErrorPath;
            }

            if (string.IsNullOrEmpty(mergedOptions.Instance) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Instance))
            {
                mergedOptions.Instance = microsoftIdentityApplicationOptions.Instance!;
            }

            if (microsoftIdentityApplicationOptions.ResetPasswordPath != Constants.ResetPasswordPath)
            {
                mergedOptions.ResetPasswordPath = microsoftIdentityApplicationOptions.ResetPasswordPath;
            }

            if (string.IsNullOrEmpty(mergedOptions.ResetPasswordPolicyId) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.ResetPasswordPolicyId))
            {
                mergedOptions.ResetPasswordPolicyId = microsoftIdentityApplicationOptions.ResetPasswordPolicyId;
            }

            mergedOptions.SendX5C |= microsoftIdentityApplicationOptions.SendX5C;
            if (string.IsNullOrEmpty(mergedOptions.SignUpSignInPolicyId) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.SignUpSignInPolicyId))
            {
                mergedOptions.SignUpSignInPolicyId = microsoftIdentityApplicationOptions.SignUpSignInPolicyId;
            }

            if (string.IsNullOrEmpty(mergedOptions.TenantId) && !string.IsNullOrEmpty(microsoftIdentityApplicationOptions.TenantId))
            {
                mergedOptions.TenantId = microsoftIdentityApplicationOptions.TenantId;
            }

            mergedOptions.WithSpaAuthCode |= microsoftIdentityApplicationOptions.WithSpaAuthCode;

            if ((mergedOptions.ClientCredentials == null || !mergedOptions.ClientCredentials.Any()) && microsoftIdentityApplicationOptions.ClientCredentials != null)
            {
                mergedOptions.ClientCredentials = microsoftIdentityApplicationOptions.ClientCredentials;
            }

            if ((mergedOptions.TokenDecryptionCredentials == null || !mergedOptions.TokenDecryptionCredentials.Any()) && microsoftIdentityApplicationOptions.TokenDecryptionCredentials != null)
            {
                mergedOptions.TokenDecryptionCredentials = microsoftIdentityApplicationOptions.TokenDecryptionCredentials;
            }

            if ((mergedOptions.ExtraQueryParameters == null || !mergedOptions.ExtraQueryParameters.Any()) && microsoftIdentityApplicationOptions.ExtraQueryParameters != null)
            {
                mergedOptions.ExtraQueryParameters = microsoftIdentityApplicationOptions.ExtraQueryParameters;
            }
        }

        private static IEnumerable<CredentialDescription> ComputeFromLegacyClientCredentials(MicrosoftIdentityOptions microsoftIdentityOptions)
        {
            // Compatibility with v1 API
            if (microsoftIdentityOptions.ClientCredentialsUsingManagedIdentity != null && microsoftIdentityOptions.ClientCredentialsUsingManagedIdentity.IsEnabled)
            {
                yield return new CredentialDescription { ManagedIdentityClientId = microsoftIdentityOptions.ClientCredentialsUsingManagedIdentity.ManagedIdentityClientId, SourceType = CredentialSource.SignedAssertionFromManagedIdentity };
            }
            if (microsoftIdentityOptions.ClientCertificates != null && microsoftIdentityOptions.ClientCertificates.Any())
            {
                foreach (var cert in microsoftIdentityOptions.ClientCertificates)
                {
                    yield return cert;
                }
            }
            if (!string.IsNullOrEmpty(microsoftIdentityOptions.ClientSecret))
            {
                yield return new CredentialDescription { ClientSecret = microsoftIdentityOptions.ClientSecret, SourceType = CredentialSource.ClientSecret };
            }
        }

        private static IEnumerable<CredentialDescription>? ComputeFromLegacyTokenDecryptCredentials(MicrosoftIdentityOptions microsoftIdentityOptions)
        {
            // Compatibility with v1 API
            if (microsoftIdentityOptions.TokenDecryptionCertificates != null && microsoftIdentityOptions.TokenDecryptionCertificates.Any())
            {
                return microsoftIdentityOptions.TokenDecryptionCertificates;
            }
            return null;
        }
    }
}
