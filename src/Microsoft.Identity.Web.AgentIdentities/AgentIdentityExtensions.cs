// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions to read agent identity-related claims.
    /// </summary>
    public static class AgentIdentityExtensions
    {
        /// <summary>
        /// Claim type for the parent agent blueprint.
        /// </summary>
        private const string XmsParAppAzp = "xms_par_app_azp";

        /// <summary>
        /// Claim type for subject function codes (space-separated integers).
        /// </summary>
        private const string XmsSubFct = "xms_sub_fct";

        /// <summary>
        /// Subject facet value for agent identity user.
        /// </summary>
        private const int AgentIdUser = 13;

        /// <summary>
        /// Retrieves the parent agent blueprint (xms_par_app_azp) value from a ClaimsPrincipal, if present.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>The value of the xms_par_app_azp claim if it exists; otherwise, null.</returns>
        public static string? GetParentAgentBlueprint(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal is null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            return claimsPrincipal.FindFirst(XmsParAppAzp)?.Value;
        }

        /// <summary>
        /// Retrieves the parent agent blueprint (xms_par_app_azp) value from a ClaimsIdentity, if present.
        /// </summary>
        /// <param name="identity">The claims identity.</param>
        /// <returns>The value of the xms_par_app_azp claim if it exists; otherwise, null.</returns>
        public static string? GetParentAgentBlueprint(this ClaimsIdentity identity)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return identity.FindFirst(XmsParAppAzp)?.Value;
        }

        /// <summary>
        /// Determines whether the ClaimsPrincipal represents an agent user identity.
        /// True if the xms_sub_fct claim exists, is a space-separated string of integers,
        /// and that collection contains the agent identity user facet.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>True if xms_sub_fct contains the agent identity user facet and all tokens are integers; otherwise false.</returns>
        public static bool IsAgentUserIdentity(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal is null)
            {
                throw new ArgumentNullException(nameof(claimsPrincipal));
            }

            var value = claimsPrincipal.FindFirst(XmsSubFct)?.Value;
            return ContainsSubjectFacet(value, AgentIdUser);
        }

        /// <summary>
        /// Determines whether the ClaimsIdentity represents an agent user identity.
        /// True if the xms_sub_fct claim exists, is a space-separated string of integers,
        /// and that collection contains the agent identity user facet.
        /// </summary>
        /// <param name="identity">The claims identity.</param>
        /// <returns>True if xms_sub_fct contains the agent identity user facet and all tokens are integers; otherwise false.</returns>
        public static bool IsAgentUserIdentity(this ClaimsIdentity identity)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            var value = identity.FindFirst(XmsSubFct)?.Value;
            return ContainsSubjectFacet(value, AgentIdUser);
        }

        /// <summary>
        /// Parses a claim string representing a space-separated collection of integers and checks for a target subject facet.
        /// Returns true only if all tokens are valid integers and one equals the target facet.
        /// </summary>
        private static bool ContainsSubjectFacet(string? raw, int targetFacet)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            // After the null check, raw is guaranteed to be non-null
            var tokens = raw!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries); // split on whitespace
            bool found = false;

            foreach (var token in tokens)
            {
                if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                {
                    // If any token is non-integer, the claim is not a valid collection of integers.
                    return false;
                }

                if (n == targetFacet)
                {
                    found = true;
                }
            }

            return found;
        }
    }
}
