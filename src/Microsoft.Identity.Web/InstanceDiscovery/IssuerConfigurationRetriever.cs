// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;

namespace Microsoft.Identity.Web.InstanceDiscovery
{
	/// <summary>
	/// An implementation of IConfigurationRetriever geared towards Azure AD issuers metadata.
	/// </summary>
	internal class IssuerConfigurationRetriever : IConfigurationRetriever<IssuerMetadata>
	{
		/// <summary>Retrieves a populated configuration given an address and an <see cref="T:Microsoft.IdentityModel.Protocols.IDocumentRetriever"/>.</summary>
		/// <param name="address">Address of the discovery document.</param>
		/// <param name="retriever">The <see cref="T:Microsoft.IdentityModel.Protocols.IDocumentRetriever"/> to use to read the discovery document.</param>
		/// <param name="cancel">A cancellation token that can be used by other objects or threads to receive notice of cancellation. <see cref="T:System.Threading.CancellationToken"/>.</param>
		/// <returns>
		/// A <see cref="Task{IssuerMetadata}"/> that, when completed, returns <see cref="IssuerMetadata"/> from the configuration.
		/// </returns>
		/// <exception cref="ArgumentNullException">address - Azure AD Issuer metadata address URL is required
		/// or retriever - No metadata document retriever is provided.</exception>
		public async Task<IssuerMetadata> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
		{
			if (string.IsNullOrEmpty(address))
			{
				throw new ArgumentNullException(nameof(address), IDWebErrorMessage.IssuerMetadataUrlIsRequired);
			}

			if (retriever == null)
			{
				throw new ArgumentNullException(nameof(retriever), IDWebErrorMessage.NoMetadataDocumentRetrieverProvided);
			}

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
			};

			string doc = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
			return JsonSerializer.Deserialize<IssuerMetadata>(doc, options)!; // Note: The analyzer says Deserialize can return null, but the method comment says it just throws exceptions.
		}
	}
}
