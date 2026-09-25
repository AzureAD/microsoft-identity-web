// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal interface IPerRequestClientAssertionProvider
    {
        Task<string> GetSignedAssertionForRequestAsync(AssertionRequestOptions? assertionRequestOptions);
    }
}
