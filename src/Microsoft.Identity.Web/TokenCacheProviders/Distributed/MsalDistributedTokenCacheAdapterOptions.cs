// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
    /// <summary>
    /// Options for the MSAL token cache serialization adapter,
    /// which delegates the serialization to the IDistributedCache implementations
    /// available with .NET Core.
    /// </summary>
    public class MsalDistributedTokenCacheAdapterOptions : DistributedCacheEntryOptions
    {
    }
}
