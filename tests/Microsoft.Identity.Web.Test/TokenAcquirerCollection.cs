// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    /// <summary>
    /// Disables parallel execution for every test-class that
    /// is decorated with [Collection("Run tests - serial")].
    /// NOTE: tests that rely on TokenAcquirerFactory / TokenAcquisition (and the
    /// IMsalHttpClientFactory mocks they spin-up) share several static / singleton
    /// caches.  If xUnit runs them in parallel those shared objects collide and the
    /// mocks return the wrong handler, producing flaky failures.  Putting them all
    /// in this “serial” collection forces xUnit to execute them one-by-one.
    /// Be cautious: only include tests that really need this, as this will impact
    /// the overall test suite running time
    /// </summary>
    [CollectionDefinition("Run tests - serial", DisableParallelization = true)]
    public sealed class TokenAcquirerCollection : ICollectionFixture<object>
    {
    }
}
