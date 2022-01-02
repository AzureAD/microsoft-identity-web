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
            ClientAssertionDescription clientAssertionDescription = new ClientAssertionDescription(
                cancellationToken =>
                {
                    n++;
                    return Task.FromResult(new ClientAssertion(
                        n.ToString(CultureInfo.InvariantCulture),
                        DateTime.Now + TimeSpan.FromSeconds(1)));
                });

            string assertion = await clientAssertionDescription.GetSignedAssertion(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("1", assertion);
            assertion = await clientAssertionDescription.GetSignedAssertion(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("1", assertion);

            await Task.Delay(1000).ConfigureAwait(false);
            assertion = await clientAssertionDescription.GetSignedAssertion(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal("2", assertion);
        }
    }
}
