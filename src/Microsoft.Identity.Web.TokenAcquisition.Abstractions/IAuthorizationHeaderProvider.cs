namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Creates the authorization header used to call a protected web API
    /// </summary>
    internal interface IAuthorizationHeaderProvider
    {
        /// <summary>
        /// Creates the authorization header used to call a protected web API.
        /// </summary>
        /// <returns>A string containing the protocols (for instance: "Bearer", "PoP")
        /// and tokens.</returns>
        string CreateAuthorizationHeader();
    }
}
