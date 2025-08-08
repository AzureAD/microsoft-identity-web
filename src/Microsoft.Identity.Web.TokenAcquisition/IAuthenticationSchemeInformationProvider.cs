// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides information about the effective authentication scheme. If passing null
    /// or string.Empty, this returns the default authentication scheme.
    /// </summary>
    [Obsolete("This interface is obsolete and will be removed in a future version. Use Microsoft.Identity.Abstractions.IAuthenticationSchemeInformationProvider instead.")]
    public interface IAuthenticationSchemeInformationProvider : Abstractions.IAuthenticationSchemeInformationProvider

    {
    }
}
