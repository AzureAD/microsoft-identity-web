// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
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

        public static HttpResponseMessage GetLrOboTokenResponse(string scopes, string accessToken = "header.payload.signature", string refreshToken = "header.payload.signatureRt")
        {
            return CreateSuccessResponseMessage(
          "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"refresh_in\":\"2400\",\"scope\":" +
          "\"" + scopes + "\",\"access_token\":\"" + accessToken + "\"" +
          ",\"refresh_token\":\"" + refreshToken + "\",\"client_info\"" +
          ":\"" + CreateClientInfo() + "\",\"id_token\"" +
          ":\"" + CreateIdToken("UniqueId", "DisplayableId") + "\"}");
        }

        public static string CreateIdToken(string uniqueId, string displayableId)
        {
            return CreateIdToken(uniqueId, displayableId, TestConstants.Utid);
        }

        public static string CreateIdToken(string uniqueId, string displayableId, string tenantId)
        {
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"https://login.microsoftonline.com/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/v2.0/\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Marrrrrio Bossy\"," +
                        "\"oid\": \"" + uniqueId + "\"," +
                        "\"preferred_username\": \"" + displayableId + "\"," +
                        "\"sub\": \"K4_SGGxKqW1SxUAmhg6C1F6VPiFzcx-Qd80ehIEdFus\"," +
                        "\"tid\": \"" + tenantId + "\"," +
                        "\"ver\": \"2.0\"}";
            return string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", Base64UrlHelpers.Encode(id));
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

        public static MockHttpMessageHandler CreateLrOboTokenHandler(
            string scopes, string accessToken = "header.payload.signature", string refreshToken = "header.payload.signatureRt")
        {
            var handler = new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = GetLrOboTokenResponse(scopes, accessToken, refreshToken),
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

        /// <summary>
        /// Creates a mock HTTP handler for IMDS probe failure scenarios.
        /// This is used to test managed identity flows when IMDS is unavailable.
        /// </summary>
        /// <param name="imdsVersion">The IMDS API version to mock.</param>
        /// <param name="userAssignedIdentityId">Optional user-assigned identity ID type.</param>
        /// <param name="userAssignedId">Optional user-assigned identity ID value.</param>
        /// <returns>A mock HTTP message handler configured to simulate IMDS probe failure.</returns>
        public static MockHttpMessageHandler MockImdsProbeFailure(
            ImdsVersion imdsVersion,
            UserAssignedIdentityId? userAssignedIdentityId = null,
            string? userAssignedId = null)
        {
            // IMDS probe failure returns 404 Not Found
            const string ImdsBaseEndpoint = "http://169.254.169.254";
            string endpoint;

            switch (imdsVersion)
            {
                case ImdsVersion.V2:
                    endpoint = "/metadata/instance/compute/attestedData/csr";
                    break;

                case ImdsVersion.V1:
                default:
                    endpoint = "/metadata/identity/oauth2/token";
                    break;
            }

            return new MockHttpMessageHandler
            {
                ExpectedUrl = $"{ImdsBaseEndpoint}{endpoint}",
                ExpectedMethod = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Empty),
                },
            };
        }
    }
}
