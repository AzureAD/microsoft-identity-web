// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implements an <see cref="IAuthorizationRequirement"/>
    /// which requires at least one instance of the specified claim type, and, if allowed values are specified,
    /// the claim value must be any of the allowed values.
    /// </summary>
    public class ScopeAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Creates a new instance of <see cref="ScopeAuthorizationRequirement"/>.
        /// </summary>
        /// <param name="allowedValues">The optional list of scope values.</param>
        public ScopeAuthorizationRequirement(IEnumerable<string>? allowedValues = null)
        {
            AllowedValues = allowedValues;
        }

        /// <summary>
        /// Gets the optional list of scope values.
        /// </summary>
        public IEnumerable<string>? AllowedValues { get; }

        /// <summary>
        /// Gets the optional list of scope values from configuration.
        /// </summary>
        public string? RequiredScopesConfigurationKey { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var value = (AllowedValues == null || !AllowedValues.Any())
                ? string.Empty
                : $" and `{ClaimConstants.Scp}` or `{ClaimConstants.Scope}` is one of the following values: ({string.Join("|", AllowedValues)})";

            return $"{nameof(ScopeAuthorizationRequirement)}:Scope={value}";
        }
    }
}
