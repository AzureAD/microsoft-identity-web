// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MicrosoftIdentityApplicationOptionsC
    {
        private readonly IntPtr _authority;

        /// <summary>
        /// Gets or sets the Azure Active Directory instance, e.g. <c>"https://login.microsoftonline.com/"</c>.
        /// </summary>
        private readonly IntPtr _instance;

        /// <summary>
        /// Gets or sets the tenant ID. If your application is multi-tenant, you can also use "common" if it supports
        /// both work and school, or personal accounts accounts, or "organizations" if your application supports only work 
        /// and school accounts. If your application is single tenant, set this property to the tenant ID or domain name.
        /// If your application works only for Microsoft personal accounts, use "consumers".
        /// </summary>
        private readonly IntPtr _tenantId;

        /// <summary>
        /// 
        /// </summary>
        private readonly IntPtr _audience;

        /// <summary>
        /// 
        /// </summary>
        private readonly IntPtr _audiences;

        private readonly uint _audiencesCount;

        /// <summary>
        /// 
        /// </summary>
        public readonly string? Instance => Marshal.PtrToStringUTF8(_instance);

        /// <summary>
        /// 
        /// </summary>
        public readonly string? TenantId => Marshal.PtrToStringUTF8(_tenantId);

        /// <summary>
        /// In a web API, audience of the tokens that will be accepted by the web API.
        /// <para>If your web API accepts several audiences, see <see cref="Audiences"/>.</para>
        /// </summary>
        /// <remarks>If both Audience and <see cref="Audiences"/>, are expressed, the effective audiences is the
        /// union of these properties.</remarks>
        public readonly string? Audience => Marshal.PtrToStringUTF8(_audience);

        private readonly unsafe int strlen(byte* inputString)
        {
            int len = 0;
            while (*inputString != 0)
            {
                len++;
                inputString++;
            }
            return len;
        }

        private readonly string? AuthorityInternal => Marshal.PtrToStringUTF8(_authority);

        /// <summary>
        /// Gets or sets the Authority to use when making OpenIdConnect calls. By default the authority is computed
        /// from the <see cref="Instance"/> and <see cref="TenantId"/> properties, by concatenating them, and appending "v2.0".
        /// If your authority is not an Azure AD authority, you can set it directly here.
        /// </summary>
        public readonly string? Authority
        {
            get { return AuthorityInternal ?? $"{Instance?.TrimEnd('/')}/{TenantId}/v2.0"; }
        }

        /// <summary>
        /// In a web API, accepted audiences for the tokens received by the web API.
        /// <para>See also <see cref="Audience"/>.</para>
        /// The audience is the intended recipient of the token. You can usually assume that the ApplicationID of your web API
        /// is a valid audience. It can, in general be any of the App ID URIs (or resource identitfier) you defined for your application
        /// during its registration in the Azure portal.
        /// </summary>
        /// <example>
        /// <format type="text/markdown">
        /// <![CDATA[
        /// Here is an example of client credentials in the AzureAd section of the *appsetting.json*. The app will try to use
        /// workload identity federation from Managed identity (when setup and deployed in Azure), and otherwise, will use a certificate
        /// from Key Vault, and otherwise, will use a client secret.
        /// ```json
        ///  "Audiences": [
        ///    "api://a88bb933-319c-41b5-9f04-eff36d985612",
        ///    "a88bb933-319c-41b5-9f04-eff36d985612",
        ///    "https://mydomain.com/myapp"
        ///  ]
        /// ```
        ///   See also https://aka.ms/ms-id-web-certificates.
        /// ]]></format>
        /// </example>
        /// <remarks>If both Audiences and <see cref="Audience"/>, are expressed, the effective audiences is the
        /// union of these properties.</remarks>
        unsafe readonly public string[] Audiences
        {
            get
            {
                string[] audiences = new string[_audiencesCount];

                // Iterate over each element of the byte[]* array
                for (int i = 0; i < _audiencesCount; i++)
                {
                    // Convert each element to a string using Encoding.UTF8.GetString
                    IntPtr head = Marshal.ReadIntPtr(_audiences, i * IntPtr.Size);
                    audiences[i] = Marshal.PtrToStringUTF8(head)!;
                }
                return audiences;
            }
        }
    }
}
