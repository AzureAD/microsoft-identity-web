// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe binder for <see cref="MicrosoftIdentityApplicationOptions"/>.
    /// Binds properties from the JSON schema AzureAdSection that are defined
    /// on <see cref="MicrosoftIdentityApplicationOptions"/> and its base classes
    /// (<see cref="MicrosoftEntraApplicationOptions"/> and <see cref="IdentityApplicationOptions"/>).
    /// </summary>
    internal static class MicrosoftIdentityApplicationOptionsBinder
    {
        /// <summary>
        /// Binds the <see cref="MicrosoftIdentityApplicationOptions"/> from the specified configuration section.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(MicrosoftIdentityApplicationOptions options, IConfigurationSection? configurationSection)
        {
            if (configurationSection == null)
            {
                return;
            }

            // IdentityApplicationOptions properties (base class)
            if (configurationSection[nameof(options.ClientId)] is string clientId)
            {
                options.ClientId = clientId;
            }

            if (configurationSection[nameof(options.Authority)] is string authority)
            {
                options.Authority = authority;
            }

            if (configurationSection[nameof(options.EnablePiiLogging)] is string enablePiiLogging &&
                bool.TryParse(enablePiiLogging, out bool enablePiiLoggingValue))
            {
                options.EnablePiiLogging = enablePiiLoggingValue;
            }

            if (configurationSection[nameof(options.Audience)] is string audience)
            {
                options.Audience = audience;
            }

            if (configurationSection[nameof(options.AllowWebApiToBeAuthorizedByACL)] is string allowWebApiToBeAuthorizedByACL &&
                bool.TryParse(allowWebApiToBeAuthorizedByACL, out bool allowWebApiToBeAuthorizedByACLValue))
            {
                options.AllowWebApiToBeAuthorizedByACL = allowWebApiToBeAuthorizedByACLValue;
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

            // Audiences - array binding
            var audiencesSection = configurationSection.GetSection(nameof(options.Audiences));
            if (audiencesSection.Exists())
            {
                var audiences = new List<string>();
                foreach (var child in audiencesSection.GetChildren())
                {
                    if (child.Value is not null)
                    {
                        audiences.Add(child.Value);
                    }
                }

                if (audiences.Count > 0)
                {
                    options.Audiences = audiences;
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

            // MicrosoftEntraApplicationOptions properties (derived class)
            if (configurationSection[nameof(options.Instance)] is string instance)
            {
                options.Instance = instance;
            }

            if (configurationSection[nameof(options.TenantId)] is string tenantId)
            {
                options.TenantId = tenantId;
            }

            if (configurationSection[nameof(options.AzureRegion)] is string azureRegion)
            {
                options.AzureRegion = azureRegion;
            }

            if (configurationSection[nameof(options.SendX5C)] is string sendX5C &&
                bool.TryParse(sendX5C, out bool sendX5CValue))
            {
                options.SendX5C = sendX5CValue;
            }

            // ClientCapabilities - array binding
            var clientCapabilitiesSection = configurationSection.GetSection(nameof(options.ClientCapabilities));
            if (clientCapabilitiesSection.Exists())
            {
                var clientCapabilities = new List<string>();
                foreach (var child in clientCapabilitiesSection.GetChildren())
                {
                    if (child.Value is not null)
                    {
                        clientCapabilities.Add(child.Value);
                    }
                }

                if (clientCapabilities.Count > 0)
                {
                    options.ClientCapabilities = clientCapabilities.ToArray();
                }
            }

            // MicrosoftIdentityApplicationOptions properties (most derived class)
            if (configurationSection[nameof(options.WithSpaAuthCode)] is string withSpaAuthCode &&
                bool.TryParse(withSpaAuthCode, out bool withSpaAuthCodeValue))
            {
                options.WithSpaAuthCode = withSpaAuthCodeValue;
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

            if (configurationSection[nameof(options.ResetPasswordPath)] is string resetPasswordPath)
            {
                options.ResetPasswordPath = resetPasswordPath;
            }

            if (configurationSection[nameof(options.ErrorPath)] is string errorPath)
            {
                options.ErrorPath = errorPath;
            }
        }
    }
}
