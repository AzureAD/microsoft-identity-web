﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Identity.Web
{
    internal class AzureADB2COpenIDConnectEventHandlers
    {
        private readonly ILoginErrorAccessor _errorAccessor;

        private readonly Dictionary<string, string> _userFlowToIssuerAddress =
            new(StringComparer.OrdinalIgnoreCase);

        public AzureADB2COpenIDConnectEventHandlers(
            string schemeName,
            MicrosoftIdentityOptions options,
            ILoginErrorAccessor errorAccessor)
        {
            SchemeName = schemeName;
            Options = options;
            _errorAccessor = errorAccessor;
        }

        public string SchemeName { get; }

        public MicrosoftIdentityOptions Options { get; }

        public Task OnRedirectToIdentityProvider(RedirectContext context)
        {
            var defaultUserFlow = Options.DefaultUserFlow;
            if (context.Properties.Items.TryGetValue(OidcConstants.PolicyKey, out var userFlow) &&
                !string.IsNullOrEmpty(userFlow) &&
                !string.Equals(userFlow, defaultUserFlow, StringComparison.OrdinalIgnoreCase))
            {
                context.ProtocolMessage.IssuerAddress = BuildIssuerAddress(context, defaultUserFlow, userFlow);
                context.Properties.Items.Remove(OidcConstants.PolicyKey);

                if (!Options.HasClientCredentials)
                {
                    context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdToken;
                }
                else if (Options.IsB2C)
                {
                    context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                }
                else
                {
                    context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.Code;
                }
            }

            return Task.CompletedTask;
        }

        public Task OnRemoteFailure(RemoteFailureContext context)
        {
            context.HandleResponse();

            bool isOidcProtocolException = context.Failure is OpenIdConnectProtocolException;

            // Handle the error code that Azure Active Directory B2C throws when trying to reset a password from the login page
            // because password reset is not supported by a "sign-up or sign-in user flow".
            // Below is a sample error message:
            // 'access_denied', error_description: 'AADB2C90118: The user has forgotten their password.
            // Correlation ID: f99deff4-f43b-43cc-b4e7-36141dbaf0a0
            // Timestamp: 2018-03-05 02:49:35Z
            // ', error_uri: 'error_uri is null'.
            string message = context.Failure?.Message ?? string.Empty;
            if (isOidcProtocolException && message.Contains(ErrorCodes.B2CForgottenPassword, StringComparison.OrdinalIgnoreCase))
            {
                // If the user clicked the reset password link, redirect to the reset password route
                context.Response.Redirect($"{context.Request.PathBase}{Options.ResetPasswordPath}/{SchemeName}");
            }

            // Access denied errors happen when a user cancels an action on the Azure Active Directory B2C UI. We just redirect back to
            // the main page in that case.
            // Message contains error: 'access_denied', error_description: 'AADB2C90091: The user has canceled entering self-asserted information.
            // Correlation ID: d01c8878-0732-4eb2-beb8-da82a57432e0
            // Timestamp: 2018-03-05 02:56:49Z
            // ', error_uri: 'error_uri is null'.
            else if (isOidcProtocolException && message.Contains(ErrorCodes.AccessDenied, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect($"{context.Request.PathBase}/");
            }
            else
            {
                _errorAccessor.SetMessage(context.HttpContext, message);

                context.Response.Redirect($"{context.Request.PathBase}{Options.ErrorPath}");
            }

            return Task.CompletedTask;
        }

        private string BuildIssuerAddress(RedirectContext context, string? defaultUserFlow, string userFlow)
        {
            if (!_userFlowToIssuerAddress.TryGetValue(userFlow, out var issuerAddress))
            {
                issuerAddress = context.ProtocolMessage.IssuerAddress
                    .Replace($"/{defaultUserFlow}/", $"/{userFlow}/", StringComparison.OrdinalIgnoreCase);
                issuerAddress = issuerAddress.ToLowerInvariant();

                _userFlowToIssuerAddress[userFlow] = issuerAddress;
            }

            return issuerAddress;
        }
    }
}
