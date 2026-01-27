// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe binder for <see cref="MicrosoftIdentityOptions"/>.
    /// Binds properties from the JSON schema AzureAdSection that are defined
    /// on <see cref="MicrosoftIdentityOptions"/> and its base class (OpenIdConnectOptions).
    /// </summary>
    internal static class MicrosoftIdentityOptionsBinder
    {
        /// <summary>
        /// Binds the <see cref="MicrosoftIdentityOptions"/> from the specified configuration section.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(MicrosoftIdentityOptions options, IConfigurationSection? configurationSection)
        {
            if (configurationSection == null)
            {
                return;
            }

            // OpenIdConnectOptions properties (base class)
            if (configurationSection[nameof(options.Authority)] is string authority)
            {
                options.Authority = authority;
            }

            if (configurationSection[nameof(options.ClientSecret)] is string clientSecret)
            {
                options.ClientSecret = clientSecret;
            }

            if (configurationSection[nameof(options.ClientId)] is string clientId)
            {
                options.ClientId = clientId;
            }

            // MicrosoftIdentityOptions properties
            if (configurationSection[nameof(options.Instance)] is string instance)
            {
                options.Instance = instance;
            }

            if (configurationSection[nameof(options.TenantId)] is string tenantId)
            {
                options.TenantId = tenantId;
            }

            if (configurationSection[nameof(options.Domain)] is string domain)
            {
                options.Domain = domain;
            }

            if (configurationSection[nameof(options.EditProfilePolicyId)] is string editProfilePolicyId)
            {
                options.EditProfilePolicyId = editProfilePolicyId;
            }

            if (configurationSection[nameof(options.SignUpSignInPolicyId)] is string signUpSignInPolicyId)
            {
                options.SignUpSignInPolicyId = signUpSignInPolicyId;
            }

            if (configurationSection[nameof(options.ResetPasswordPolicyId)] is string resetPasswordPolicyId)
            {
                options.ResetPasswordPolicyId = resetPasswordPolicyId;
            }

            if (configurationSection[nameof(options.LegacyCacheCompatibilityEnabled)] is string legacyCacheCompatibilityEnabled &&
                bool.TryParse(legacyCacheCompatibilityEnabled, out bool legacyCacheCompatibilityEnabledValue))
            {
                options.LegacyCacheCompatibilityEnabled = legacyCacheCompatibilityEnabledValue;
            }

            if (configurationSection[nameof(options.SendX5C)] is string sendX5C &&
                bool.TryParse(sendX5C, out bool sendX5CValue))
            {
                options.SendX5C = sendX5CValue;
            }

            if (configurationSection[nameof(options.WithSpaAuthCode)] is string withSpaAuthCode &&
                bool.TryParse(withSpaAuthCode, out bool withSpaAuthCodeValue))
            {
                options.WithSpaAuthCode = withSpaAuthCodeValue;
            }

            if (configurationSection[nameof(options.AllowWebApiToBeAuthorizedByACL)] is string allowWebApiToBeAuthorizedByACL &&
                bool.TryParse(allowWebApiToBeAuthorizedByACL, out bool allowWebApiToBeAuthorizedByACLValue))
            {
                options.AllowWebApiToBeAuthorizedByACL = allowWebApiToBeAuthorizedByACLValue;
            }

            if (configurationSection[nameof(options.UserAssignedManagedIdentityClientId)] is string userAssignedManagedIdentityClientId)
            {
                options.UserAssignedManagedIdentityClientId = userAssignedManagedIdentityClientId;
            }

            if (configurationSection[nameof(options.ResetPasswordPath)] is string resetPasswordPath)
            {
                options.ResetPasswordPath = resetPasswordPath;
            }

            if (configurationSection[nameof(options.ErrorPath)] is string errorPath)
            {
                options.ErrorPath = errorPath;
            }

            // ExtraQueryParameters - dictionary binding
            var extraQueryParametersSection = configurationSection.GetSection(nameof(options.ExtraQueryParameters));
            if (extraQueryParametersSection.Exists())
            {
                options.ExtraQueryParameters ??= new Dictionary<string, string>();
                foreach (var child in extraQueryParametersSection.GetChildren())
                {
                    if (child.Value is not null)
                    {
                        options.ExtraQueryParameters[child.Key] = child.Value;
                    }
                }
            }

            // ClientCertificates - collection binding using CertificateDescription
            var clientCertificatesSection = configurationSection.GetSection(nameof(options.ClientCertificates));
            if (clientCertificatesSection.Exists())
            {
                var clientCertificates = BindCertificateDescriptionCollection(clientCertificatesSection);
                if (clientCertificates is not null)
                {
                    options.ClientCertificates = clientCertificates;
                }
            }

            // TokenDecryptionCertificates - collection binding using CertificateDescription
            var tokenDecryptionCertificatesSection = configurationSection.GetSection(nameof(options.TokenDecryptionCertificates));
            if (tokenDecryptionCertificatesSection.Exists())
            {
                var tokenDecryptionCertificates = BindCertificateDescriptionCollection(tokenDecryptionCertificatesSection);
                if (tokenDecryptionCertificates is not null)
                {
                    options.TokenDecryptionCertificates = tokenDecryptionCertificates;
                }
            }

            // ClientCredentials - collection binding using CredentialDescriptionBinder
            var clientCredentialsSection = configurationSection.GetSection(nameof(options.ClientCredentials));
            if (clientCredentialsSection.Exists())
            {
                var clientCredentials = CredentialDescriptionBinder.BindCollection(clientCredentialsSection);
                if (clientCredentials is not null)
                {
                    options.ClientCredentials = clientCredentials;
                }
            }

            // TokenDecryptionCredentials - collection binding using CredentialDescriptionBinder
            var tokenDecryptionCredentialsSection = configurationSection.GetSection(nameof(options.TokenDecryptionCredentials));
            if (tokenDecryptionCredentialsSection.Exists())
            {
                var tokenDecryptionCredentials = CredentialDescriptionBinder.BindCollection(tokenDecryptionCredentialsSection);
                if (tokenDecryptionCredentials is not null)
                {
                    options.TokenDecryptionCredentials = tokenDecryptionCredentials;
                }
            }

            // CertificatelessOptions binding for ClientCredentialsUsingManagedIdentity
            var clientCredentialsUsingManagedIdentitySection = configurationSection.GetSection(nameof(options.ClientCredentialsUsingManagedIdentity));
            if (clientCredentialsUsingManagedIdentitySection.Exists())
            {
                options.ClientCredentialsUsingManagedIdentity = BindCertificatelessOptions(clientCredentialsUsingManagedIdentitySection);
            }
        }

        /// <summary>
        /// Binds a collection of <see cref="CertificateDescription"/> from configuration.
        /// </summary>
        private static List<CertificateDescription>? BindCertificateDescriptionCollection(IConfigurationSection configurationSection)
        {
            var list = new List<CertificateDescription>();
            foreach (var child in configurationSection.GetChildren())
            {
                var item = new CertificateDescription();
                BindCertificateDescription(item, child);
                list.Add(item);
            }

            return list.Count > 0 ? list : null;
        }

        /// <summary>
        /// Binds a <see cref="CertificateDescription"/> from configuration.
        /// </summary>
        private static void BindCertificateDescription(CertificateDescription cert, IConfigurationSection configurationSection)
        {
            if (configurationSection[nameof(cert.SourceType)] is string sourceType &&
                System.Enum.TryParse<CertificateSource>(sourceType, ignoreCase: true, out var sourceTypeValue))
            {
                cert.SourceType = sourceTypeValue;
            }

            if (configurationSection[nameof(cert.KeyVaultUrl)] is string keyVaultUrl)
            {
                cert.KeyVaultUrl = keyVaultUrl;
            }

            if (configurationSection[nameof(cert.KeyVaultCertificateName)] is string keyVaultCertificateName)
            {
                cert.KeyVaultCertificateName = keyVaultCertificateName;
            }

            if (configurationSection[nameof(cert.Base64EncodedValue)] is string base64EncodedValue)
            {
                cert.Base64EncodedValue = base64EncodedValue;
            }

            if (configurationSection[nameof(cert.CertificateDiskPath)] is string certificateDiskPath)
            {
                cert.CertificateDiskPath = certificateDiskPath;
            }

            if (configurationSection[nameof(cert.CertificatePassword)] is string certificatePassword)
            {
                cert.CertificatePassword = certificatePassword;
            }

            // CertificateStorePath (e.g. "CurrentUser/My")
            if (configurationSection[nameof(cert.CertificateStorePath)] is string certificateStorePath)
            {
                cert.CertificateStorePath = certificateStorePath;
            }

            // CertificateThumbprint
            if (configurationSection[nameof(cert.CertificateThumbprint)] is string certificateThumbprint)
            {
                cert.CertificateThumbprint = certificateThumbprint;
            }

            // CertificateDistinguishedName
            if (configurationSection[nameof(cert.CertificateDistinguishedName)] is string certificateDistinguishedName)
            {
                cert.CertificateDistinguishedName = certificateDistinguishedName;
            }
        }

        /// <summary>
        /// Binds <see cref="CertificatelessOptions"/> from configuration.
        /// </summary>
        private static CertificatelessOptions BindCertificatelessOptions(IConfigurationSection configurationSection)
        {
            var options = new CertificatelessOptions();

            if (configurationSection[nameof(options.IsEnabled)] is string isEnabled &&
                bool.TryParse(isEnabled, out bool isEnabledValue))
            {
                options.IsEnabled = isEnabledValue;
            }

            if (configurationSection[nameof(options.ManagedIdentityClientId)] is string managedIdentityClientId)
            {
                options.ManagedIdentityClientId = managedIdentityClientId;
            }

            return options;
        }
    }
}
