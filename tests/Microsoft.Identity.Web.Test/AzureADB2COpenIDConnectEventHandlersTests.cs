// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
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
        private readonly string _defaultIssuer = $"IssuerAddress/{DefaultUserFlow}/";
        private readonly string _customIssuer = $"IssuerAddress/{CustomUserFlow}/";
        private readonly AuthenticationScheme _authScheme;

        public AzureADB2COpenIDConnectEventHandlersTests()
        {
            _authScheme = new AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
                typeof(OpenIdConnectHandler));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OnRedirectToIdentityProvider_CustomUserFlow_UpdatesContextAsync(bool hasClientCredentials)
        {
            var errorAccessor = Substitute.For<ILoginErrorAccessor>();
            var options = new MicrosoftIdentityOptions() { SignUpSignInPolicyId = DefaultUserFlow };
            if (hasClientCredentials)
            {
                options.ClientSecret = TestConstants.ClientSecret;
            }

            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, options, errorAccessor);
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authProperties = new AuthenticationProperties();
            authProperties.Items.Add(OidcConstants.PolicyKey, CustomUserFlow);
            var context = new RedirectContext(httpContext, _authScheme, new OpenIdConnectOptions(), authProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage()
                {
                    IssuerAddress = _defaultIssuer,
                    Scope = TestConstants.Scopes,
                },
            };

            await handler.OnRedirectToIdentityProvider(context);

            errorAccessor.DidNotReceive().SetMessage(httpContext, Arg.Any<string>());
            Assert.Equal(TestConstants.Scopes, context.ProtocolMessage.Scope);
            Assert.Equal(_customIssuer, context.ProtocolMessage.IssuerAddress, true);
            Assert.False(context.Properties.Items.ContainsKey(OidcConstants.PolicyKey));
            if (hasClientCredentials)
            {
                Assert.Equal(OpenIdConnectResponseType.CodeIdToken, context.ProtocolMessage.ResponseType);
            }
            else
            {
                Assert.Equal(OpenIdConnectResponseType.IdToken, context.ProtocolMessage.ResponseType);
            }
        }

        [Fact]
        public async Task OnRedirectToIdentityProvider_DefaultUserFlow_DoesntUpdateContextAsync()
        {
            var errorAccessor = Substitute.For<ILoginErrorAccessor>();
            var options = new MicrosoftIdentityOptions() { SignUpSignInPolicyId = DefaultUserFlow };
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, options, errorAccessor);
            var httpContext = HttpContextUtilities.CreateHttpContext();
            var authProperties = new AuthenticationProperties();
            authProperties.Items.Add(OidcConstants.PolicyKey, DefaultUserFlow);
            var context = new RedirectContext(httpContext, _authScheme, new OpenIdConnectOptions(), authProperties) { ProtocolMessage = new OpenIdConnectMessage() { IssuerAddress = _defaultIssuer } };

            await handler.OnRedirectToIdentityProvider(context);

            errorAccessor.DidNotReceive().SetMessage(httpContext, Arg.Any<string>());
            Assert.Null(context.ProtocolMessage.Scope);
            Assert.Null(context.ProtocolMessage.ResponseType);
            Assert.Equal(_defaultIssuer, context.ProtocolMessage.IssuerAddress);
            Assert.True(context.Properties.Items.ContainsKey(OidcConstants.PolicyKey));
        }

        [Fact]
        public async Task OnRemoteFailure_PasswordReset_RedirectsSuccessfullyAsync()
        {
            var errorAccessor = Substitute.For<ILoginErrorAccessor>();
            var httpContext = Substitute.For<HttpContext>();
            httpContext.Request.PathBase = PathBase;
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, new MicrosoftIdentityOptions(), errorAccessor);

            var passwordResetException = "'access_denied', error_description: 'AADB2C90118: The user has forgotten their password. Correlation ID: f99deff4-f43b-43cc-b4e7-36141dbaf0a0 Timestamp: 2018-03-05 02:49:35Z', error_uri: 'error_uri is null'";

            await handler.OnRemoteFailure(new RemoteFailureContext(httpContext, _authScheme, new OpenIdConnectOptions(), new OpenIdConnectProtocolException(passwordResetException)));

            errorAccessor.DidNotReceive().SetMessage(httpContext, Arg.Any<string>());
            httpContext.Response.Received().Redirect($"{httpContext.Request.PathBase}/MicrosoftIdentity/Account/ResetPassword/{OpenIdConnectDefaults.AuthenticationScheme}");
        }

        [Fact]
        public async Task OnRemoteFailure_Cancel_RedirectsSuccessfullyAsync()
        {
            var errorAccessor = Substitute.For<ILoginErrorAccessor>();
            var httpContext = Substitute.For<HttpContext>();
            httpContext.Request.PathBase = PathBase;
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, new MicrosoftIdentityOptions(), errorAccessor);

            var cancelException = "'access_denied', error_description: 'AADB2C90091: The user has canceled entering self-asserted information. Correlation ID: d01c8878-0732-4eb2-beb8-da82a57432e0 Timestamp: 2018-03-05 02:56:49Z ', error_uri: 'error_uri is null'";

            await handler.OnRemoteFailure(
                new RemoteFailureContext(
                    httpContext,
                    _authScheme,
                    new OpenIdConnectOptions(),
                    new OpenIdConnectProtocolException(cancelException)));

            errorAccessor.DidNotReceive().SetMessage(httpContext, Arg.Any<string>());

            httpContext.Response.Received().Redirect($"{httpContext.Request.PathBase}/");
        }

        [Fact]
        public async Task OnRemoteFailure_OtherException_RedirectsSuccessfullyAsync()
        {
            var errorAccessor = Substitute.For<ILoginErrorAccessor>();
            var httpContext = Substitute.For<HttpContext>();
            httpContext.Request.PathBase = PathBase;
            var handler = new AzureADB2COpenIDConnectEventHandlers(OpenIdConnectDefaults.AuthenticationScheme, new MicrosoftIdentityOptions(), errorAccessor);

            var otherException = "Generic exception.";

            await handler.OnRemoteFailure(
                new RemoteFailureContext(
                    httpContext,
                    _authScheme,
                    new OpenIdConnectOptions(),
                    new OpenIdConnectProtocolException(otherException)));

            errorAccessor.Received(1).SetMessage(httpContext, otherException);
            httpContext.Response.Received().Redirect($"{httpContext.Request.PathBase}/MicrosoftIdentity/Account/Error");
        }
    }
}
