using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    public interface ITokenAcquirerFactory
    {
        ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, X509Certificate2 certificate);
        ITokenAcquirer GetTokenAcquirer(string authenticationScheme);
    }
}
