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
        /// <param name="tokenAcquirer"></param>
        /// <param name="options"></param>
        /// <param name="clientAssertion"></param>
        /// <param name="scope"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<AcquireTokenResult> GetFicAsync(this ITokenAcquirer tokenAcquirer,
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
        /// Consider the client assertion
        /// </summary>
        /// <param name="options">Options (can be null)</param>
        /// <param name="clientAssertion">client assertion</param>
        /// <returns></returns>
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
    }
}
