// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal class MergedOptionsValidation
    {
        public static void Validate(MergedOptions options)
        {
            // Delegate to shared validation logic
            MicrosoftIdentityOptionsValidation.Validate(options);
        }
    }
}
