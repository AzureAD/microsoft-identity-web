// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Web.Util;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public static class MockHttpCreator
    {
        private static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage(
            string token = "header.payload.signature",
            string tokenType = "Bearer",
            int expiry = 3599)
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"" + tokenType + "\",\"expires_in\":" + expiry + ",\"client_info\":\"" + CreateClientInfo() + "\",\"access_token\":\"" + token + "\"}");
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
            return Base64UrlHelpers.Encode("{\"uid\":\"" + uid + "\",\"utid\":\"" + utid + "\"}")!;
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
            string token = "header.payload.signature", string tokenType = "Bearer", int expiresIn = 3599)
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateSuccessfulClientCredentialTokenResponseMessage(token, tokenType, expiresIn),
            };

            return handler;
        }

        public static MockHttpMessageHandler CreateHandlerToValidatePostData(
            HttpMethod expectedMethod,
            IDictionary<string, string> expectedPostData)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = expectedMethod,
                ExpectedPostData = expectedPostData,
                ResponseMessage = CreateSuccessfulClientCredentialTokenResponseMessage(),
            };
        }

        public static MockHttpMessageHandler CreateMsiTokenHandler(
            string accessToken,
            string resource = "https://management.azure.com/",
            int expiresIn = 3599)
        {
            string expiresOn = DateTimeOffset.UtcNow
                               .AddSeconds(expiresIn)
                               .ToUnixTimeSeconds()
                               .ToString(CultureInfo.InvariantCulture);

            string json = $@"{{
              ""access_token"": ""{accessToken}"",
              ""expires_in""  : ""{expiresIn}"",
              ""expires_on""  : ""{expiresOn}"",
              ""resource""    : ""{resource}"",
              ""token_type""  : ""Bearer"",
              ""client_id""   : ""client_id""
            }}";

            return new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = CreateSuccessResponseMessage(json)
            };
        }
    }
}
