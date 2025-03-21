// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    internal static class MsAuth10AtPop
    {
        internal static AcquireTokenForClientParameterBuilder WithAtPop(
            this AcquireTokenForClientParameterBuilder builder,
            string popPublicKey,
            string jwkClaim)
        {
            _ = Throws.IfNull(popPublicKey);
            _ = Throws.IfNull(jwkClaim);

            builder.WithProofOfPosessionKeyId(popPublicKey);
            builder.OnBeforeTokenRequest((data) =>
            {
                data.BodyParameters.Add("req_cnf", Base64UrlEncoder.Encode(jwkClaim));
                data.BodyParameters.Add("token_type", "pop");

                return Task.CompletedTask;
            });

            return builder;
        }
    }
}
