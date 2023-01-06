// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Description of a client assertion in the application configuration.
    /// See https://aka.ms/ms-id-web/client-assertions.
    /// </summary>
    public abstract class ClientAssertionProviderBase
    {
        /// <summary>
        /// Gets the Client assertion
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task<ClientAssertion> GetClientAssertion(CancellationToken cancellationToken);

        /// <summary>
        /// Client assertion.
        /// </summary>
        private ClientAssertion? _clientAssertion;

        /// <summary>
        /// Get the signed assertion (and refreshes it if needed).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The signed assertion.</returns>
        public async Task<string> GetSignedAssertion(CancellationToken cancellationToken)
        {
            if (_clientAssertion == null || (Expiry != null && DateTimeOffset.Now > Expiry))
            {
                _clientAssertion = await GetClientAssertion(cancellationToken).ConfigureAwait(false);
            }

            return _clientAssertion.SignedAssertion;
        }

        /// <summary>
        /// Expiry of the client assertion.
        /// </summary>
        public DateTimeOffset? Expiry
        {
            get
            {
                return _clientAssertion?.Expiry;
            }
        }
    }
}
