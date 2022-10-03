// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Abstractions
{
    /// <summary>
    /// Result of a token acquisition.
    /// </summary>
    public class AcquireTokenResult
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="expiresOn">Expiration date/time.</param>
        /// <param name="tenantId">Tenant for which the token was acquired.</param>
        /// <param name="idToken">ID Token, in the case of a token for a user.</param>
        /// <param name="scopes">Scopes granted by the IdP.</param>
        /// <param name="correlationId">Correlation ID of the token acquisition.</param>
        public AcquireTokenResult(string accessToken, DateTimeOffset expiresOn, string tenantId, string idToken, IEnumerable<string> scopes, Guid correlationId)
        {
            AccessToken = accessToken;
            ExpiresOn = expiresOn;
            TenantId = tenantId;
            IdToken = idToken;
            Scopes = scopes;
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Access Token that can be used to build an authorization header 
        /// to access protected web APIs. 
        /// </summary>
        /// <seealso cref="IAuthorizationHeaderProvider"/> which creates the authorization
        /// header directly, whatever the protocol.
        public string? AccessToken { get; set; }

        /// <summary>
        /// Gets the point in time in which the Access Token returned in the <see cref="AccessToken"/>
        /// property ceases to be valid. This value is calculated based on current UTC time
        /// measured locally and the value expiresIn received from the service.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; set; }

        /// <summary>
        ///  In the case of Azure AD, gets an identifier for the tenant from which the token was acquired.
        ///  This property will be null if tenant information is not returned by the service or the service
        ///  is not Azure AD.
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets the Id Token if returned by the service or null if no Id Token is returned.
        /// </summary>
        public string? IdToken { get; set; }

        /// <summary>
        /// Gets the scope values effectively granted by the IdP.
        /// </summary>
        public IEnumerable<string>? Scopes { get; set; }

        /// <summary>
        /// Gets the correlation id used for the request.
        /// </summary>
        public Guid CorrelationId { get; set; }
    }
}
