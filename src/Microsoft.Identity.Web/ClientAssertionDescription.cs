// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Description of a client assertion.
    /// </summary>
    public class ClientAssertionDescription
    {
        /// <summary>
        /// Constructor of a ClientAssertionDescription.
        /// </summary>
        /// <param name="clientAssertionProvider">delegate providing the client assertion
        /// when it is necessary.</param>
        public ClientAssertionDescription(Func<ClientAssertion> clientAssertionProvider)
        {
            ClientAssertionProvider = clientAssertionProvider;
        }

        /// <summary>
        /// delegate to get the client assertion.
        /// </summary>
        protected Func<ClientAssertion> ClientAssertionProvider { get; private set; }

        /// <summary>
        /// Client assertion.
        /// </summary>
        private ClientAssertion? _clientAssertion;

        /// <summary>
        /// Get the signed assertion (and refreshes it if needed).
        /// </summary>
        public string SignedAssertion
        {
            get
            {
                if (_clientAssertion == null || (Expiry != null && DateTime.Now > Expiry))
                {
                    _clientAssertion = ClientAssertionProvider();
                }

                return _clientAssertion.SignedAssertion;
            }
        }

        /// <summary>
        /// Expiry of the client assertion.
        /// </summary>
        public DateTime? Expiry
        {
            get
            {
                return _clientAssertion?.Expiry;
            }
        }
    }
}
