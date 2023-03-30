// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    internal static class MsAuth10AtPop
    {
        internal static AcquireTokenForClientParameterBuilder WithAtPop(
            this AcquireTokenForClientParameterBuilder builder,
            X509Certificate2 clientCertificate,
            string popPublicKey,
            string jwkClaim,
            string clientId)
        {
            _ = Throws.IfNull(popPublicKey);
            _ = Throws.IfNull(jwkClaim);

            builder.WithProofOfPosessionKeyId(popPublicKey);
            builder.OnBeforeTokenRequest((data) =>
             {
                 string? signedAssertion = GetSignedClientAssertion(
                     clientCertificate,
                     data.RequestUri.AbsoluteUri,
                     jwkClaim,
                     clientId);

                 data.BodyParameters.Remove("client_assertion");
                 data.BodyParameters.Add("request", signedAssertion);
                 return Task.CompletedTask;
             });

            return builder;
        }

        private static string? GetSignedClientAssertion(
            X509Certificate2 certificate,
            string audience,
            string jwkClaim,
            string clientId)
        {
            // no need to add exp, nbf as JsonWebTokenHandler will add them by default
            var claims = new Dictionary<string, object>()
            {
                { "aud", audience },
                { "iss", clientId },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", clientId },
                { "pop_jwk", jwkClaim }
            };

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(certificate)
            };

            var handler = new JsonWebTokenHandler();
            return handler.CreateToken(securityTokenDescriptor);
        }
    }
}
