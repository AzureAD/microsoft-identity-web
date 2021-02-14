﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    /// <summary>
    /// Temporary, until the Graph SDK exposes it.
    /// </summary>
    public class Spa
    {
        public IEnumerable<string>? redirectUris { get; set; }
    }
}
