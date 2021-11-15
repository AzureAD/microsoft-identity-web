// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Client assertion.
    /// </summary>
    public class ClientAssertion
    {
        /// <summary>
        /// Constructor of a ClientAssertion.
        /// </summary>
        /// <param name="signedAssertion">Signed assertion.</param>
        /// <param name="expiry">Optional expiry.</param>
        public ClientAssertion(string signedAssertion, DateTime? expiry)
        {
            SignedAssertion = signedAssertion;
            Expiry = expiry;
        }

        /// <summary>
        /// Signed assertion.
        /// </summary>
        public string SignedAssertion { get; private set; }

        /// <summary>
        /// Expiry of the client assertion.
        /// </summary>
        public DateTime? Expiry { get; private set; }
    }
}
