// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TokenCache.SnapshotPoc
{
    internal sealed class CountingDistributedCache : IDistributedCache
    {
        private readonly MemoryDistributedCache _inner =
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        internal int Reads { get; private set; }
        internal int Writes { get; private set; }

        public byte[]? Get(string key)
        {
            Reads++;
            return _inner.Get(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            Reads++;
            return _inner.GetAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Writes++;
            _inner.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Writes++;
            return _inner.SetAsync(key, value, options, token);
        }

        public void Refresh(string key) => _inner.Refresh(key);
        public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);
        public void Remove(string key) => _inner.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);
    }

    internal sealed class TokenEndpoint : HttpMessageHandler, IMsalHttpClientFactory
    {
        internal const string TenantA = "11111111-1111-1111-1111-111111111111";
        internal const string TenantB = "22222222-2222-2222-2222-222222222222";
        internal const string Scope = "https://resource.example/.default";
        internal const string ClientId = "33333333-3333-3333-3333-333333333333";
        private readonly HttpClient _client;

        internal int TokenRequests { get; private set; }

        internal TokenEndpoint()
        {
            _client = new HttpClient(this, disposeHandler: false);
        }

        public HttpClient GetHttpClient() => _client;

        internal IConfidentialClientApplication CreateApplication(SnapshotTokenCacheProvider provider)
        {
            var app = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithClientSecret("synthetic-poc-credential")
                .WithAuthority($"https://login.microsoftonline.com/{TenantA}")
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(false)
                .WithHttpClientFactory(this)
                .Build();
            provider.Initialize(app.AppTokenCache);
            provider.Initialize(app.UserTokenCache);
            return app;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Uri uri = request.RequestUri ?? throw new InvalidOperationException("Missing request URI.");
            string tenant = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)[0];
            if (uri.Host != "login.microsoftonline.com" || (tenant != TenantA && tenant != TenantB))
            {
                throw new InvalidOperationException("Unexpected request outside the synthetic token endpoint.");
            }

            object response;
            if (request.Method == HttpMethod.Get && uri.AbsolutePath.EndsWith("/.well-known/openid-configuration", StringComparison.Ordinal))
            {
                response = new
                {
                    authorization_endpoint = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize",
                    token_endpoint = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token",
                    issuer = $"https://login.microsoftonline.com/{tenant}/v2.0"
                };
            }
            else if (request.Method == HttpMethod.Post && uri.AbsolutePath.EndsWith("/oauth2/v2.0/token", StringComparison.Ordinal))
            {
                TokenRequests++;
                response = new
                {
                    access_token = $"synthetic-token-{tenant}-{TokenRequests}",
                    token_type = "Bearer",
                    expires_in = 3600,
                    ext_expires_in = 7200,
                    scope = Scope
                };
            }
            else
            {
                throw new InvalidOperationException("Unexpected synthetic endpoint request.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
