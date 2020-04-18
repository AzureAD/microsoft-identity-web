// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory. It has both AAD and B2C configuration attributes.
    /// </summary>
    public class MicrosoftIdentityOptions : OpenIdConnectOptions
    {
        /// <summary>
        /// Gets or sets the Azure Active Directory instance, e.g. "https://login.microsoftonline.com".
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Gets or sets the tenant Id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the domain of the Azure Active Directory tenant, e.g. contoso.onmicrosoft.com.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// In a Web app, gets or sets the RedirectUri (URI where the token will be sent back by
        /// Azure Active Directory or Azure Active Directory B2C)
        /// This property is exclusive with <see cref="RemoteAuthenticationOptions.CallbackPath"/> which should be used preferably if you don't want
        /// to have a different deployed configuration from your developper configuration.
        /// There are cases where RedirectUri is needed, for instance when you use a reverse proxy that transforms https
        /// URLs (external world) to http URLs (inside the protected area). This can also be useful for Web apps running
        /// in containers (for the same reasons)
        /// If you don't specify the RedirectUri, the redirect URI will be computed from the URL on which the app is
        /// deployed and the CallbackPath.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Gets or sets the edit profile user flow name for B2C, e.g. b2c_1_edit_profile.
        /// </summary>
        public string EditProfilePolicyId { get; set; }

        /// <summary>
        /// Gets or sets the sign up or sign in user flow name for B2C, e.g. b2c_1_susi.
        /// </summary>
        public string SignUpSignInPolicyId { get; set; }

        /// <summary>
        /// Gets or sets the reset password user flow name for B2C, e.g. B2C_1_password_reset.
        /// </summary>
        public string ResetPasswordPolicyId { get; set; }

        /// <summary>
        /// Gets the default user flow (which is signUpsignIn).
        /// </summary>
        public string DefaultUserFlow => SignUpSignInPolicyId;

        /// <summary>
        /// Is considered B2C if the attribute SignUpSignInPolicyId is defined.
        /// </summary>
        internal bool IsB2C { get { return !string.IsNullOrWhiteSpace(DefaultUserFlow); } }
    }
}
