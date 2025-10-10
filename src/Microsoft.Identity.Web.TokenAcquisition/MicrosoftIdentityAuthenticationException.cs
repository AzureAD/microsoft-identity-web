// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Exception thrown when authentication fails during HTTP message handling by <see cref="MicrosoftIdentityMessageHandler"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is typically thrown in the following scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><description>No authentication options are configured (neither default nor per-request)</description></item>
    /// <item><description>No scopes are specified in the authentication options</description></item>
    /// <item><description>Token acquisition fails due to authentication provider issues</description></item>
    /// </list>
    /// <para>
    /// <strong>Note on WWW-Authenticate Challenge Handling:</strong>
    /// When a downstream API returns a 401 Unauthorized response with a WWW-Authenticate header containing 
    /// additional claims (e.g., for Conditional Access), the handler automatically extracts these claims using 
    /// <see cref="Microsoft.Identity.Client.WwwAuthenticateParameters"/> and attempts to acquire a new token 
    /// with the requested claims. If this automatic retry succeeds, no exception is thrown. If the retry also 
    /// fails with a 401, the response is returned to the caller without throwing an exception - the caller 
    /// should check the status code. Exceptions are only thrown for token acquisition failures, not for 
    /// HTTP 401 responses themselves.
    /// </para>
    /// <para>
    /// When handling this exception, examine the <see cref="Exception.Message"/> property for specific details
    /// about what caused the authentication failure. If an inner exception is present, it may contain
    /// additional information from the underlying authentication provider.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Typical exception handling pattern:</para>
    /// <code>
    /// try
    /// {
    ///     var response = await httpClient.SendAsync(request, cancellationToken);
    ///     response.EnsureSuccessStatusCode();
    /// }
    /// catch (MicrosoftIdentityAuthenticationException authEx)
    /// {
    ///     // Handle authentication-specific failures
    ///     logger.LogError(authEx, "Authentication failed: {Message}", authEx.Message);
    ///     throw; // Re-throw or handle as appropriate
    /// }
    /// catch (HttpRequestException httpEx)
    /// {
    ///     // Handle other HTTP-related failures
    ///     logger.LogError(httpEx, "HTTP request failed: {Message}", httpEx.Message);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="MicrosoftIdentityMessageHandler"/>
    /// <seealso cref="MicrosoftIdentityMessageHandlerOptions"/>
    public class MicrosoftIdentityAuthenticationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAuthenticationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <example>
        /// <code>
        /// throw new MicrosoftIdentityAuthenticationException(
        ///     "Authentication options must be configured either in default options or per-request using WithAuthenticationOptions().");
        /// </code>
        /// </example>
        public MicrosoftIdentityAuthenticationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAuthenticationException"/> class
        /// with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or <see langword="null"/>
        /// if no inner exception is specified.
        /// </param>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     var authHeader = await headerProvider.CreateAuthorizationHeaderAsync(scopes, options);
        /// }
        /// catch (Exception ex)
        /// {
        ///     throw new MicrosoftIdentityAuthenticationException("Failed to acquire authorization header.", ex);
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Use this constructor when you want to preserve the original exception that caused the authentication failure,
        /// such as exceptions from the underlying token acquisition provider.
        /// </remarks>
        public MicrosoftIdentityAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAuthenticationException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">
        /// The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data 
        /// about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information 
        /// about the source or destination.
        /// </param>
        /// <remarks>
        /// This constructor is called during deserialization to reconstitute the exception object transmitted over a stream.
        /// This constructor is only available in .NET Framework and .NET Standard 2.0 where binary serialization is supported.
        /// </remarks>
        protected MicrosoftIdentityAuthenticationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}