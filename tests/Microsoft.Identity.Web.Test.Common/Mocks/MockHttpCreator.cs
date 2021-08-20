// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Util;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public static class MockHttpCreator
    {
        private static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage(string token = "header.payload.signature", string expiry = "3599")
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"Bearer\",\"expires_in\":\"" + expiry + "\",\"client_info\":\"" + CreateClientInfo() + "\",\"access_token\":\"" + token + "\"}");
        }

        public static HttpResponseMessage CreateSuccessResponseMessage(string successResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(successResponse);
            responseMessage.Content = content;
            return responseMessage;
        }

        private static string CreateClientInfo(string uid = TestConstants.Uid, string utid = TestConstants.Utid)
        {
            return Base64UrlHelpers.Encode("{\"uid\":\"" + uid + "\",\"utid\":\"" + utid + "\"}");
        }

        public static MockHttpMessageHandler CreateInstanceDiscoveryMockHandler(
           string discoveryEndpoint = "https://login.microsoftonline.com/common/discovery/instance",
           string content = TestConstants.DiscoveryJsonResponse)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedUrl = discoveryEndpoint,
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content),
                },
            };
        }

        public static MockHttpMessageHandler CreateClientCredentialTokenHandler(
            string token = "header.payload.signature", string expiresIn = "3599")
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateSuccessfulClientCredentialTokenResponseMessage(token, expiresIn),
            };

            return handler;
        }
    }
}
