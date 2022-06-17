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
        /// <param name="clientCredential">Client Credentials. (Client Certificate, ...)</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, CredentialDescription clientCredential);

        /// <summary>
        /// Get a token acquirer given a set of application identity options.
        /// 
        /// </summary>
        /// <param name="aadApplicationIdentityOptions">Application configuration.</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(AadApplicationIdentityOptions aadApplicationIdentityOptions);
    }
}
