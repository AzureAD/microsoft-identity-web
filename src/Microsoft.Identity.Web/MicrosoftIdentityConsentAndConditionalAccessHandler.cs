// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Handler for Blazor specific APIs to handle incremental consent
    /// and conditional access.
    /// </summary>
    public class MicrosoftIdentityConsentAndConditionalAccessHandler
    {
        private ClaimsPrincipal? _user;
        private string? _baseUri;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityConsentAndConditionalAccessHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider to get the HttpContextAccessor for the current HttpContext, when available.</param>
        public MicrosoftIdentityConsentAndConditionalAccessHandler(IServiceProvider serviceProvider)
        {
            _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        }

        /// <summary>
        /// Boolean to determine if server is Blazor.
        /// </summary>
        public bool IsBlazorServer { get; set; }

        /// <summary>
        /// Current user.
        /// </summary>
        public ClaimsPrincipal User
        {
            get
            {
                if (_user != null)
                {
                    return _user;
                }

                HttpContext httpContext = _httpContextAccessor!.HttpContext!;
                ClaimsPrincipal user;

                lock (httpContext)
                {
                    user = httpContext.User;
                }

                return !IsBlazorServer ? user :
                    throw new InvalidOperationException(IDWebErrorMessage.BlazorServerUserNotSet);
            }
            set
            {
                _user = value;
            }
        }

        /// <summary>
        /// Base URI to use in forming the redirect.
        /// </summary>
        public string? BaseUri
        {
            get
            {
                if (_baseUri != null)
                {
                    return _baseUri;
                }

                HttpRequest httpRequest;
                HttpContext httpContext = _httpContextAccessor!.HttpContext!;

                lock (httpContext)
                {
                    httpRequest = httpContext.Request;
                }

                return !IsBlazorServer ? CreateBaseUri(httpRequest) :
                    throw new InvalidOperationException(IDWebErrorMessage.BlazorServerBaseUriNotSet);
            }
            set
            {
                _baseUri = value;
            }
        }

        private static string CreateBaseUri(HttpRequest request)
        {
            string baseUri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}://{1}/{2}",
                request.Scheme,
                request.Host.ToString(),
                request.PathBase.ToString().TrimStart('/'));
            return baseUri.TrimEnd('/');
        }

        /// <summary>
        /// For Blazor/Razor pages to process the exception from
        /// a user challenge.
        /// </summary>
        /// <param name="exception">Exception.</param>
        public void HandleException(Exception exception)
        {
            MicrosoftIdentityWebChallengeUserException? microsoftIdentityWebChallengeUserException =
                   exception as MicrosoftIdentityWebChallengeUserException;

            if (microsoftIdentityWebChallengeUserException == null)
            {
#pragma warning disable CA1062 // Validate arguments of public methods
                microsoftIdentityWebChallengeUserException = exception.InnerException as MicrosoftIdentityWebChallengeUserException;
#pragma warning restore CA1062 // Validate arguments of public methods
            }

            if (microsoftIdentityWebChallengeUserException != null &&
               IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(microsoftIdentityWebChallengeUserException.MsalUiRequiredException))
            {
                var properties = IncrementalConsentAndConditionalAccessHelper.BuildAuthenticationProperties(
                    microsoftIdentityWebChallengeUserException.Scopes,
                    microsoftIdentityWebChallengeUserException.MsalUiRequiredException,
                    User,
                    microsoftIdentityWebChallengeUserException.Userflow);

                List<string> scopes = properties.Parameters.ContainsKey(Constants.Scope) ? (List<string>)properties.Parameters[Constants.Scope]! : new List<string>();
                string claims = properties.Parameters.ContainsKey(Constants.Claims) ? (string)properties.Parameters[Constants.Claims]! : string.Empty;
                string userflow = properties.Items.ContainsKey(OidcConstants.PolicyKey) ? properties.Items[OidcConstants.PolicyKey]! : string.Empty;

                ChallengeUser(
                    scopes.ToArray(),
                    claims,
                    userflow);
            }
            else
            {
                throw exception;
            }
        }

        /// <summary>
        /// Forces the user to consent to specific scopes and perform
        /// Conditional Access to get specific claims. Use on a Razor/Blazor
        /// page or controller to proactively ensure the scopes and/or claims
        /// before acquiring a token. The other mechanism <see cref="HandleException(Exception)"/>
        /// ensures claims and scopes requested by Azure AD after a failed token acquisition attempt.
        /// See https://aka.ms/ms-id-web/ca_incremental-consent for details.
        /// </summary>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="claims">Claims to ensure.</param>
        /// <param name="userflow">Userflow being invoked for AAD B2C.</param>
        public void ChallengeUser(
            string[]? scopes,
            string? claims = null,
            string? userflow = null)
        {
            IEnumerable<string> effectiveScopes = scopes ?? new string[0];

            string[] additionalBuiltInScopes =
            {
                 OidcConstants.ScopeOpenId,
                 OidcConstants.ScopeOfflineAccess,
                 OidcConstants.ScopeProfile,
            };

            effectiveScopes = effectiveScopes.Union(additionalBuiltInScopes);

            string redirectUri;
            if (IsBlazorServer)
            {
                redirectUri = NavigationManager.Uri;
            }
            else
            {
                HttpRequest httpRequest;
                HttpContext httpContext = _httpContextAccessor!.HttpContext!;

                lock (httpContext)
                {
                    httpRequest = httpContext.Request;
                }

                redirectUri = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    CreateBaseUri(httpRequest),
                    httpRequest.Path.ToString().TrimStart('/'));
            }

            string url = $"{BaseUri}/{Constants.BlazorChallengeUri}{redirectUri}"
                + $"&{Constants.Scope}={string.Join(" ", effectiveScopes!)}&{Constants.LoginHintParameter}={User.GetLoginHint()}"
                + $"&{Constants.DomainHintParameter}={User.GetDomainHint()}&{Constants.Claims}={claims}"
                + $"&{OidcConstants.PolicyKey}={userflow}";

            if (IsBlazorServer)
            {
                NavigationManager.NavigateTo(url, true);
            }
            else
            {
                HttpContext httpContext = _httpContextAccessor!.HttpContext!;

                lock (httpContext)
                {
                    httpContext.Response.Redirect(url); // CodeQL [SM00405] Intentionally redirect to URL containing specific claims
                }
            }
        }

        internal NavigationManager NavigationManager { get; set; } = null!;
    }
}
