using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    // TODO: Reconcile with RestDownstreamApiOptions
    internal class TokenAcquisitionAuthenticationProviderOption : IAuthenticationProviderOption
    {
        public string[]? Scopes { get; set; }
        public bool? AppOnly { get; set; }
        public string? Tenant { get; set; }

        public string? AuthenticationScheme { get; set; }
    }
}
