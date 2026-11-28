// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.NativeAOT;

internal sealed class NativeRuntime
{
    private readonly object _configureLock = new();
    private NativeEnvironment? _environment;

    internal void Configure()
    {
        var replacement = new NativeEnvironment();
        NativeEnvironment? previous;

        lock (_configureLock)
        {
            previous = _environment;
            _environment = replacement;
        }

        if (previous is not null)
        {
            _ = Task.Run(() => RetireEnvironmentAsync(previous));
        }
    }

    private static async Task RetireEnvironmentAsync(NativeEnvironment environment)
    {
        await Task.Delay(TimeSpan.FromMinutes(15)).ConfigureAwait(false);
        environment.Dispose();
    }
}
