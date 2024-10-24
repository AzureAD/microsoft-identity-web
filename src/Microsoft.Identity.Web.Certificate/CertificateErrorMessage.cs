// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Constants related to the error messages.
    /// </summary>
    internal static class CertificateErrorMessage
    {
        // Configuration IDW10100 = "IDW10100:"
        public const string ClientSecretAndCertificateNull =
        "IDW10104: Both client secret and client certificate cannot be null or whitespace, " +
        "and only ONE must be included in the configuration of the web app when calling a web API. " +
        "For instance, in the appsettings.json file. ";
        public const string BothClientSecretAndCertificateProvided = "IDW10105: Both client secret and client certificate, " +
                   "cannot be included in the configuration of the web app when calling a web API. ";
        public const string ClientCertificatesHaveExpiredOrCannotBeLoaded = "IDW10109: All client certificates passed to the configuration have expired or can't be loaded. ";

        // Encoding IDW10600 = "IDW10600:"
        public const string InvalidBase64UrlString = "IDW10601: Invalid Base64URL string. ";

        // Certificates IDW10700 = "IDW10700:"
        public const string OnlyPkcs12IsSupported = "IDW10701: Only PKCS #12 content type is supported. Found Content-Type: {0}. ";
        public const string IncorrectNumberOfUriSegments = "IDW10702: Number of URI segments is incorrect: {0}, URI: {1}. ";
        public const string InvalidCertificateStorePath = "IDW10703: Certificate store path must be of the form 'StoreLocation/StoreName'. " +
            "StoreLocation must be one of 'CurrentUser', 'LocalMachine'. " +
            "StoreName must be empty or one of '{0}'. ";

        // Obsolete messages IDW10800 = "IDW10800:"
        public const string FromStoreWithThumbprintIsObsolete = "IDW10803: Use FromStoreWithThumbprint instead, due to spelling error. ";
    }
}
