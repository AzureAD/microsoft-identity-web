// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    ///  Microsoft Identity Web specific exception class for
    ///  use in Blazor or Razor pages to process the user challenge.
    ///  Handles the <see cref="MsalUiRequiredException"/>.
    /// </summary>
    public class MicrosoftIdentityWebChallengeUserException : Exception
    {
        /// <summary>
        /// Exception thrown by MSAL when a user challenge is encountered.
        /// </summary>
        public MsalUiRequiredException MsalUiRequiredException { get; set; }

        /// <summary>
        /// Scopes to request.
        /// </summary>
        public string[] Scopes { get; set; }

        /// <summary>
        /// Specified userflow.
        /// </summary>
        public string? Userflow { get; set; }

        /// <summary>
        /// Handles the user challenge for Blazor or Razor pages.
        /// </summary>
        /// <param name="msalUiRequiredException">Exception thrown by MSAL when a user challenge is encountered.</param>
        /// <param name="scopes">Scopes to request.</param
        /// <param name="userflow">Userflow used in B2C.</param>
        public MicrosoftIdentityWebChallengeUserException(
            MsalUiRequiredException msalUiRequiredException,
            string[] scopes,
            string? userflow = null)
            : base(
                  IDWebErrorMessage.MicrosoftIdentityWebChallengeUserException,
                  msalUiRequiredException)
        {
            MsalUiRequiredException = msalUiRequiredException;
            Scopes = scopes;
            Userflow = userflow;
        }
    }
}
