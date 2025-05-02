// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web
{
    internal static class MsAuth10AtPop
    {
        internal static AcquireTokenForClientParameterBuilder WithAtPop(
            this AcquireTokenForClientParameterBuilder builder,
            string popPublicKey,
            string jwkClaim)
        {
            _ = Throws.IfNullOrWhitespace(popPublicKey);
            _ = Throws.IfNullOrWhitespace(jwkClaim);

            AtPopOperation op = new AtPopOperation(popPublicKey, jwkClaim);
            builder.WithAuthenticationExtension(new MsalAuthenticationExtension()
            {
                AuthenticationOperation = op
            });
            return builder;
        }
    }
}
