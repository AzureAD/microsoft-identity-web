// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
	/// <summary>
	/// Utility methods used by L1/L2 cache.
	/// </summary>
	internal static class Utility
	{
		internal static readonly Stopwatch Watch = Stopwatch.StartNew();

		internal static async Task<MeasureDurationResult> Measure(this Task task)
		{
			if (task == null)
			{
				throw new ArgumentNullException(nameof(task));
			}

			var startTicks = Watch.Elapsed.Ticks;
			await task.ConfigureAwait(false);

			return new MeasureDurationResult(Watch.Elapsed.Ticks - startTicks);
		}

		internal static async Task<MeasureDurationResult<TResult>> Measure<TResult>(this Task<TResult> task)
		{
			if (task == null)
			{
				throw new ArgumentNullException(nameof(task));
			}

			var startTicks = Watch.Elapsed.Ticks;
			var taskResult = await task.ConfigureAwait(false);

			return new MeasureDurationResult<TResult>(taskResult, Watch.Elapsed.Ticks - startTicks);
		}
	}
}
