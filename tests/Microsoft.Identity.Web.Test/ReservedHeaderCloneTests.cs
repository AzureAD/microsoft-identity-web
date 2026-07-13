// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    // Verifies reserved headers are dropped when a request is cloned (challenge retry / mTLS PoP),
    // consistently with ExtraHeaderParameters handling, and that the reserved X-MS-TOKEN- prefix
    // covers the whole X-MS-TOKEN-* family.
    public class ReservedHeaderCloneTests
    {
        [Theory]
        // Exact reserved names.
        [InlineData("Authorization", true)]
        [InlineData("Cookie", true)]
        [InlineData("Host", true)]
        [InlineData("X-MS-CLIENT-PRINCIPAL", true)]
        [InlineData("x-ms-client-principal-id", true)]
        // AAD token headers stay reserved.
        [InlineData("X-MS-TOKEN-AAD-ID-TOKEN", true)]
        [InlineData("x-ms-token-aad-refresh-token", true)]
        // Any other X-MS-TOKEN- header is reserved by the broadened prefix.
        [InlineData("X-MS-TOKEN-EXAMPLE-ACCESS-TOKEN", true)]
        [InlineData("x-ms-token-example-refresh-token", true)]
        // Forwarded headers stay reserved.
        [InlineData("X-Forwarded-For", true)]
        // Ordinary headers are not reserved.
        [InlineData("Accept", false)]
        [InlineData("X-Custom-Header", false)]
        [InlineData("X-MS-Something-Else", false)]
        [InlineData("", false)]
        public void IsReserved_ClassifiesHeaders(string headerName, bool expected)
        {
            Assert.Equal(expected, ReservedHeaderNames.IsReserved(headerName));
        }

        [Fact]
        public async Task CloneHttpRequestMessageAsync_DropsReservedHeaders_KeepsOrdinaryHeaders()
        {
            // Arrange: an original request carrying Authorization, reserved headers, and an ordinary one.
            var original = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
            original.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "sample-token");
            original.Headers.TryAddWithoutValidation("X-MS-CLIENT-PRINCIPAL", "sample-principal");
            original.Headers.TryAddWithoutValidation("X-MS-TOKEN-EXAMPLE-ACCESS-TOKEN", "sample-token-value");
            original.Headers.TryAddWithoutValidation("X-Forwarded-For", "10.0.0.1");
            original.Headers.TryAddWithoutValidation("X-Custom-Header", "keep-me");

            // Act: invoke the private static clone helper.
            var method = typeof(MicrosoftIdentityMessageHandler).GetMethod(
                "CloneHttpRequestMessageAsync",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var clone = await (Task<HttpRequestMessage>)method!.Invoke(null, new object[] { original })!;

            // Assert: reserved headers (and Authorization, set later by the handler) are not copied.
            Assert.False(clone.Headers.Contains("Authorization"));
            Assert.False(clone.Headers.Contains("X-MS-CLIENT-PRINCIPAL"));
            Assert.False(clone.Headers.Contains("X-MS-TOKEN-EXAMPLE-ACCESS-TOKEN"));
            Assert.False(clone.Headers.Contains("X-Forwarded-For"));
            // Ordinary header survives the clone.
            Assert.True(clone.Headers.Contains("X-Custom-Header"));
        }
    }
}
