// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    }
}
