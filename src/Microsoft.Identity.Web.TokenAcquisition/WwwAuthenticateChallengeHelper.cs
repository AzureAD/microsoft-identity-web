// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Internal helper for handling WWW-Authenticate challenges from downstream APIs.
    /// This helper provides shared logic for detecting claims challenges and preparing retry requests.
    /// </summary>
    internal static class WwwAuthenticateChallengeHelper
    {
        /// <summary>
        /// Extracts the claims challenge from WWW-Authenticate response headers.
        /// </summary>
        /// <param name="responseHeaders">The HTTP response headers to examine.</param>
        /// <returns>The claims challenge string if present; otherwise, null.</returns>
        public static string? ExtractClaimsChallenge(HttpResponseHeaders responseHeaders)
        {
            return WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(responseHeaders);
        }

        /// <summary>
        /// Clones HttpContent for retry scenarios. This is necessary because HttpContent can only be
        /// read once, especially with non-seekable streams. The clone allows the content to be sent
        /// again in a retry request.
        /// </summary>
        /// <param name="originalContent">The original HttpContent to clone.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new HttpContent instance with the same data and headers, or null if original was null.</returns>
        /// <remarks>
        /// This method defensively handles content cloning by reading the content into a byte array
        /// and creating new ByteArrayContent. This ensures the content can be sent multiple times,
        /// even if the original stream was non-seekable.
        /// </remarks>
        public static async Task<HttpContent?> CloneHttpContentAsync(
            HttpContent? originalContent,
            CancellationToken cancellationToken = default)
        {
            if (originalContent == null)
            {
                return null;
            }

            // Read the content into a byte array to ensure it can be reused
#if NET
            byte[] contentBytes = await originalContent.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
            byte[] contentBytes = await originalContent.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
            
            // Create new content with the same data
            var clonedContent = new ByteArrayContent(contentBytes);

            // Copy headers from original content
            foreach (var header in originalContent.Headers)
            {
                clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clonedContent;
        }

        /// <summary>
        /// Determines if a response should trigger a claims challenge retry.
        /// </summary>
        /// <param name="response">The HTTP response to evaluate.</param>
        /// <returns>True if the response is a 401 Unauthorized; otherwise, false.</returns>
        /// <remarks>
        /// A 401 Unauthorized response may include a WWW-Authenticate header with a claims challenge.
        /// The actual claims extraction should be done using <see cref="ExtractClaimsChallenge"/>.
        /// </remarks>
        public static bool ShouldAttemptClaimsChallengeRetry(HttpResponseMessage response)
        {
            return response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        }
    }
}
