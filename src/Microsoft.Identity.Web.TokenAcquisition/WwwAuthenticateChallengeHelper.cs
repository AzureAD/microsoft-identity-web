// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Internal helper for handling WWW-Authenticate challenge responses from downstream APIs.
    /// Used by both MicrosoftIdentityMessageHandler and DownstreamApi to handle Conditional Access 
    /// Evaluation (CAE) scenarios consistently.
    /// </summary>
    internal static class WwwAuthenticateChallengeHelper
    {
        /// <summary>
        /// Extracts claims challenge from WWW-Authenticate response headers.
        /// </summary>
        /// <param name="response">The HTTP response message containing WWW-Authenticate headers.</param>
        /// <returns>The claims challenge string if present; otherwise null.</returns>
        public static string? ExtractClaimChallenge(HttpResponseMessage response)
        {
            return WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(response.Headers);
        }

        /// <summary>
        /// Clones an HTTP request message for retry scenarios.
        /// </summary>
        /// <param name="originalRequest">The original HTTP request to clone.</param>
        /// <returns>A cloned HTTP request message with content and headers copied.</returns>
        /// <remarks>
        /// This method defensively clones the request content by reading it into memory.
        /// While most scenarios use seekable content (StringContent, ByteArrayContent),
        /// this approach ensures compatibility with non-seekable streams (StreamContent)
        /// and guarantees the retry request has fresh, readable content.
        /// </remarks>
        public static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage originalRequest)
        {
            var clonedRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);
            
            // Copy headers (skip Authorization as it will be refreshed with new token)
            foreach (var header in originalRequest.Headers)
            {
                if (header.Key != "Authorization")
                {
                    clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Clone content defensively to support non-seekable streams.
            // While most DownstreamApi scenarios use seekable content (StringContent, ByteArrayContent),
            // this ensures compatibility with StreamContent and custom content types.
            if (originalRequest.Content != null)
            {
                var contentBytes = await originalRequest.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                clonedRequest.Content = new ByteArrayContent(contentBytes);
                
                // Copy content headers
                foreach (var header in originalRequest.Content.Headers)
                {
                    clonedRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Copy properties/options (excluding authentication options which will be set separately)
            // Note: We don't copy options to avoid complications with typed keys.
            // Most HttpClient scenarios don't rely on copying all options to retry requests.
#if !NET5_0_OR_GREATER
            foreach (var property in originalRequest.Properties)
            {
                // Skip our authentication options as they will be set separately
                if (!property.Key.Equals("Microsoft.Identity.AuthenticationOptions", StringComparison.Ordinal))
                {
                    clonedRequest.Properties[property.Key] = property.Value;
                }
            }
#endif

            return clonedRequest;
        }
    }
}