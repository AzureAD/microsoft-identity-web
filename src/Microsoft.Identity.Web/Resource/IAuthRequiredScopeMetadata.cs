using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// This is the metadata that describes auth scopes. It's the underlying data structure our requirment will look for
    /// in order to detect scopes in the scope claim.
    /// </summary>
    public interface IAuthRequiredScopeMetadata
    {
        IEnumerable<string> AcceptedScopes { get; }

        string RequiredScopesConfigurationKey { get; }
    }
}
