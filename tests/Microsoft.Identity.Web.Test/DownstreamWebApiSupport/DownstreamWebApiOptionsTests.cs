// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Xunit;

namespace Microsoft.Identity.Web.Test.DownstreamWebApiSupport
{
    public class DownstreamWebApiOptionsTests
    {
        [Fact]
        public void OnCloningOptions_ShouldCloneAllProperties()
        {
            var original = new DownstreamWebApiOptions
            {
                Scopes = "scope",
                Tenant = "tenant",
                UserFlow = "flow",
                IsProofOfPossessionRequest = true,
                TokenAcquisitionOptions = new TokenAcquisitionOptions { Claims = "claims", FmiPath = "fmiPath"},
                AuthenticationScheme = "scheme",
                BaseUrl = "base",
                RelativePath = "relative",
                HttpMethod = HttpMethod.Get,
                CustomizeHttpRequestMessage = message => message.Headers.Add("HeaderKey", "HeaderValue"),
            };

            var clone = original.Clone();

            Assert.Equal(original.Scopes, clone.Scopes);
            Assert.Equal(original.Tenant, clone.Tenant);
            Assert.Equal(original.UserFlow, clone.UserFlow);
            Assert.Equal(original.IsProofOfPossessionRequest, clone.IsProofOfPossessionRequest);
            Assert.Equivalent(original.TokenAcquisitionOptions, clone.TokenAcquisitionOptions);
            Assert.Equal(original.AuthenticationScheme, clone.AuthenticationScheme);
            Assert.Equal(original.BaseUrl, clone.BaseUrl);
            Assert.Equal(original.RelativePath, clone.RelativePath);
            Assert.Equal(original.HttpMethod, clone.HttpMethod);
            Assert.Equal(original.CustomizeHttpRequestMessage, clone.CustomizeHttpRequestMessage);
        }
    }
}
