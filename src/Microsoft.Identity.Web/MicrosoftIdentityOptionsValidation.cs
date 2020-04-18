// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal class MicrosoftIdentityOptionsValidation : IValidateOptions<MicrosoftIdentityOptions>
    {
        public ValidateOptionsResult Validate(string name, MicrosoftIdentityOptions options)
        {
            if (string.IsNullOrEmpty(options.ClientId))
            {
                return ValidateOptionsResult.Fail($"The '{nameof(options.ClientId)}' option must be provided.");
            }

            if (string.IsNullOrEmpty(options.Instance))
            {
                return ValidateOptionsResult.Fail($"The '{nameof(options.Instance)}' option must be provided.");
            }

            if (options.IsB2C)
            {
                if (string.IsNullOrEmpty(options.Domain))
                {
                    return ValidateOptionsResult.Fail($"The '{nameof(options.Domain)}' option must be provided.");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(options.TenantId))
                {
                    return ValidateOptionsResult.Fail($"The '{nameof(options.TenantId)}' option must be provided.");
                }
            }

            return ValidateOptionsResult.Success;
        }

        public ValidateOptionsResult ValidateClientSecret(ConfidentialClientApplicationOptions options)
        {
            if (string.IsNullOrEmpty(options.ClientSecret))
            {
                return ValidateOptionsResult.Fail($"The '{nameof(options.ClientSecret)}' option must be provided.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
