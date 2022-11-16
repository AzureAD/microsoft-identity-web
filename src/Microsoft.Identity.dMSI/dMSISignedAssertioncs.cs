// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.dMSI
{
    /// <summary>
    /// Gets a signed assertion from dMSI
    /// </summary>
    public class dMSISignedAssertion : ClientAssertionProviderBase
    {
        /// <summary>
        /// Gets a signed assertion from a file.
        /// See https://aka.ms/ms-id-web/certificateless.
        /// </summary>
        public dMSISignedAssertion(string certificateUrl, string clientId, string authority)
        {
            _certificateUrl= certificateUrl;
            _clientId = clientId;
            _authority = authority;
        }

        private readonly string _certificateUrl;
        private readonly string _clientId;
        private readonly string _authority;

        /// <summary>
        /// Get the signed assertion from a file.
        /// </summary>
        /// <returns>The signed assertion.</returns>
        internal override Task<ClientAssertion> GetClientAssertion(CancellationToken cancellationToken)
        {
            X509Certificate2 certificate2 = GetCertificateFromdMSI(_certificateUrl);
            string signedAssertion = GetSignedClientAssertionAlt(certificate2);
            // Compute the expiry
            JsonWebToken jwt = new JsonWebToken(signedAssertion);
            return Task.FromResult(new ClientAssertion(signedAssertion, jwt.ValidTo));
        }

        private string GetSignedClientAssertionAlt(X509Certificate2 certificate)
        {
            //aud = https://login.microsoftonline.com/ + Tenant ID + /v2.0
            string aud = (_authority.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase)) 
                ? _authority 
                : _authority + "/v2.0";

            // no need to add exp, nbf as JsonWebTokenHandler will add them by default.
            var claims = new Dictionary<string, object>()
            {
                { "aud", aud },
                { "iss", _clientId },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", _clientId }
            };

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(certificate)
            };

            var handler = new JsonWebTokenHandler();
            var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);
            return signedClientAssertion;
        }

        private X509Certificate2 GetCertificateFromdMSI(string certificateUrl)
        {
            throw new NotImplementedException();
        }
    }
}
