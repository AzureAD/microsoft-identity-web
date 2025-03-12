// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal static class TokenCacheErrorMessage
    {
        public const string InitializeAsyncIsObsolete = "IDW10801: Use Initialize instead. See https://aka.ms/ms-id-web/1.9.0. ";
        public const string ExceptionDeserializingCache = "IDW10802: Exception occurred while deserializing token cache. See https://aka.ms/msal-net-token-cache-serialization general guidance and https://aka.ms/ms-id-web/token-cache-troubleshooting for token cache troubleshooting information.";
        public const string CannotUseDistributedCache = "IDW10803: Do not use a distributed cache for the current configuration. Use an in memory cache instead.";
    }
}
