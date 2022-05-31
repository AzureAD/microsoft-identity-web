using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Result of a token acquisition
    /// </summary>
    public interface ITokenAcquirerResult
    {
        /// <summary>
        /// Access Token that can be used as a bearer token to access protected web APIs
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the <see cref="AccessToken"/>
        /// property ceases to be valid. This value is calculated based on current UTC time
        /// measured locally and the value expiresIn received from the service.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; }

        /// <summary>
        ///  Gets an identifier for the Azure AD tenant from which the token was acquired.
        ///  This property will be null if tenant information is not returned by the service.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets the Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public string IdToken { get; }

        /// <summary>
        /// Gets the scope values effectively granted by the IdP.
        /// </summary>
        public IEnumerable<string> Scopes { get; }

        /// <summary>
        /// Gets the correlation id used for the request.
        /// </summary>
        public Guid CorrelationId { get; }
    }
}
