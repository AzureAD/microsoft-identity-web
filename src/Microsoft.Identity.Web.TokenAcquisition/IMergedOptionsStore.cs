// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web, Microsoft.Identity.Web.OWIN
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal interface IMergedOptionsStore
    {
        public MergedOptions Get(string name);
    }
}
