// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace CustomSignedAssertionProviderTests
{
    internal class MyCustomSignedAssertionProvider : ClientAssertionProviderBase
    {
        public MyCustomSignedAssertionProvider(Dictionary<string, object>? properties)
        {
            // Implement logic to extract information from the properties passed in the configuration.
        }

        protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            // Implement logic to get the signed assertion, which is probably going to be a call to a service.
            // This call can be parameterized by using the parameters from the properties arg in the constructor.

            // In this sample code we just create an empty signed assertion and return it.
            var clientAssertion = new ClientAssertion("FakeAssertion", DateTimeOffset.Now);
            return Task.FromResult(clientAssertion);
        }
    }
}
