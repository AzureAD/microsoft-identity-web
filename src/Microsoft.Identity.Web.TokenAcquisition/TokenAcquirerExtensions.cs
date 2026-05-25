// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods for TokenAcquirer
    /// </summary>
    public static class TokenAcquirerExtensions
    {
        /// <summary>
        /// Used to exchange a client assertion for a token.
        /// </summary>
        /// <param name="tokenAcquirer">Token acquirer from which to get a Federation Identity Credential
        /// token. A FIC token is used as a client assertion to get another token.</param>
        /// <param name="options">Options for the token acquisition (for instance to override the tenant
        /// programmatically).</param>
        /// <param name="clientAssertion">Client assertion to use for the token acquisition. If
        /// null, the FIC Token will be acquired using the credentials associated with token token
        /// acquirer (that is the <see cref="MicrosoftIdentityApplicationOptions"/> named from the
        /// <see cref="AcquireTokenOptions.AuthenticationOptionsName"/>)</param>
        /// <param name="scope">Scope for the token acquisition.  By default this is <c>api://AzureAdTokenExchange/.default</c>.
        /// for other clouds, you would need to override it:
        /// <list type="bullet">
        /// <item>
        /// <term>api://AzureAdTokenExchange/.default</term><description>for public cloud</description>
        /// <term>api://AzureADTokenExchangeChina/.default</term><description>for China cloud</description>
        /// <term>api://AzureADTokenExchangeFrance/.default</term><description>for Bleu</description>
        /// <term>api://AzureADTokenExchangeGermany/.default</term><description>for the German cloud</description>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public static async Task<AcquireTokenResult> GetFicTokenAsync(this ITokenAcquirer tokenAcquirer,
            AcquireTokenOptions? options = null,
            string? clientAssertion = null,
            string scope = "api://AzureAdTokenExchange/.default",
            CancellationToken cancellationToken = default)
        {
            AcquireTokenOptions? tokenAcquisitionOptions;
            if (clientAssertion != null)
            {
                tokenAcquisitionOptions = options != null ? options.Clone() : new AcquireTokenOptions();
                tokenAcquisitionOptions.WithClientAssertion(clientAssertion);
            }
            else
            {
                tokenAcquisitionOptions = options;
            }

            return await tokenAcquirer.GetTokenForAppAsync(scope, tokenAcquisitionOptions, cancellationToken);
        }


        /// <summary>
        /// Consider the client assertion for the token request.
        /// </summary>
        /// <param name="options">Options (can be null)</param>
        /// <param name="clientAssertion">client assertion (shouldn't be null)</param>
        /// <returns>The modified options. New options with the signed assertion if <paramref name="options"/> was null.</returns>
        public static AcquireTokenOptions WithClientAssertion(this AcquireTokenOptions options, string clientAssertion)
        {
            AcquireTokenOptions tokenAcquisitionOptions = options ?? new AcquireTokenOptions();

            tokenAcquisitionOptions.ExtraParameters ??= new Dictionary<string, object>();
            if (!tokenAcquisitionOptions.ExtraParameters.ContainsKey(Constants.ClientAssertion))
            {
                tokenAcquisitionOptions.ExtraParameters.Add(Constants.ClientAssertion, clientAssertion);
            }
            else
            {
                tokenAcquisitionOptions.ExtraParameters[Constants.ClientAssertion] = clientAssertion;
            }

            return tokenAcquisitionOptions;
        }

        /// <summary>
        /// Adds extra body parameters to the token acquisition request.
        /// Parameters are merged into any existing extra body parameters dictionary,
        /// so this can be composed with other extensions that also add body parameters.
        /// </summary>
        /// <param name="options">The acquire token options.</param>
        /// <param name="extraBodyParameters">The extra body parameters to include in the token request.</param>
        /// <returns>The modified options for fluent chaining.</returns>
        public static AcquireTokenOptions WithExtraBodyParameters(
            this AcquireTokenOptions options,
            IDictionary<string, string> extraBodyParameters)
        {
            if (options is null)
            {
                throw new System.ArgumentNullException(nameof(options));
            }

            if (extraBodyParameters is null || extraBodyParameters.Count == 0)
            {
                return options;
            }

            options.ExtraParameters ??= new Dictionary<string, object>();

            Dictionary<string, System.Func<CancellationToken, Task<string>>> asyncParams;
            if (options.ExtraParameters.TryGetValue(Constants.ExtraBodyParametersKey, out var existing) &&
                existing is Dictionary<string, System.Func<CancellationToken, Task<string>>> existingDict)
            {
                asyncParams = existingDict;
            }
            else
            {
                asyncParams = new Dictionary<string, System.Func<CancellationToken, Task<string>>>();
                options.ExtraParameters[Constants.ExtraBodyParametersKey] = asyncParams;
            }

            foreach (var kvp in extraBodyParameters)
            {
                string value = kvp.Value;
                asyncParams[kvp.Key] = _ => Task.FromResult(value);
            }

            return options;
        }
    }
}
