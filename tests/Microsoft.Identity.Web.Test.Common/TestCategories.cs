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

        /// <summary>
        /// Tests that additionally require an IMDSv2 managed identity capable of mTLS
        /// Proof-of-Possession + key attestation (the MSI FIC two-leg tests). These carry both
        /// this trait and <see cref="ManagedIdentity"/>, and run only on the MISEManagedIdentity
        /// pool. Pipelines that run the assembly on another pool (for example the Wilson pool used
        /// by the id4s-official / nightly builds) must exclude them with <c>Category!=MI_FIC_E2E</c>.
        /// </summary>
        public const string ManagedIdentityFic = "MI_FIC_E2E";
    }
}
