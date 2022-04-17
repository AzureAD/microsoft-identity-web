using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Token acquirer factory.
    /// </summary>
    public interface ITokenAcquirerFactory
    {
        /// <summary>
        /// Get a token acquirer given an authority, region, client ID, and certificate
        /// </summary>
        /// <param name="authority">Authority.</param>
        /// <param name="region">Region.</param>
        /// <param name="clientId">Client ID.</param>
        /// <param name="certificate">Client Certificate.</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, X509Certificate2 certificate);

        /// <summary>
        /// Get a token acquirer from an authentication scheme (configuration options)
        /// </summary>
        /// <param name="authenticationScheme">Authentication scheme</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string authenticationScheme);
    }
}
