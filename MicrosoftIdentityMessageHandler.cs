// Note: ForceRefresh is not set because MSAL automatically bypasses the cache when
// Claims are specified (see CacheRefreshReason.ForceRefreshOrClaims in MSAL.NET).
// Reference: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/src/client/Microsoft.Identity.Client/Cache/CacheRefreshReason.cs#L16

// ... (Content before line 273)

        // Updated line 273
        var challenge = WwwAuthenticateChallengeHelper.ExtractClaimChallenge(response);

        // ... (Content between line 273 and line 284)

        // Updated line 284
        var clonedRequest = await WwwAuthenticateChallengeHelper.CloneRequestAsync(request);

        // ... (Content before line 377)

        // Updated lines 377-390
        Claims = challengeClaims;
        // Note: ForceRefresh is not set because MSAL automatically bypasses the cache when
        // Claims are specified (see CacheRefreshReason.ForceRefreshOrClaims in MSAL.NET).
        // Reference: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/src/client/Microsoft.Identity.Client/Cache/CacheRefreshReason.cs#L16

        // ... (Content from line 390 to line 410)

        // Deleted lines 410-457 (CloneHttpRequestMessageAsync method)

        // ... (Content after line 457)