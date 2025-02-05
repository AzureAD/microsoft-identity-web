// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace CustomSignedAssertionProviderTests
{
    internal class MyCustomSignedAssertionProvider : ClientAssertionProviderBase
    {
        public MyCustomSignedAssertionProvider(Dictionary<string, object>? properties)
        {
            // Implement the logic to extract what you need from the properties passed in the configuration
        }

        protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
        {
            // Implement the logic to get the signed assertion, which is probably going to be a call to a service.
            // This call can be parameterized by the parameters in the properties of the constructor.

            // In this sample code we just create an empty signed assertion and return it.
            var clientAssertion = new ClientAssertion("FakeAssertion", DateTimeOffset.Now);
            return Task.FromResult(clientAssertion);
        }
    }
}
