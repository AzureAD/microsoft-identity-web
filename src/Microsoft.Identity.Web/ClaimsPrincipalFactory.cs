// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;

namespace Microsoft.Identity.Web
{
	/// <summary>
	/// Factory class to create <see cref="ClaimsPrincipal"/> objects.
	/// </summary>
	public static class ClaimsPrincipalFactory
	{
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
		///  foreach (var notification in notifications)
		///  {
		///   SubscriptionStore subscription =
		///		   subscriptionStore.GetSubscriptionInfo(notification.SubscriptionId);
		///  HttpContext.User = ClaimsPrincipalExtension.FromTenantIdAndObjectId(subscription.TenantId,
		///																	  subscription.UserId);
		///  string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
		/// </code>
		/// </example>
		public static ClaimsPrincipal FromTenantIdAndObjectId(string tenantId, string objectId)
		{
			return new ClaimsPrincipal(
				new ClaimsIdentity(new Claim[]
				{
					new Claim(ClaimConstants.UniqueTenantIdentifier, tenantId),
					new Claim(ClaimConstants.UniqueObjectIdentifier, objectId),
				}));
		}
	}
}
