// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web
{
    internal class RequiredScopePolicyProvider : IAuthorizationPolicyProvider
    {
        public RequiredScopePolicyProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IConfiguration _configuration;
        public const string PolicyPrefix = "RequiredScope(";

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return Task.FromResult<AuthorizationPolicy>(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return Task.FromResult<AuthorizationPolicy>(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        // Policies are looked up by string name, so expect 'parameters' (like age)
        // to be embedded in the policy names. This is abstracted away from developers
        // by the more strongly-typed attributes derived from AuthorizeAttribute
        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase) && policyName.IndexOf('|', StringComparison.OrdinalIgnoreCase) > 0)
            {
                string scopeString = policyName[PolicyPrefix.Length..policyName.IndexOf('|', StringComparison.OrdinalIgnoreCase)];
                var scopes = scopeString.Split(',');

                if (string.IsNullOrEmpty(scopeString))
                {
                    string scopeConfigurationKeyName = policyName.Substring(policyName.IndexOf('|', StringComparison.OrdinalIgnoreCase) + 1);

                    scopes = _configuration.GetValue<string>(scopeConfigurationKeyName)?.Split(' ');
                }

                var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme);
                policy.AddRequirements(new ScopeAuthorizationRequirement(scopes));
                return Task.FromResult(policy.Build());
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            return Task.FromResult<AuthorizationPolicy>(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}
