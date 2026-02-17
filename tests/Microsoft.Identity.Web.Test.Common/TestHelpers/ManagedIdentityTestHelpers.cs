// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    /// <summary>
    /// IMDS version enum for managed identity testing.
    /// </summary>
    public enum ImdsVersion
    {
        /// <summary>
        /// IMDS V1 API.
        /// </summary>
        V1,

        /// <summary>
        /// IMDS V2 API.
        /// </summary>
        V2,
    }

    /// <summary>
    /// User-assigned identity ID types for managed identity testing.
    /// </summary>
    public enum UserAssignedIdentityId
    {
        /// <summary>
        /// No user-assigned identity.
        /// </summary>
        None,

        /// <summary>
        /// Client ID.
        /// </summary>
        ClientId,

        /// <summary>
        /// Resource ID.
        /// </summary>
        ResourceId,

        /// <summary>
        /// Object ID.
        /// </summary>
        ObjectId,
    }
}
