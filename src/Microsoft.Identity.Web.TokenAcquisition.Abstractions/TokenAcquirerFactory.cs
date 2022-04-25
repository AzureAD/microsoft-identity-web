using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Factory of a token acquirer.
    /// </summary>
    public class TokenAcquirerFactory : ITokenAcquirerFactory
    {
        /// <summary>
        /// Get the default instance.
        /// </summary>
        static public ITokenAcquirerFactory GetDefaultInstance()
        {
            return new TokenAcquirerFactory();
        }

        IDictionary<string, ITokenAcquirer> tokenAcquirers = new Dictionary<string, ITokenAcquirer>();

        /// <summary>
        /// Get a token acquirer for  a given authority, region, clientId, certificate?
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="region"></param>
        /// <param name="clientId"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, X509Certificate2 certificate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a token described by a specific authentication scheme/configuration
        /// </summary>
        /// <param name="authenticationScheme"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ITokenAcquirer GetTokenAcquirer(string authenticationScheme)
        {
            throw new NotImplementedException();
        }
    }
}
