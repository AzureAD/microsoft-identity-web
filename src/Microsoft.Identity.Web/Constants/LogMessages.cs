// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Constants related to the log messages.
    /// </summary>
    internal static class LogMessages
    {
        public const string MissingRoles = "The 'roles' or 'role' claim does not contain roles '{0}' or was not found";
        public const string MissingScopes = "The 'scope' or 'scp' claim does not contain scopes '{0}' or was not found";
        public const string ExceptionOccurredWhenAddingAnAccountToTheCacheFromAuthCode = "Exception occurred while adding an account to the cache from the auth code. ";

        // Diagnostics
        public const string MethodBegin = "Begin {0}. ";
        public const string MethodEnd = "End {0}. ";

        // Caching
        public const string DeserializingSessionCache = "Deserializing session {0}, cache key {1}. ";
        public const string SessionCacheKeyNotFound = "Cache key {0} not found in session {1}. ";
        public const string SerializingSessionCache = "Serializing session {0}, cache key {1}. ";
        public const string ClearingSessionCache = "Clearing session {0}, cache key {1}. ";

        public const string ErrorAcquiringTokenForOboForWebApi = "Error acquiring a token for obo for a web API - MsalUiRequiredException message is: {0} .";
    }
}
