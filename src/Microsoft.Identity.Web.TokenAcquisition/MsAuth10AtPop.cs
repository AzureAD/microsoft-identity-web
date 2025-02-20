// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
            string clientId,
            bool sendX5C)
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
                     clientId, 
                     sendX5C);

                 //data.BodyParameters.Remove("client_assertion");
                 //data.BodyParameters.Add("request", signedAssertion);

                 data.BodyParameters["client_assertion"] = signedAssertion;
                 data.BodyParameters.Add("req_cnf", Base64UrlEncoder.Encode(jwkClaim));
                 data.BodyParameters.Add("token_type", "pop");

                 return Task.CompletedTask;
             });

            return builder;
        }

        private static string? GetSignedClientAssertion(
            X509Certificate2 certificate,
            string audience,
            string jwkClaim,
            string clientId,
            bool sendX5C)
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

            var signingCredentials = new X509SigningCredentials(certificate);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            { 
                Claims = claims,
                SigningCredentials = signingCredentials
            };

            if (sendX5C)
            {
                string x5cValue = Convert.ToBase64String(certificate.GetRawCertData());
                securityTokenDescriptor.AdditionalHeaderClaims = 
                    new Dictionary<string, object>() { { "x5c", new List<string> { x5cValue } } };
            }
            
            JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
            string token = tokenHandler.CreateToken(securityTokenDescriptor);

            return token;
        }
    }
}
