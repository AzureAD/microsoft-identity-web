﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

// TODO
// This class should really be in Microsoft.IdentityModel.Tokens

namespace Microsoft.Identity.Web
{
    class MicrosoftAuthenticationTicket : ClaimsPrincipal 
    {
        // TODO: decide the contructors to have.

        /// <summary>
        /// Subject identity
        /// </summary>
        public MicrosoftClaimsIdentity? SubjectIdentity
        {
            get
            {
                return Identities.
                    OfType<MicrosoftClaimsIdentity>()
                    .FirstOrDefault(i => i.Label == "SubjectIdentity");
            }
        }

        public MicrosoftClaimsIdentity? ApplicationIdentity
        {
            get
            {
                return Identities.
                    OfType<MicrosoftClaimsIdentity>()
                    .FirstOrDefault(i => i.Label == "ApplicationIdentity") ;
            }
        }

        public string? AccessTokenType { get; private set; }
        public string? AuthenticationScheme { get; private set; }
        public string? AuthenticationMode { get; private set; }


        /// <summary>
        /// Gets the <see cref="SecurityToken"/> that was used to create to the <see cref="ApplicationIdentity"/>.
        /// </summary>
        public SecurityToken? ApplicationToken => ApplicationIdentity?.Token;

        /// <summary>
        /// Gets othe rawData associated with the ApplicationToken.
        /// </summary>
        public string?ApplicationTokenRawData => ApplicationIdentity?.TokenRawData;


        /// <summary>
        /// Gets the <see cref="SecurityToken"/> that was used to create to the <see cref="ApplicationIdentity"/>.
        /// </summary>
        public SecurityToken? SubjectToken => SubjectIdentity?.Token;

        /// <summary>
        /// Gets othe rawData associated with the SubjectToken.
        /// </summary>
        public string? SubjectTokenRawData => SubjectIdentity?.TokenRawData;

    }
}
