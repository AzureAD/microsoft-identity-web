﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace WebAppUiTests
{
    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class UiTestNoParallelization : ICollectionFixture<InstallPlaywrightBrowserFixture>
    {
    }
}
