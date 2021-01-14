// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides access to get or set the current error status.
    /// The default implementation will use TempData and be enabled when run under Development.
    /// </summary>
    public interface ILoginErrorAccessor
    {
        /// <summary>
        /// Gets the error message for the current request.
        /// </summary>
        /// <param name="context">Current <see cref="HttpContext"/>.</param>
        /// <returns>The current error message if available.</returns>
        string? GetMessage(HttpContext context);

        /// <summary>
        /// Sets the error message for the current request.
        /// </summary>
        /// <param name="context">Current <see cref="HttpContext"/>.</param>
        /// <param name="message">Error message to set.</param>
        void SetMessage(HttpContext context, string? message);

        /// <summary>
        /// Gets whether error messages should be displayed.
        /// </summary>
        bool IsEnabled { get; }
    }
}
