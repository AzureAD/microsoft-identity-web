// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Microsoft.Identity.Web
{
    internal class MergedOptionsValidation
    {
        public static void Validate(MergedOptions options)
        {
            if (string.IsNullOrEmpty(options.ClientId))
            {
                throw new ArgumentNullException(options.ClientId, string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.ClientId)));
            }

            if (string.IsNullOrEmpty(options.Authority))
            {
                if (string.IsNullOrEmpty(options.Instance))
                {
                    throw new ArgumentNullException(options.Instance, string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.Instance)));
                }

                if (options.IsB2C)
                {
                    if (string.IsNullOrEmpty(options.Domain))
                    {
                        throw new ArgumentNullException(options.Domain, string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.Domain)));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(options.TenantId))
                    {
                        throw new ArgumentNullException(options.TenantId, string.Format(CultureInfo.InvariantCulture, IDWebErrorMessage.ConfigurationOptionRequired, nameof(options.TenantId)));
                    }
                }
            }
        }
    }
}
