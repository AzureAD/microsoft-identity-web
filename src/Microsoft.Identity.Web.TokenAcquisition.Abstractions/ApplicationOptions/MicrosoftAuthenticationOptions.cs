// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory. It has both AAD and B2C configuration attributes.
    /// </summary>
    public class MicrosoftAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the Azure Active Directory instance, e.g. <c>"https://login.microsoftonline.com/"</c>.
        /// </summary>
        public string Instance { get; set; } = null!;

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Authority to use when making OpenIdConnect calls.
        /// </summary>
        public override string? Authority
        {
            get { return _authority ?? $"{Instance}{TenantId}/v2.0"; }
            set { _authority = value; }
        }
        private string? _authority;

        #region Token acquisition
        /// <summary>
        /// Specifies the Azure region. See https://aka.ms/azure-region. By default
        /// the app attempts to detect the Azure region automatically (the default
        /// value is "TryAutoDetect").
        /// </summary>
        public string? AzureRegion { get; set; } = "TryAutoDetect";

        /// <summary>
        /// Specifies the capabilities of the client (for instance {"cp1", "cp2"}). This is
        /// useful to express that the Client is capable of handling claims challenge.
        /// </summary>
        public IEnumerable<string>? ClientCapabilities { get; set; }
        #endregion Token acquisition

        #region AADB2C
        /// <summary>
        /// Gets or sets the domain of the Azure Active Directory tenant, e.g. contoso.onmicrosoft.com.
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Gets or sets the edit profile user flow name for B2C, e.g. b2c_1_edit_profile.
        /// </summary>
        public string? EditProfilePolicyId { get; set; }

        /// <summary>
        /// Gets or sets the sign up or sign in user flow name for B2C, e.g. b2c_1_susi.
        /// </summary>
        public string? SignUpSignInPolicyId { get; set; }

        /// <summary>
        /// Gets or sets the reset password user flow name for B2C, e.g. B2C_1_password_reset.
        /// </summary>
        public string? ResetPasswordPolicyId { get; set; }

        /// <summary>
        /// Gets the default user flow (which is signUpsignIn).
        /// </summary>
        public string? DefaultUserFlow => SignUpSignInPolicyId;

        /// <summary>
        /// Is considered B2C if the attribute SignUpSignInPolicyId is defined.
        /// </summary>
        internal bool IsB2C
        {
            get => !string.IsNullOrWhiteSpace(DefaultUserFlow);
        }
        #endregion AADB2C

        #region web app
        /// <summary>
        /// Sets the ResetPassword route path (from the root of the web site).
        /// Defaults to /MicrosoftIdentity/Account/ResetPassword,
        /// which is the value used by Microsoft.Identity.Web.UI.
        /// If you override it, you need to provide your own controller/actions.
        /// </summary>
        public string ResetPasswordPath { get; set; } = "/MicrosoftIdentity/Account/ResetPassword";

        /// <summary>
        /// Sets the Error route path.
        /// Defaults to the value /MicrosoftIdentity/Account/Error,
        /// which is the value used by Microsoft.Identity.Web.UI.
        /// </summary>
        public string ErrorPath { get; set; } = "/MicrosoftIdentity/Account/Error";
        #endregion web app
    }
}
