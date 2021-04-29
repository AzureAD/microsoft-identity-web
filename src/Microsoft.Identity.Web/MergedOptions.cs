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
    internal class MergedOptions : ConfidentialClientApplicationOptions, IOptionsForIdentityWeb
    {
        public string? Domain { get; set; }
        public string? EditProfilePolicyId { get; set; }
        public string? SignUpSignInPolicyId { get; set; }
        public string? ResetPasswordPolicyId { get; set; }
        public string? DefaultUserFlow => SignUpSignInPolicyId;
        public IEnumerable<CertificateDescription>? ClientCertificates { get; set; }
        public IEnumerable<CertificateDescription>? TokenDecryptionCertificates { get; set; }
        public bool SendX5C { get; set; }
        public bool AllowWebApiToBeAuthorizedByACL { get; set; }
        public string? UserAssignedManagedIdentityClientId { get; set; }
        public bool HasClientCredentials { get; set; }
        public bool IsB2C { get => !string.IsNullOrWhiteSpace(DefaultUserFlow); }

        public void MergeIdWebOptionsAndCcaOptions(
            MicrosoftIdentityOptions microsoftIdentityOptions,
            ConfidentialClientApplicationOptions confidentialClientApplication,
            MicrosoftIdentityOptions? microsoftIdentityOptionsFallback,
            ConfidentialClientApplicationOptions? confidentialClientApplicationFallback)
        {
            ClientSecret = Merge(microsoftIdentityOptions.ClientSecret, confidentialClientApplication.ClientSecret, microsoftIdentityOptionsFallback?.ClientSecret, confidentialClientApplicationFallback?.ClientSecret);
            AzureRegion = Merge(confidentialClientApplication.AzureRegion, confidentialClientApplicationFallback?.AzureRegion);
            ClientId = Merge(microsoftIdentityOptions.ClientId, confidentialClientApplication.ClientId, microsoftIdentityOptionsFallback?.ClientId, confidentialClientApplicationFallback?.ClientId);
            RedirectUri = Merge(confidentialClientApplication.RedirectUri, confidentialClientApplicationFallback?.RedirectUri);
            Instance = Merge(microsoftIdentityOptions.Instance, confidentialClientApplication.Instance, microsoftIdentityOptionsFallback?.Instance, confidentialClientApplicationFallback?.Instance);
            TenantId = Merge(microsoftIdentityOptions.TenantId, confidentialClientApplication.TenantId, microsoftIdentityOptionsFallback?.TenantId, confidentialClientApplicationFallback?.TenantId);
            Domain = Merge(microsoftIdentityOptions.Domain, microsoftIdentityOptionsFallback?.Domain);
            EditProfilePolicyId = Merge(microsoftIdentityOptions.EditProfilePolicyId, microsoftIdentityOptionsFallback?.EditProfilePolicyId);
            SignUpSignInPolicyId = Merge(microsoftIdentityOptions.SignUpSignInPolicyId, microsoftIdentityOptionsFallback?.SignUpSignInPolicyId);
            ResetPasswordPolicyId = Merge(microsoftIdentityOptions.ResetPasswordPolicyId, microsoftIdentityOptionsFallback?.ResetPasswordPolicyId);
            UserAssignedManagedIdentityClientId = Merge(microsoftIdentityOptions.UserAssignedManagedIdentityClientId, microsoftIdentityOptionsFallback?.UserAssignedManagedIdentityClientId);
            ClientCertificates = microsoftIdentityOptions.ClientCertificates ??= microsoftIdentityOptionsFallback?.ClientCertificates;
            TokenDecryptionCertificates = microsoftIdentityOptions.TokenDecryptionCertificates ??= microsoftIdentityOptionsFallback?.TokenDecryptionCertificates;
            if (microsoftIdentityOptions.LegacyCacheCompatibilityEnabled ||
                (microsoftIdentityOptionsFallback != null && microsoftIdentityOptionsFallback.LegacyCacheCompatibilityEnabled) ||
                confidentialClientApplication.LegacyCacheCompatibilityEnabled ||
                (confidentialClientApplicationFallback != null && confidentialClientApplicationFallback.LegacyCacheCompatibilityEnabled))
            {
                LegacyCacheCompatibilityEnabled = true;
            }

            if (microsoftIdentityOptions.SendX5C || (microsoftIdentityOptionsFallback != null && microsoftIdentityOptionsFallback.SendX5C))
            {
                SendX5C = true;
            }

            if (microsoftIdentityOptions.HasClientCredentials || (microsoftIdentityOptionsFallback != null && microsoftIdentityOptionsFallback.HasClientCredentials))
            {
                HasClientCredentials = true;
            }

            if (microsoftIdentityOptions.AllowWebApiToBeAuthorizedByACL || (microsoftIdentityOptionsFallback != null && microsoftIdentityOptionsFallback.AllowWebApiToBeAuthorizedByACL))
            {
                AllowWebApiToBeAuthorizedByACL = true;
            }
        }

        public string? Merge(string? v1, string? v2, string? v3 = null, string? v4 = null)
        {
            string? result = v1;
            if (string.IsNullOrEmpty(result))
            {
                result = v2;
            }

            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(v3))
            {
                result = v3;
            }

            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(v4))
            {
                result = v4;
            }

            return result;
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
