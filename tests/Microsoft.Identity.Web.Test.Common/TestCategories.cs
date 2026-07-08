// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Test.Common
{
    /// <summary>
    /// Well-known xUnit trait categories (used with <c>[Trait("Category", ...)]</c>)
    /// so that specific groups of tests can be included or excluded from a test run
    /// via the VSTest <c>testFilterCriteria</c> (for example <c>Category!=MI_E2E</c>).
    /// </summary>
    public static class TestCategories
    {
        /// <summary>
        /// Tests that require a real Azure managed identity to be assigned to the host
        /// (they call the IMDS endpoint). These pass on the official pipeline, which runs
        /// on VM-based agents with a managed identity, but cannot run on Microsoft-hosted
        /// agents that have no managed identity, so they are filtered out there.
        /// </summary>
        public const string ManagedIdentity = "MI_E2E";
    }
}
