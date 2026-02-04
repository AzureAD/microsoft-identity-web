// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Internal;

namespace Microsoft.Identity.Web
{
    internal class MergedOptionsValidation
    {
        public static void Validate(MergedOptions options)
        {
            // Delegate to shared validation helper
            var appOptions = ConvertToApplicationOptions(options);
            IdentityOptionsHelpers.ValidateRequiredOptions(appOptions);
        }

        private static MicrosoftIdentityApplicationOptions ConvertToApplicationOptions(MergedOptions options)
        {
            return new MicrosoftIdentityApplicationOptions
            {
                ClientId = options.ClientId,
                Authority = options.Authority,
                Instance = options.Instance,
                TenantId = options.TenantId,
                Domain = options.Domain,
                SignUpSignInPolicyId = options.IsB2C ? (options.SignUpSignInPolicyId ?? options.DefaultUserFlow) : null,
            };
        }
    }
}
