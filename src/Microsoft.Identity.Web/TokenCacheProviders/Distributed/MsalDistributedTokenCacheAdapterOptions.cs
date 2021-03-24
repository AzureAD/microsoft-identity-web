// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Identity.Web.TokenCacheProviders.Distributed
{
	/// <summary>
	/// Options for the MSAL token cache serialization adapter,
	/// which delegates the serialization to the IDistributedCache implementations
	/// available with .NET Core.
	/// </summary>
	public class MsalDistributedTokenCacheAdapterOptions : DistributedCacheEntryOptions
	{
		/// <summary>
		/// Options of the In Memory (L1) cache.
		/// </summary>
		public MemoryCacheOptions L1CacheOptions { get; set; } = new MemoryCacheOptions()
		{
			SizeLimit = 500 * 1024 * 1024,   // 500 Mb
		};

		/// <summary>
		/// Callback offered to the app to be notified when the L2 cache fails.
		/// This way the app is given the possibility to act on the L2 cache,
		/// for instance, in the case of Redis, to reconnect. This is left to the application as it's
		/// the only one that knows about the real implementation of the L2 cache.
		/// The handler should return <c>true</c> if the cache should try again the operation, and
		/// <c>false</c> otherwise. When <c>true</c> is passed and the retry fails, an exception
		/// will be thrown.
		/// </summary>
		public Func<Exception, bool>? OnL2CacheFailure { get; set; }

		/// <summary>
		/// Value more than 0, less than 1, to set the In Memory (L1) cache
		/// expiration time values relative to the Distributed (L2) cache.
		/// Default is 1.
		/// </summary>
		internal double L1ExpirationTimeRatio { get; set; } = 1;
	}
}
