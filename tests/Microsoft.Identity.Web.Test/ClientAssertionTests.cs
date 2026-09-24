// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TestClientAssertion : ClientAssertionProviderBase
    {
        private int _n = 0;

        protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            _n++;
            return Task.FromResult(new ClientAssertion(
                _n.ToString(CultureInfo.InvariantCulture),
                DateTimeOffset.Now + TimeSpan.FromSeconds(1)));
        }
    }

    public class ClientAssertionTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task GetSignedAssertionAsync_RespectsCachePolicyAndPreservesExpiry(bool cache, bool hasExpiry)
        {
            // Arrange
            DateTimeOffset? expiry = hasExpiry ? DateTimeOffset.UtcNow.AddHours(1) : null;
            var provider = new CachePolicyAssertionProvider(cache, expiry);
            var firstOptions = new AssertionRequestOptions();
            var secondOptions = new AssertionRequestOptions();
            Assert.Null(provider.Expiry);

            // Act
            string first = await provider.GetSignedAssertionAsync(firstOptions);
            string second = await provider.GetSignedAssertionAsync(secondOptions);

            // Assert
            Assert.Equal("1", first);
            Assert.Equal(cache ? "1" : "2", second);
            Assert.Equal(cache ? 1 : 2, provider.Requests.Count);
            Assert.Same(firstOptions, provider.Requests[0]);
            if (!cache)
            {
                Assert.Same(secondOptions, provider.Requests[1]);
            }
            Assert.Equal(expiry, provider.Expiry);
        }

        [Fact]
        public async Task GetSignedAssertionAsync_CacheDisabled_DoesNotReturnPreviousAssertionAfterFailure()
        {
            // Arrange
            DateTimeOffset expiry = DateTimeOffset.UtcNow.AddHours(1);
            var provider = new CachePolicyAssertionProvider(false, expiry);
            await provider.GetSignedAssertionAsync(null);
            provider.Failure = new InvalidOperationException("Acquisition failed.");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetSignedAssertionAsync(new AssertionRequestOptions()));

            // Assert
            Assert.Same(provider.Failure, exception);
            Assert.Equal(2, provider.Requests.Count);
            Assert.Equal(expiry, provider.Expiry);
            provider.Failure = null;
            Assert.Equal("3", await provider.GetSignedAssertionAsync(null));
        }

        [Fact]
        public async Task TestClientAssertionAsync()
        {
            TestClientAssertion clientAssertionDescription = new TestClientAssertion();
            AssertionRequestOptions options = new AssertionRequestOptions();

            string assertion = await clientAssertionDescription.GetSignedAssertionAsync(options);

            Assert.Equal("1", assertion);
            assertion = await clientAssertionDescription.GetSignedAssertionAsync(options);
            Assert.Equal("1", assertion);

            Assert.NotNull(clientAssertionDescription.Expiry);
            await Task.Delay(clientAssertionDescription.Expiry.Value - DateTimeOffset.Now + TimeSpan.FromMilliseconds(100));
            assertion = await clientAssertionDescription.GetSignedAssertionAsync(options);
            Assert.Equal("2", assertion);
        }

        [Fact]
        public void Constructor_ValidInput_SetsProperties()
        {
            // Arrange
            var signedAssertion = "assertion";
            var expiry = DateTimeOffset.Now.AddDays(1);

            // Act
            var assertion = new ClientAssertion(signedAssertion, expiry);

            // Assert
            Assert.Equal(signedAssertion, assertion.SignedAssertion);
            Assert.Equal(expiry, assertion.Expiry);
        }

        private sealed class CachePolicyAssertionProvider(bool cache, DateTimeOffset? expiry) : ClientAssertionProviderBase
        {
            public List<AssertionRequestOptions?> Requests { get; } = new();
            public Exception? Failure { get; set; }
            protected override bool CacheSignedAssertion => cache;

            protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
            {
                Requests.Add(assertionRequestOptions);
                return Failure is not null
                    ? Task.FromException<ClientAssertion>(Failure)
                    : Task.FromResult(new ClientAssertion(Requests.Count.ToString(CultureInfo.InvariantCulture), expiry));
            }
        }
    }
}
