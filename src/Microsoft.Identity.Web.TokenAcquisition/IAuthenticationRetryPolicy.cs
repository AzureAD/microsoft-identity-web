// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Determines whether a failed token acquisition or mTLS request should be retried.
    /// </summary>
    /// <remarks>
    /// Register an implementation in dependency injection to customize token acquisition and
    /// <see cref="MicrosoftIdentityMessageHandler"/> retry decisions. Callers enforce retry limits
    /// and execute retries; the policy only evaluates eligibility.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSingleton&lt;IAuthenticationRetryPolicy, MyAuthenticationRetryPolicy&gt;();
    /// services.AddHttpClient("MyApi").AddMicrosoftIdentityMessageHandler(options =>
    /// {
    ///     options.ProtocolScheme = "MTLS";
    ///     options.BaseUrl = "https://api.example.com";
    /// });
    /// </code>
    /// </example>
    public interface IAuthenticationRetryPolicy
    {
        /// <summary>
        /// Evaluates whether a retry should be attempted based on the authentication failure.
        /// </summary>
        /// <param name="context">The authentication failure and associated certificate context.</param>
        /// <returns>True if a retry should be attempted; otherwise, false.</returns>
        /// <remarks>
        /// A true result also permits callers to report unsuccessful certificate usage when a
        /// certificate is available, allowing the credentials provider to select another certificate.
        /// User challenges requiring interaction are not evaluated by this policy.
        /// </remarks>
        bool ShouldRetry(AuthenticationRetryContext context);
    }
}
