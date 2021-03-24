// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.Identity.Web.Resource
{
	/// <summary>
	/// Diagnostics used in the OpenID Connect middleware
	/// (used in web apps).
	/// </summary>
	public interface IOpenIdConnectMiddlewareDiagnostics
	{
		/// <summary>
		/// Method to subscribe to <see cref="OpenIdConnectEvents"/>.
		/// </summary>
		/// <param name="events">OpenID Connect events.</param>
		void Subscribe(OpenIdConnectEvents events);
	}
}
