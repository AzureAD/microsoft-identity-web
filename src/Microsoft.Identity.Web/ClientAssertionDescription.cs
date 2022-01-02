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
    public class ClientAssertionDescription
    {
        /// <summary>
        /// Constructor of a ClientAssertionDescription.
        /// </summary>
        /// <param name="clientAssertionProvider">delegate providing the client assertion
        /// when it is necessary.</param>
        public ClientAssertionDescription(Func<CancellationToken, Task<ClientAssertion>> clientAssertionProvider)
        {
            ClientAssertionProvider = clientAssertionProvider;
        }

        /// <summary>
        /// delegate to get the client assertion.
        /// </summary>
        protected Func<CancellationToken, Task<ClientAssertion>> ClientAssertionProvider { get; set; }

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
                _clientAssertion = await ClientAssertionProvider(cancellationToken).ConfigureAwait(false);
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
