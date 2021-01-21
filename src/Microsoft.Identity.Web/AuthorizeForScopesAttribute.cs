// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Filter used on a controller action to trigger incremental consent.
    /// </summary>
    /// <example>
    /// The following controller action will trigger.
    /// <code>
    /// [AuthorizeForScopes(Scopes = new[] {"Mail.Send"})]
    /// public async Task&lt;IActionResult&gt; SendEmail()
    /// {
    /// }
    /// </code>
    /// </example>
    public class AuthorizeForScopesAttribute : ExceptionFilterAttribute
    {
        /// <summary>
        /// Scopes to request.
        /// </summary>
        public string[]? Scopes { get; set; }

        /// <summary>
        /// Key section on the configuration file that holds the scope value.
        /// </summary>
        public string? ScopeKeySection { get; set; }

        /// <summary>
        /// Azure AD B2C user flow.
        /// </summary>
        public string? UserFlow { get; set; }

        /// <summary>
        /// Allows specifying an AuthenticationScheme if OpenIdConnect is not the default challenge scheme.
        /// </summary>
        public string? AuthenticationScheme { get; set; }

        /// <summary>
        /// Handles the <see cref="MsalUiRequiredException"/>.
        /// </summary>
        /// <param name="context">Context provided by ASP.NET Core.</param>
        public override void OnException(ExceptionContext context)
        {
            if (context != null)
            {
                MsalUiRequiredException? msalUiRequiredException = FindMsalUiRequiredExceptionIfAny(context.Exception);
                if (msalUiRequiredException != null &&
                    IncrementalConsentAndConditionalAccessHelper.CanBeSolvedByReSignInOfUser(msalUiRequiredException))
                {
                    // the users cannot provide both scopes and ScopeKeySection at the same time
                    if (!string.IsNullOrWhiteSpace(ScopeKeySection) && Scopes != null && Scopes.Length > 0)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                IDWebErrorMessage.ProvideEitherScopeKeySectionOrScopes,
                                nameof(ScopeKeySection),
                                nameof(Scopes)));
                    }

                    // Do not re-use the property Scopes. For more info: https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/issues/273
                    string[]? incrementalConsentScopes;

                    // If the user wishes us to pick the Scopes from a particular config setting.
                    if (!string.IsNullOrWhiteSpace(ScopeKeySection))
                    {
                        // Load the injected IConfiguration
                        IConfiguration configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

                        if (configuration == null)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    IDWebErrorMessage.ScopeKeySectionIsProvidedButNotPresentInTheServicesCollection,
                                    nameof(ScopeKeySection)));
                        }

                        incrementalConsentScopes = new string[] { configuration.GetValue<string>(ScopeKeySection) };

                        if (Scopes != null && Scopes.Length > 0 && incrementalConsentScopes.Length > 0)
                        {
                            throw new InvalidOperationException(IDWebErrorMessage.NoScopesProvided);
                        }
                    }
                    else
                    {
                        incrementalConsentScopes = Scopes;
                    }

                    AuthenticationProperties properties = IncrementalConsentAndConditionalAccessHelper.BuildAuthenticationProperties(
                        incrementalConsentScopes,
                        msalUiRequiredException,
                        context.HttpContext.User,
                        UserFlow);

                    if (IsAjaxRequest(context.HttpContext.Request) && (!string.IsNullOrEmpty(context.HttpContext.Request.Headers[Constants.XReturnUrl])
                        || !string.IsNullOrEmpty(context.HttpContext.Request.Query[Constants.XReturnUrl])))
                    {
                        string redirectUri = !string.IsNullOrEmpty(context.HttpContext.Request.Headers[Constants.XReturnUrl]) ? context.HttpContext.Request.Headers[Constants.XReturnUrl]
                            : context.HttpContext.Request.Query[Constants.XReturnUrl];

                        UrlHelper urlHelper = new UrlHelper(context);
                        if (urlHelper.IsLocalUrl(redirectUri))
                        {
                            properties.RedirectUri = redirectUri;
                        }
                    }

                    if (AuthenticationScheme != null)
                    {
                        context.Result = new ChallengeResult(AuthenticationScheme, properties);
                    }
                    else
                    {
                        context.Result = new ChallengeResult(properties);
                    }
                }
            }

            base.OnException(context);
        }

        /// <summary>
        /// Finds an MsalUiRequiredException in one of the inner exceptions.
        /// </summary>
        /// <param name="exception">Exception from which we look for an MsalUiRequiredException.</param>
        /// <returns>The MsalUiRequiredException if there is one, null, otherwise.</returns>
        internal /* for testing */ static MsalUiRequiredException? FindMsalUiRequiredExceptionIfAny(Exception exception)
        {
            MsalUiRequiredException? msalUiRequiredException = exception as MsalUiRequiredException;
            if (msalUiRequiredException != null)
            {
                return msalUiRequiredException;
            }
            else if (exception.InnerException != null)
            {
                return FindMsalUiRequiredExceptionIfAny(exception.InnerException);
            }
            else
            {
                return null;
            }
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return string.Equals(request.Query[Constants.XRequestedWith], Constants.XmlHttpRequest, StringComparison.Ordinal) ||
                string.Equals(request.Headers[Constants.XRequestedWith], Constants.XmlHttpRequest, StringComparison.Ordinal);
        }
    }
}
