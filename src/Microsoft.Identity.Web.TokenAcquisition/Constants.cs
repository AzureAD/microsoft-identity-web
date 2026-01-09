    /// <summary>
    /// Centralized list of error codes that indicate certificate-related or signed assertion errors
    /// returned by eSTS. Used to determine when certificate reload retries are appropriate.
    /// Update here for maintainability.
    /// </summary>
    internal static readonly HashSet<string> CertificateErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        InvalidKeyError,                       // "AADSTS700027"
        SignedAssertionInvalidTimeRange,       // "AADSTS700024"
        CertificateHasBeenRevoked,             // "AADSTS7000214"
        CertificateIsOutsideValidityWindow,    // "AADSTS1000502"
        CertificateNotWithinValidityPeriod,    // "AADSTS7000274"
        CertificateWasRevoked                  // "AADSTS7000277"
    };
