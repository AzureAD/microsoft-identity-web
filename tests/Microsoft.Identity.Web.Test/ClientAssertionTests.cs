// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ClientAssertionTests
    {
        [Fact]
        public async Task TestClientAssertion()
        {
            int n = 0;
            ClientAssertionProviderBase clientAssertionDescription = new ClientAssertionProviderBase()
            {
                ClientAssertionProvider = (cancellationToken =>
                {
                    n++;
                    return Task.FromResult(new ClientAssertion(
                        n.ToString(CultureInfo.InvariantCulture),
                        DateTimeOffset.Now + TimeSpan.FromSeconds(1)));
                })
            };

            string assertion = await clientAssertionDescription.GetSignedAssertion(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("1", assertion);
            assertion = await clientAssertionDescription.GetSignedAssertion(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("1", assertion);

            await Task.Delay(clientAssertionDescription.Expiry.Value - DateTimeOffset.Now + TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
            assertion = await clientAssertionDescription.GetSignedAssertion(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("2", assertion);
        }
    }
}
