// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

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
        /// <param name="assertionRequestOptions"></param>
        /// <returns></returns>
        protected abstract Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions);

        /// <summary>
        /// Client assertion.
        /// </summary>
        private ClientAssertion? _clientAssertion;
        private readonly object _assertionLock = new object();
        private DateTimeOffset? _expiry;

        /// <summary>
        /// Gets whether this provider caches assertion values in Microsoft.Identity.Web.
        /// Defaults to <c>true</c>. Providers backed by a token-acquisition library can override
        /// this to <c>false</c> to use that library's cache and per-request telemetry instead.
        /// </summary>
        /// <remarks>
        /// The policy should remain constant for the lifetime of the provider.
        /// Disabling this cache does not disable caching in the underlying token-acquisition library.
        /// </remarks>
        protected virtual bool CacheSignedAssertion => true;

        /// <summary>
        /// Gets the signed assertion, using the provider's assertion caching policy.
        /// </summary>
        /// <param name="assertionRequestOptions">Input object which is populated by the SDK.</param>
        /// <returns>The signed assertion.</returns>
        public async Task<string> GetSignedAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            bool cacheSignedAssertion = CacheSignedAssertion;
            lock (_assertionLock)
            {
                if (cacheSignedAssertion && _clientAssertion is not null
                    && (_clientAssertion.Expiry is null || DateTimeOffset.Now <= _clientAssertion.Expiry))
                {
                    return _clientAssertion.SignedAssertion;
                }
            }

            ClientAssertion assertion = await GetClientAssertionAsync(assertionRequestOptions).ConfigureAwait(false);
            lock (_assertionLock)
            {
                _expiry = assertion.Expiry;
                if (cacheSignedAssertion)
                {
                    _clientAssertion = assertion;
                }
            }

            return assertion.SignedAssertion;
        }

        /// <summary>
        /// Gets the expiry of the most recent assertion acquired by <see cref="GetSignedAssertionAsync"/>,
        /// or <c>null</c> if no assertion has been acquired or its expiry is unspecified.
        /// Available even when assertion-value caching is disabled.
        /// </summary>
        public DateTimeOffset? Expiry
        {
            get
            {
                lock (_assertionLock)
                {
                    return _expiry;
                }
            }
        }

        /// <summary>
        /// Indicates whether this provider can produce a binding certificate alongside its signed
        /// assertion, enabling the outer confidential client to issue mTLS Proof-of-Possession
        /// tokens. Defaults to <c>false</c>; providers that opt in must override this property to
        /// <c>true</c> and override <see cref="GetSignedAssertionWithBindingAsync"/> to return a
        /// non-null <see cref="ClientSignedAssertion"/>.
        /// </summary>
        public virtual bool SupportsTokenBinding => false;

        /// <summary>
        /// Acquires a signed assertion together with its binding certificate, used by
        /// confidential clients configured for mTLS Proof-of-Possession. Returns <c>null</c>
        /// when the provider does not support token binding (the default).
        /// </summary>
        /// <param name="assertionRequestOptions">Input options populated by MSAL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// The signed assertion paired with its binding certificate, or <c>null</c> if the
        /// provider does not support token binding. Providers that return non-null must also
        /// return <c>true</c> from <see cref="SupportsTokenBinding"/>.
        /// </returns>
        public virtual Task<ClientSignedAssertion?> GetSignedAssertionWithBindingAsync(
            AssertionRequestOptions? assertionRequestOptions,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ClientSignedAssertion?>(null);
    }
}
