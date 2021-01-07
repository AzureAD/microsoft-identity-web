// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides access to get or set the current error status. The default implementation will use TempData and be enabled when run under Development.
    /// </summary>
    public interface ILoginErrorAccessor
    {
        /// <summary>
        /// Gets or sets the error message for the current login.
        /// </summary>
        string? Message { get; set; }

        /// <summary>
        /// Gets whether error messages should be displayed.
        /// </summary>
        bool IsEnabled { get; }
    }
}
