// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Shared validation logic for MicrosoftIdentity options.
    /// Used by both AOT and non-AOT authentication paths.
    /// </summary>
    internal static class MicrosoftIdentityOptionsValidation
    {
        /// <summary>
        /// Validates that the required options are configured.
        /// </summary>
        /// <param name="options">The options to validate (can be MicrosoftIdentityOptions or MergedOptions).</param>
        /// <exception cref="ArgumentNullException">Thrown when required options are missing.</exception>
        public static void Validate(MicrosoftIdentityOptions options)
        {
            if (string.IsNullOrEmpty(options.ClientId))
            {
                throw new ArgumentNullException(
                    nameof(options.ClientId),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        IDWebErrorMessage.ConfigurationOptionRequired,
                        nameof(options.ClientId)));
            }

            if (string.IsNullOrEmpty(options.Authority))
            {
                if (string.IsNullOrEmpty(options.Instance))
                {
                    throw new ArgumentNullException(
                        nameof(options.Instance),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            IDWebErrorMessage.ConfigurationOptionRequired,
                            nameof(options.Instance)));
                }

                if (options.IsB2C)
                {
                    if (string.IsNullOrEmpty(options.Domain))
                    {
                        throw new ArgumentNullException(
                            nameof(options.Domain),
                            string.Format(
                                CultureInfo.InvariantCulture,
                                IDWebErrorMessage.ConfigurationOptionRequired,
                                nameof(options.Domain)));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(options.TenantId))
                    {
                        throw new ArgumentNullException(
                            nameof(options.TenantId),
                            string.Format(
                                CultureInfo.InvariantCulture,
                                IDWebErrorMessage.ConfigurationOptionRequired,
                                nameof(options.TenantId)));
                    }
                }
            }
        }
    }
}
