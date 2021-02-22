// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#define AzureSDK

using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace Microsoft.Identity.App.DeveloperCredentials
{
    internal class Metadata
    {
        public string? issuer { get; set; }
    }
}
