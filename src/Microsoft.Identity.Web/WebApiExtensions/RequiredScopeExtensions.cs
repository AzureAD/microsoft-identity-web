using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Resource;

namespace Microsoft.Identity.Web
{
    static class RequiredScopeExtensions
    {
        /// <summary>
        /// This method adds support for the required scope attribute. It adds a default policy that 
        /// adds a scope requirement. This requirement looks for IAuthScopeMetadata on the current endpoint.
        /// </summary>
        public static IServiceCollection AddRequiredScopeAuthorization(this IServiceCollection services)
        {
            // TODO: Make this idempotent 
            var defaultPolicy = new AuthorizationPolicyBuilder().AddRequirements(new ScopeAuthorizationRequirement()).Build();

            return services.AddAuthorization(o =>
            {
                o.DefaultPolicy = o.DefaultPolicy is null
                                 ? defaultPolicy
                                 : AuthorizationPolicy.Combine(o.DefaultPolicy, defaultPolicy);
            });
        }

        /// <summary>
        /// This method adds metadata to route endpoint to descript required scopes. It's the imperative version of 
        /// the [RequiredScope] attribute.
        /// </summary>
        public static TBuilder RequireScopes<TBuilder>(this TBuilder endpointConventionBuilder, params string[] scopes) where TBuilder : IEndpointConventionBuilder
        {
            return endpointConventionBuilder.WithMetadata(new RequiredScopesMetadata(scopes));
        }

        // This the 
        private sealed class RequiredScopesMetadata : IAuthRequiredScopeMetadata
        {
            public RequiredScopesMetadata(string[] scopes)
            {
                AcceptedScopes = scopes;
            }

            public IEnumerable<string> AcceptedScopes { get; }

            public string RequiredScopesConfigurationKey => throw new NotImplementedException();
        }

        public static AuthorizationPolicyBuilder RequireScopes(this AuthorizationPolicyBuilder builder, params string[] scopes)
        {
            builder.Requirements.Add(new ScopeAuthorizationRequirement(scopes));
            return builder;
        }
    }
}
