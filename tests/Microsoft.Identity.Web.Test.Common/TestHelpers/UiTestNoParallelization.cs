// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class UiTestNoParallelization
    {
    }
}
