// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe binder for <see cref="ConfidentialClientApplicationOptions"/>.
    /// </summary>
    internal static class ConfidentialClientApplicationOptionsBinder
    {
        /// <summary>
        /// Binds the <see cref="ConfidentialClientApplicationOptions"/> from the specified configuration section.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(ConfidentialClientApplicationOptions options, IConfigurationSection? configurationSection)
        {
            if (configurationSection == null)
            {
                return;
            }

            // TODO: Implement hand-written binding
            // For now, this is a stub that will be filled in with AOT-safe binding
        }
    }
}
