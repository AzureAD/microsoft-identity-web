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

        public ITokenAcquirer GetTokenAcquirer(string authority, string region, string clientId, X509Certificate2 certificate)
        {
            throw new NotImplementedException();
        }

        public ITokenAcquirer GetTokenAcquirer(string authenticationScheme)
        {
            throw new NotImplementedException();
        }
    }
}
