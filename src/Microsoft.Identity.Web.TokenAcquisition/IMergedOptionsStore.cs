// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    internal interface IMergedOptionsStore
    {
        public MergedOptions Get(string name);
    }
}
