using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Token acquirer factory.
    /// </summary>
    public interface ITokenAcquirerFactory
    {
        /// <summary>
        /// Get a token acquirer from an authentication scheme (configuration options)
        /// </summary>
        /// <param name="outboudPolicyName">Outbound policy name or application configuration moniker</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string outboudPolicyName);

        /// <summary>
        /// Get a token acquirer given an authority, region, client ID, and certificate
        /// </summary>
        /// <param name="authority">Authority.</param>
        /// <param name="region">Region.</param>
        /// <param name="clientId">Client ID.</param>
        /// <param name="credential">Client Credentials. (Client Certificate, ...)</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, CredentialDescription credential);

        /// <summary>
        /// Get a token acquirer given a set of application identity options.
        /// Note that it's rather recommended to use the override of <see cref="GetTokenAcquirer(string)"/>
        /// 
        /// </summary>
        /// <param name="applicationIdentityOptions">Application configuration.</param>
        /// <returns>A instance of <see cref="ITokenAcquirer"/> that will enable token acquisition.</returns>
        ITokenAcquirer GetTokenAcquirer(ApplicationIdentityOptions applicationIdentityOptions);
    }
}
