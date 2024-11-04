// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Factory class to create <see cref="ClaimsPrincipal"/> objects.
    /// </summary>
    public static class ClaimsPrincipalFactory
    {
        /// <summary>
        /// Instantiate a <see cref="ClaimsPrincipal"/> from a home account object ID and home tenant ID. This can
        /// be useful when the web app subscribes to another service on behalf of the user
        /// and then is called back by a notification where the user is identified by their home tenant
        /// ID and home object ID (like in Microsoft Graph Web Hooks).
        /// </summary>
        /// <param name="homeTenantId">Home tenant ID of the account.</param>
        /// <param name="homeObjectId">Home object ID of the account in this tenant ID.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing these two claims.</returns>
        ///
        /// <example>
        /// <code>
        /// private async Task GetChangedMessagesAsync(IEnumerable&lt;Notification&gt; notifications)
        /// {
        ///  HttpContext.User = ClaimsPrincipalExtension.FromHomeTenantIdAndHomeObjectId(subscription.HomeTenantId,
        ///                                                                      subscription.HomeUserId);
        ///  foreach (var notification in notifications)
        ///  {
        ///   SubscriptionStore subscription =
        ///           subscriptionStore.GetSubscriptionInfo(notification.SubscriptionId);
        ///  string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        ///  ...}
        ///  }
        /// </code>
        /// </example>
        public static ClaimsPrincipal FromHomeTenantIdAndHomeObjectId(string homeTenantId, string homeObjectId)
        {
            if (AppContextSwitches.UseClaimsIdentityType)
            {
#pragma warning disable RS0030 // Do not use banned APIs
                return new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimConstants.UniqueTenantIdentifier, homeTenantId),
                        new Claim(ClaimConstants.UniqueObjectIdentifier, homeObjectId),
                    }));
#pragma warning restore RS0030 // Do not use banned APIs
            }
            else
            {
                return new ClaimsPrincipal(
                    new CaseSensitiveClaimsIdentity(new[]
                    {
                        new Claim(ClaimConstants.UniqueTenantIdentifier, homeTenantId),
                        new Claim(ClaimConstants.UniqueObjectIdentifier, homeObjectId),
                    }));
            }
        }

        /// <summary>
        /// Instantiate a <see cref="ClaimsPrincipal"/> from an account object ID and tenant ID. This can
        /// be useful when the web app subscribes to another service on behalf of the user
        /// and then is called back by a notification where the user is identified by their tenant
        /// ID and object ID (like in Microsoft Graph Web Hooks).
        /// </summary>
        /// <param name="tenantId">Tenant ID of the account.</param>
        /// <param name="objectId">Object ID of the account in this tenant ID.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing these two claims.</returns>
        ///
        /// <example>
        /// <code>
        /// private async Task GetChangedMessagesAsync(IEnumerable&lt;Notification&gt; notifications)
        /// {
        ///  HttpContext.User = ClaimsPrincipalExtension.FromTenantIdAndObjectId(subscription.TenantId,
        ///                                                                      subscription.UserId);
        ///  foreach (var notification in notifications)
        ///  {
        ///   SubscriptionStore subscription =
        ///           subscriptionStore.GetSubscriptionInfo(notification.SubscriptionId);
        ///  string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        ///  ...}
        ///  }
        /// </code>
        /// </example>
        public static ClaimsPrincipal FromTenantIdAndObjectId(string tenantId, string objectId)
        {
            if (AppContextSwitches.UseClaimsIdentityType)
            {
#pragma warning disable RS0030 // Do not use banned APIs
                return new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimConstants.Tid, tenantId),
                        new Claim(ClaimConstants.Oid, objectId),
                    }));
#pragma warning restore RS0030 // Do not use banned APIs
            } else
            {
                return new ClaimsPrincipal(
                    new CaseSensitiveClaimsIdentity(new[]
                    {
                        new Claim(ClaimConstants.Tid, tenantId),
                        new Claim(ClaimConstants.Oid, objectId),
                    }));
            }
        }

        /// <summary>
        /// Instantiate a <see cref="ClaimsPrincipal"/> from a username and password.
        /// This can be used for ROPC flow for testing purposes.
        /// </summary>
        /// <param name="username">UPN of the user for example username@domain.</param>
        /// <param name="password">Password for the user.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing these two claims.</returns>
        public static ClaimsPrincipal FromUsernamePassword(string username, string password)
        {
            return new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(ClaimConstants.Username, username),
                    new Claim(ClaimConstants.Password, password),
                }));
        }
    }
}
