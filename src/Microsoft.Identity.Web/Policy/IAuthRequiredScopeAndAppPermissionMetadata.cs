using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// This is the metadata that describes required auth scopes and/or required app roles for a given endpoint
    /// in a web API. It's the underlying data structure the requirement <see cref="ScopeAuthorizationRequirement"/> will look for
    /// in order to validate scopes in the scope claims.
    /// </summary>
    public interface IAuthRequiredScopeAndAppPermissionMetadata : IAuthRequiredScopeMetadata
    {
        /// <summary>
        /// App Roles accepted by this Web API
        /// </summary>
        IEnumerable<string>? AcceptedAppPermissions { get; }


        /// <summary>
        /// Fully qualified name of the configuration key containing the required app roles (separated
        /// by spaces).
        /// </summary>
        string? RequiredAppPermissionsConfigurationKey { get;}
    }
}
