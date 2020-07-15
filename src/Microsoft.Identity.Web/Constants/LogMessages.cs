// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal static class LogMessages
    {
        public const string MissingRoles = "The 'roles' or 'role' claim does not contain roles '{0}' or was not found";
        public const string MissingScopes = "The 'scope' or 'scp' claim does not contain scopes '{0}' or was not found";
        public const string ExceptionOccurredWhenAddingAnAccountToTheCacheFromAuthCode = "Exception occurred while adding an account to the cache from the auth code. ";
    }
}
