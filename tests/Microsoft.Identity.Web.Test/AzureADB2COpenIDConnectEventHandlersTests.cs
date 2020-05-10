// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AzureADB2COpenIDConnectEventHandlersTests
    {
        private const string PathBase = "/PathBase";
        private const string DefaultUserFlow = TestConstants.B2CSignUpSignInUserFlow;
        private const string CustomUserFlow = TestConstants.B2CResetPasswordUserFlow;
        private string _defaultIssuer = $"IssuerAddress/{DefaultUserFlow}/";
        private string _customIssuer = $"IssuerAddress/{CustomUserFlow}/";
        private AuthenticationScheme _authScheme;

        public AzureADB2COpenIDConnectEventHandlersTests()
        {
            _authScheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme, typeof(OpenIdConnectHandler));
        }

        [Fact]
        public async void OnRedirectToIdentityProvider_CustomUserFlow_UpdatesContext()
        {
            var options = new MicrosoftIdentityOptions() { SignUpSignInPolicyId = DefaultUserFlow };
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, options);
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authProperties = new AuthenticationProperties();
            authProperties.Items.Add(OidcConstants.PolicyKey, CustomUserFlow);
            var context = new RedirectContext(httpContext, _authScheme, new OpenIdConnectOptions(), authProperties) { ProtocolMessage = new OpenIdConnectMessage() { IssuerAddress = _defaultIssuer } };

            await handler.OnRedirectToIdentityProvider(context).ConfigureAwait(false);

            Assert.Equal(OpenIdConnectScope.OpenIdProfile, context.ProtocolMessage.Scope);
            Assert.Equal(OpenIdConnectResponseType.IdToken, context.ProtocolMessage.ResponseType);
            Assert.Equal(_customIssuer, context.ProtocolMessage.IssuerAddress, true);
            Assert.False(context.Properties.Items.ContainsKey(OidcConstants.PolicyKey));
        }

        [Fact]
        public async void OnRedirectToIdentityProvider_DefaultUserFlow_DoesntUpdateContext()
        {
            var options = new MicrosoftIdentityOptions() { SignUpSignInPolicyId = DefaultUserFlow };
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, options);
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authProperties = new AuthenticationProperties();
            authProperties.Items.Add(OidcConstants.PolicyKey, DefaultUserFlow);
            var context = new RedirectContext(httpContext, _authScheme, new OpenIdConnectOptions(), authProperties) { ProtocolMessage = new OpenIdConnectMessage() { IssuerAddress = _defaultIssuer } };

            await handler.OnRedirectToIdentityProvider(context).ConfigureAwait(false);

            Assert.Null(context.ProtocolMessage.Scope);
            Assert.Null(context.ProtocolMessage.ResponseType);
            Assert.Equal(_defaultIssuer, context.ProtocolMessage.IssuerAddress);
            Assert.True(context.Properties.Items.ContainsKey(OidcConstants.PolicyKey));
        }

        [Fact]
        public async void OnRemoteFailure_PasswordReset_RedirectsSuccessfully()
        {
            var httpContext = Substitute.For<HttpContext>();
            httpContext.Request.PathBase = PathBase;
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, new MicrosoftIdentityOptions());

            var passwordResetException = "'access_denied', error_description: 'AADB2C90118: The user has forgotten their password. Correlation ID: f99deff4-f43b-43cc-b4e7-36141dbaf0a0 Timestamp: 2018-03-05 02:49:35Z', error_uri: 'error_uri is null'";

            await handler.OnRemoteFailure(new RemoteFailureContext(httpContext, _authScheme, new OpenIdConnectOptions(), new OpenIdConnectProtocolException(passwordResetException))).ConfigureAwait(false);

            httpContext.Response.Received().Redirect($"{httpContext.Request.PathBase}/MicrosoftIdentity/Account/ResetPassword/{OpenIdConnectDefaults.AuthenticationScheme}");
        }

        [Fact]
        public async void OnRemoteFailure_Cancel_RedirectsSuccessfully()
        {
            var httpContext = Substitute.For<HttpContext>();
            httpContext.Request.PathBase = PathBase;
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, new MicrosoftIdentityOptions());

            var cancelException = "'access_denied', error_description: 'AADB2C90091: The user has canceled entering self-asserted information. Correlation ID: d01c8878-0732-4eb2-beb8-da82a57432e0 Timestamp: 2018-03-05 02:56:49Z ', error_uri: 'error_uri is null'";

            await handler.OnRemoteFailure(
                new RemoteFailureContext(
                    httpContext,
                    _authScheme,
                    new OpenIdConnectOptions(),
                    new OpenIdConnectProtocolException(cancelException))).ConfigureAwait(false);

            httpContext.Response.Received().Redirect($"{httpContext.Request.PathBase}/");
        }

        [Fact]
        public async void OnRemoteFailure_OtherException_RedirectsSuccessfully()
        {
            var httpContext = Substitute.For<HttpContext>();
            httpContext.Request.PathBase = PathBase;
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, new MicrosoftIdentityOptions());

            var otherException = "Generic exception.";

            await handler.OnRemoteFailure(
                new RemoteFailureContext(
                    httpContext,
                    _authScheme,
                    new OpenIdConnectOptions(),
                    new OpenIdConnectProtocolException(otherException))).ConfigureAwait(false);

            httpContext.Response.Received().Redirect($"{httpContext.Request.PathBase}/MicrosoftIdentity/Account/Error");
        }
    }
}
