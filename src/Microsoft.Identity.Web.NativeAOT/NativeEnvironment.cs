// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web.NativeAOT;

internal sealed class NativeEnvironment : IDisposable
{
    private readonly ServiceCollection _services = new();

    public void Dispose()
    {
        _services.Clear();
    }
}
