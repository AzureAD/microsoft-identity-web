// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
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

        public static void ValidateEitherClientCertificateOrClientSecret(
            string? clientSecret,
            IEnumerable<CertificateDescription>? cert)
        {
            if (string.IsNullOrEmpty(clientSecret) && (cert == null))
            {
                throw new MsalClientException(
                    ErrorCodes.MissingClientCredentials,
                    IDWebErrorMessage.ClientSecretAndCertficateNull);
            }
            else if (!string.IsNullOrEmpty(clientSecret) && (cert != null))
            {
                throw new MsalClientException(
                    ErrorCodes.DuplicateClientCredentials,
                    IDWebErrorMessage.BothClientSecretAndCertificateProvided);
            }
        }
    }
}
