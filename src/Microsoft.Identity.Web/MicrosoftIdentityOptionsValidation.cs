// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

        public void ValidateEitherClientCertificateOrClientSecret(
            string clientSecret,
            IEnumerable<CertificateDescription> cert)
        {
            if (string.IsNullOrEmpty(clientSecret) && (cert == null))
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Both client secret & client certificate cannot be null or whitespace, " +
                 "and ONE, must be included in the configuration of the web app when calling a web API. " +
                 "For instance, in the appsettings.json file. ");

                throw new MsalClientException(
                    "missing_client_credentials",
                    msg);
            }
            else if (!string.IsNullOrEmpty(clientSecret) && (cert != null))
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Both Client secret & client certificate, " +
                   "cannot be included in the configuration of the web app when calling a web API. ");

                throw new MsalClientException(
                    "duplicate_client_credentials",
                    msg);
            }
        }
    }
}
