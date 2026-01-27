// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Internal
{
    /// <summary>
    /// AOT-safe binder for <see cref="MicrosoftIdentityApplicationOptions"/>.
    /// </summary>
    internal static class MicrosoftIdentityApplicationOptionsBinder
    {
        /// <summary>
        /// Binds the <see cref="MicrosoftIdentityApplicationOptions"/> from the specified configuration section.
        /// </summary>
        /// <param name="options">The options instance to bind to.</param>
        /// <param name="configurationSection">The configuration section containing the values.</param>
        public static void Bind(MicrosoftIdentityApplicationOptions options, IConfigurationSection? configurationSection)
        {
            if (configurationSection == null)
            {
                return;
            }

            // TODO: Implement hand-written binding logic
            // For now, this is a stub that will be filled in with AOT-safe binding
        }
    }
}
