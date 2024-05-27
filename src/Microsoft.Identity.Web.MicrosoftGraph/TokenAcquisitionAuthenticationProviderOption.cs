// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal class TokenAcquisitionAuthenticationProviderOption : IAuthenticationProviderOption
    {
        public string[]? Scopes { get; set; }
        public bool? AppOnly { get; set; }
        public string? Tenant { get; set; }
        public string? AuthenticationScheme { get; set; }
        public Action<AuthorizationHeaderProviderOptions>? AuthorizationHeaderProviderOptions { get; set; }
        public ClaimsPrincipal? User { get; set; }
    }
}
