// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// EventIds for Logging.
    /// </summary>
    internal static class LoggingEventId
    {
#pragma warning disable IDE1006 // Naming Styles
        // SessionCacheProvider EventIds 200+
        public static readonly EventId SessionCache = new EventId(200, "SessionCache");
        public static readonly EventId SessionCacheKeyNotFound = new EventId(201, "SessionCacheKeyNotFound");

        // TokenAcquisition EventIds 300+
        public static readonly EventId TokenAcquisitionError = new EventId(300, "TokenAcquisitionError");
        public static readonly EventId TokenAcquisitionMsalAuthenticationResultTime = new EventId(301, "TokenAcquisitionMsalAuthenticationResultTime");

        // ConfidentialClientApplicationBuilderExtension EventIds 400+
        public static readonly EventId NotUsingManagedIdentity = new EventId(400, "NotUsingManagedIdentity");
        public static readonly EventId UsingManagedIdentity = new EventId(401, "UsingManagedIdentity");
        public static readonly EventId UsingPodIdentityFile = new EventId(402, "UsingPodIdentityFile");
        public static readonly EventId UsingCertThumbprint = new EventId(403, "UsingCertThumbprint");
        public static readonly EventId UsingSignedAssertionFromVault = new EventId(404, "UsingSignedAssertionFromVault");
        public static readonly EventId CredentialLoadAttempt = new EventId(405, "CredentialLoadAttempt");
        public static readonly EventId CredentialLoadAttemptFailed = new EventId(406, "CredentialLoadAttemptFailed");
        public static readonly EventId UsingSignedAssertionFromCustomProvider = new EventId(407, "UsingSignedAssertionFromCustomProvider");

        // MergedOptions EventIds 408+
        public static readonly EventId AuthorityConflict = new EventId(408, "AuthorityConflict");

#pragma warning restore IDE1006 // Naming Styles
    }
}
