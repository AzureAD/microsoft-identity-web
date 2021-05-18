// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.Identity.Web
{
    internal class JwtBearerMergedOptions : JwtBearerOptions
    {
        public static void UpdateJwtBearerOptionsFromJwtBearerOptions(JwtBearerOptions jwtBearerOptions, JwtBearerOptions jwtBearerMergedOptions)
        {
            if (string.IsNullOrEmpty(jwtBearerMergedOptions.Audience) && !string.IsNullOrEmpty(jwtBearerOptions.Audience))
            {
                jwtBearerMergedOptions.Audience = jwtBearerOptions.Audience;
            }

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.Authority) && !string.IsNullOrEmpty(jwtBearerOptions.Authority))
            {
                jwtBearerMergedOptions.Authority = jwtBearerOptions.Authority;
            }

#if DOTNET_50_AND_ABOVE
            jwtBearerMergedOptions.AutomaticRefreshInterval = jwtBearerOptions.AutomaticRefreshInterval;
#endif

            jwtBearerMergedOptions.BackchannelHttpHandler ??= jwtBearerOptions.BackchannelHttpHandler;
            jwtBearerMergedOptions.BackchannelTimeout = jwtBearerOptions.BackchannelTimeout;
            if (string.IsNullOrEmpty(jwtBearerMergedOptions.Challenge) && !string.IsNullOrEmpty(jwtBearerOptions.Challenge))
            {
                jwtBearerMergedOptions.Challenge = jwtBearerOptions.Challenge;
            }

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ClaimsIssuer) && !string.IsNullOrEmpty(jwtBearerOptions.ClaimsIssuer))
            {
                jwtBearerMergedOptions.ClaimsIssuer = jwtBearerOptions.ClaimsIssuer;
            }

            jwtBearerMergedOptions.Configuration ??= jwtBearerOptions.Configuration;
            jwtBearerMergedOptions.ConfigurationManager ??= jwtBearerOptions.ConfigurationManager;
            jwtBearerMergedOptions.Events ??= jwtBearerOptions.Events;
            jwtBearerMergedOptions.EventsType ??= jwtBearerOptions.EventsType;
            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ForwardAuthenticate) && !string.IsNullOrEmpty(jwtBearerOptions.ForwardAuthenticate))
            {
                jwtBearerMergedOptions.ForwardAuthenticate = jwtBearerOptions.ForwardAuthenticate;
            }

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ForwardChallenge) && !string.IsNullOrEmpty(jwtBearerOptions.ForwardChallenge))
            {
                jwtBearerMergedOptions.ForwardChallenge = jwtBearerOptions.ForwardChallenge;
            }

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ForwardDefault) && !string.IsNullOrEmpty(jwtBearerOptions.ForwardDefault))
            {
                jwtBearerMergedOptions.ForwardDefault = jwtBearerOptions.ForwardDefault;
            }

            jwtBearerMergedOptions.ForwardDefaultSelector ??= jwtBearerOptions.ForwardDefaultSelector;
            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ForwardForbid) && !string.IsNullOrEmpty(jwtBearerOptions.ForwardForbid))
            {
                jwtBearerMergedOptions.ForwardForbid = jwtBearerOptions.ForwardForbid;
            }

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ForwardSignIn) && !string.IsNullOrEmpty(jwtBearerOptions.ForwardSignIn))
            {
                jwtBearerMergedOptions.ForwardSignIn = jwtBearerOptions.ForwardSignIn;
            }

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.ForwardSignOut) && !string.IsNullOrEmpty(jwtBearerOptions.ForwardSignOut))
            {
                jwtBearerMergedOptions.ForwardSignOut = jwtBearerOptions.ForwardSignOut;
            }

            jwtBearerMergedOptions.IncludeErrorDetails = jwtBearerOptions.IncludeErrorDetails;

#if DOTNET_50_AND_ABOVE
            jwtBearerMergedOptions.MapInboundClaims = jwtBearerOptions.MapInboundClaims;
#endif 

            if (string.IsNullOrEmpty(jwtBearerMergedOptions.MetadataAddress) && !string.IsNullOrEmpty(jwtBearerOptions.MetadataAddress))
            {
                jwtBearerMergedOptions.MetadataAddress = jwtBearerOptions.MetadataAddress;
            }

#if DOTNET_50_AND_ABOVE
            jwtBearerMergedOptions.RefreshInterval = jwtBearerOptions.RefreshInterval;
#endif

            jwtBearerMergedOptions.RefreshOnIssuerKeyNotFound = jwtBearerOptions.RefreshOnIssuerKeyNotFound;
            jwtBearerMergedOptions.RequireHttpsMetadata = jwtBearerOptions.RequireHttpsMetadata;
            jwtBearerMergedOptions.SaveToken = jwtBearerOptions.SaveToken;
            jwtBearerMergedOptions.TokenValidationParameters ??= jwtBearerOptions.TokenValidationParameters;
        }
    }
}
