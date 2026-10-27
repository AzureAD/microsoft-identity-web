// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Identity.Web.Test.Integration
{

    /// <summary>
    /// This is a compilation test only. It is not meant to be run.
    /// </summary>
    public class GraphServiceClientTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS0649 // Field 'GraphServiceClientTests._authorizationHeaderProvider' is never assigned to, and will always have its default value null
        readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
#pragma warning restore CS0649 // Field 'GraphServiceClientTests._authorizationHeaderProvider' is never assigned to, and will always have its default value null
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable IDE0051 // Remove unused private members
        async Task TestAsync()
#pragma warning restore IDE0051 // Remove unused private members
        {
            GraphServiceClient graphServiceClient = new(new GraphAuthenticationProvider(_authorizationHeaderProvider, new GraphServiceClientOptions()));

            User? me = await graphServiceClient.Me.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
                    o.Scopes = new string[] { "user.read" };
                    o.RequestAppToken = true;
                    o.ProtocolScheme = "Pop";
                    o.AcquireTokenOptions.Claims = "claims";
                    o.AcquireTokenOptions.PopPublicKey = "";
                    o.AcquireTokenOptions.CorrelationId = Guid.NewGuid();
                    o.AcquireTokenOptions.UserFlow = "susi";
                    o.AcquireTokenOptions.AuthenticationOptionsName = "JwtBearer";
                    o.AcquireTokenOptions.Tenant = "TenantId";
                });
            }
            );

            MailFolderCollectionResponse? mailFolders = await graphServiceClient.Me.MailFolders.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
                    // Specify scopes for the request
                    o.Scopes = new string[] { "Mail.Read" };

                    // Specify the ASP.NET Core authentication scheme if needed (in the case
                    // of multiple authentication schemes)
                    // o.AuthenticationOptionsName = JwtBearerDefaults.AuthenticationScheme;
                });
            });

            int? appsInTenant = await graphServiceClient.Applications.Count.GetAsync(r =>
            {
                r.Options.WithAuthenticationOptions(o =>
                {
                    // It's an app permission. Requires an app token
                    o.RequestAppToken = true;
                });
            });
        }

        [Fact]
        public async Task AuthenticateRequestAsync_NonGraphUri_DoesNotSetAuthZHeaderAsync()
        {
            // arrange
            RequestInformation request = new()
            {
                URI = new Uri("http://www.contoso.com/")
            };

            GraphAuthenticationProvider graphAuthenticationProvider = new(_authorizationHeaderProvider, new GraphServiceClientOptions());

            // act
            await graphAuthenticationProvider.AuthenticateRequestAsync(request);

            // assert
            Assert.False(request.Headers.ContainsKey("Authorization"));
        }

        [Fact]
        public async Task AuthenticateRequestAsync_CustomRequestOptions_AreIsolatedAndReceiveGeneratedSessionKeyAsync()
        {
            // Arrange
            var requestOptions = new CustomGraphAuthenticationOptions
            {
                ProtocolScheme = "Pop",
                Scopes = null!,
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    LongRunningWebApiSessionKey = AcquireTokenOptions.LongRunningWebApiSessionKeyAuto,
                    ExtraParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Configured"] = "value"
                    }
                }
            };
            var authorizationHeaderProvider = new RecordingAuthorizationHeaderProvider();
            RequestInformation request = new()
            {
                URI = new Uri("https://graph.microsoft.com/v1.0/me"),
                HttpMethod = Method.GET
            };
            request.AddRequestOptions([requestOptions]);
            GraphAuthenticationProvider graphAuthenticationProvider = new(
                authorizationHeaderProvider,
                new GraphServiceClientOptions { Scopes = ["default-scope"] });

            // Act
            await graphAuthenticationProvider.AuthenticateRequestAsync(request);

            // Assert
            Assert.Equal(["default-scope"], authorizationHeaderProvider.CapturedScopes);
            Assert.NotSame(requestOptions, authorizationHeaderProvider.CapturedOptions);
            Assert.True(requestOptions.CloneInternalCalled);
            Assert.IsType<CustomGraphAuthenticationOptions>(authorizationHeaderProvider.CapturedOptions);
            Assert.NotSame(requestOptions.AcquireTokenOptions, authorizationHeaderProvider.CapturedOptions!.AcquireTokenOptions);
            Assert.Equal(
                "value",
                authorizationHeaderProvider.CapturedOptions.AcquireTokenOptions.ExtraParameters!["CONFIGURED"]);
            Assert.Single(requestOptions.AcquireTokenOptions.ExtraParameters);
            Assert.False(requestOptions.AcquireTokenOptions.ExtraParameters.ContainsKey("request"));
            Assert.Equal("generated-session-key", requestOptions.AcquireTokenOptions.LongRunningWebApiSessionKey);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void GraphOptionsClone_PreservesRuntimeTypeAndDictionaryComparers(int optionsKind)
        {
            // Arrange
            GraphServiceClientOptions options = optionsKind switch
            {
                0 => new GraphServiceClientOptions(),
                1 => new GraphAuthenticationOptions(),
                2 => new DerivedGraphAuthenticationOptions { Marker = "derived-value" },
                _ => throw new ArgumentOutOfRangeException(nameof(optionsKind))
            };
            options.Scopes = ["configured-scope"];
            options.User = new ClaimsPrincipal();
            options.AcquireTokenOptions.ExtraParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Configured"] = "value"
            };
            options.AcquireTokenOptions.ExtraHeadersParameters = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Configured"] = "header"
            };
            options.AcquireTokenOptions.ExtraQueryParameters = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Configured"] = "query"
            };

            // Act
            AuthorizationHeaderProviderOptions clone = options.Clone();

            // Assert
            Assert.Equal(options.GetType(), clone.GetType());
            GraphServiceClientOptions graphClone = Assert.IsAssignableFrom<GraphServiceClientOptions>(clone);
            Assert.Equal(options.Scopes, graphClone.Scopes);
            Assert.Same(options.User, graphClone.User);
            if (options is DerivedGraphAuthenticationOptions derived)
            {
                Assert.Equal(derived.Marker, Assert.IsType<DerivedGraphAuthenticationOptions>(clone).Marker);
            }
            Assert.NotSame(options.AcquireTokenOptions, clone.AcquireTokenOptions);
            Assert.Equal("value", clone.AcquireTokenOptions.ExtraParameters!["CONFIGURED"]);
            Assert.Equal("header", clone.AcquireTokenOptions.ExtraHeadersParameters!["CONFIGURED"]);
            Assert.Equal("query", clone.AcquireTokenOptions.ExtraQueryParameters!["CONFIGURED"]);
            clone.AcquireTokenOptions.ExtraParameters["CONFIGURED"] = "request";
            clone.AcquireTokenOptions.ExtraHeadersParameters["CONFIGURED"] = "request-header";
            clone.AcquireTokenOptions.ExtraQueryParameters["CONFIGURED"] = "request-query";
            Assert.Single(clone.AcquireTokenOptions.ExtraParameters);
            Assert.Single(clone.AcquireTokenOptions.ExtraHeadersParameters);
            Assert.Single(clone.AcquireTokenOptions.ExtraQueryParameters);
            Assert.Equal("value", options.AcquireTokenOptions.ExtraParameters["Configured"]);
            Assert.Equal("header", options.AcquireTokenOptions.ExtraHeadersParameters["Configured"]);
            Assert.Equal("query", options.AcquireTokenOptions.ExtraQueryParameters["Configured"]);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_ConcurrentDefaultOptions_AreIsolatedAsync()
        {
            // Arrange
            var defaultOptions = new GraphServiceClientOptions
            {
                ProtocolScheme = "Pop",
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    LongRunningWebApiSessionKey = AcquireTokenOptions.LongRunningWebApiSessionKeyAuto,
                    ExtraParameters = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Configured"] = "value"
                    }
                }
            };
            using var release = new SemaphoreSlim(0, 2);
            var authorizationHeaderProvider = new RecordingAuthorizationHeaderProvider
            {
                PendingHeader = async () =>
                {
                    await release.WaitAsync();
                    return "test-authorization-header";
                }
            };
            var provider = new GraphAuthenticationProvider(authorizationHeaderProvider, defaultOptions);
            var firstRequest = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/me"), HttpMethod = Method.GET };
            var secondRequest = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/me"), HttpMethod = Method.GET };

            // Act
            Task first = provider.AuthenticateRequestAsync(firstRequest);
            Task second = provider.AuthenticateRequestAsync(secondRequest);
            release.Release(2);
            await Task.WhenAll(first, second);

            // Assert
            Assert.Equal(2, authorizationHeaderProvider.OptionsHistory.Count);
            AuthorizationHeaderProviderOptions firstOptions = authorizationHeaderProvider.OptionsHistory[0];
            AuthorizationHeaderProviderOptions secondOptions = authorizationHeaderProvider.OptionsHistory[1];
            Assert.IsType<GraphServiceClientOptions>(firstOptions);
            Assert.NotSame(defaultOptions, firstOptions);
            Assert.NotSame(firstOptions, secondOptions);
            Assert.NotSame(firstOptions.AcquireTokenOptions, secondOptions.AcquireTokenOptions);
            Assert.NotSame(firstOptions.AcquireTokenOptions.ExtraParameters, secondOptions.AcquireTokenOptions.ExtraParameters);
            Assert.Equal("value", firstOptions.AcquireTokenOptions.ExtraParameters!["CONFIGURED"]);
            Assert.Equal("value", secondOptions.AcquireTokenOptions.ExtraParameters!["CONFIGURED"]);
            Assert.NotEqual(firstOptions.AcquireTokenOptions.LongRunningWebApiSessionKey, secondOptions.AcquireTokenOptions.LongRunningWebApiSessionKey);
            Assert.Single(defaultOptions.AcquireTokenOptions.ExtraParameters);
            Assert.Equal(AcquireTokenOptions.LongRunningWebApiSessionKeyAuto, defaultOptions.AcquireTokenOptions.LongRunningWebApiSessionKey);
            Assert.True(firstRequest.Headers.ContainsKey("Authorization"));
            Assert.True(secondRequest.Headers.ContainsKey("Authorization"));
        }

        [Fact]
        public async Task AuthenticateRequestAsync_RequestOwnedSessionKey_IsReturnedAndReusedAsync()
        {
            // Arrange
            var requestOptions = new GraphAuthenticationOptions
            {
                ProtocolScheme = "Pop",
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    LongRunningWebApiSessionKey = AcquireTokenOptions.LongRunningWebApiSessionKeyAuto
                }
            };
            var authorizationHeaderProvider = new RecordingAuthorizationHeaderProvider();
            var provider = new GraphAuthenticationProvider(authorizationHeaderProvider, new GraphServiceClientOptions());
            var firstRequest = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/me"), HttpMethod = Method.GET };
            var secondRequest = new RequestInformation { URI = new Uri("https://graph.microsoft.com/v1.0/me"), HttpMethod = Method.GET };
            firstRequest.AddRequestOptions([requestOptions]);
            secondRequest.AddRequestOptions([requestOptions]);

            // Act
            await provider.AuthenticateRequestAsync(firstRequest);
            await provider.AuthenticateRequestAsync(secondRequest);

            // Assert
            Assert.Equal("generated-session-key", requestOptions.AcquireTokenOptions.LongRunningWebApiSessionKey);
            Assert.All(authorizationHeaderProvider.OptionsHistory, options =>
                Assert.Equal("generated-session-key", options.AcquireTokenOptions.LongRunningWebApiSessionKey));
        }

        [Fact]
        public async Task AuthenticateRequestAsync_CancelledAuthentication_DoesNotModifySourceOptionsAsync()
        {
            // Arrange
            var defaultOptions = new GraphServiceClientOptions
            {
                ProtocolScheme = "Pop",
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    LongRunningWebApiSessionKey = AcquireTokenOptions.LongRunningWebApiSessionKeyAuto,
                    ExtraParameters = new Dictionary<string, object>
                    {
                        ["configured"] = "value"
                    }
                }
            };
            var requestOptions = new GraphAuthenticationOptions
            {
                ProtocolScheme = "Pop",
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    LongRunningWebApiSessionKey = AcquireTokenOptions.LongRunningWebApiSessionKeyAuto,
                    ExtraParameters = new Dictionary<string, object>
                    {
                        ["request-configured"] = "value"
                    }
                }
            };
            var authorizationHeaderProvider = new RecordingAuthorizationHeaderProvider
            {
                Exception = new OperationCanceledException()
            };
            RequestInformation request = new()
            {
                URI = new Uri("https://graph.microsoft.com/v1.0/me"),
                HttpMethod = Method.GET
            };
            request.AddRequestOptions([requestOptions]);
            GraphAuthenticationProvider graphAuthenticationProvider = new(authorizationHeaderProvider, defaultOptions);

            // Act
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => graphAuthenticationProvider.AuthenticateRequestAsync(request));

            // Assert
            Assert.Single(defaultOptions.AcquireTokenOptions.ExtraParameters);
            Assert.False(defaultOptions.AcquireTokenOptions.ExtraParameters.ContainsKey("request"));
            Assert.Equal(
                AcquireTokenOptions.LongRunningWebApiSessionKeyAuto,
                defaultOptions.AcquireTokenOptions.LongRunningWebApiSessionKey);
            Assert.Single(requestOptions.AcquireTokenOptions.ExtraParameters);
            Assert.False(requestOptions.AcquireTokenOptions.ExtraParameters.ContainsKey("request"));
            Assert.Equal(
                AcquireTokenOptions.LongRunningWebApiSessionKeyAuto,
                requestOptions.AcquireTokenOptions.LongRunningWebApiSessionKey);
        }

        [Fact]
        public async Task AuthenticateRequestAsync_BearerRequest_PreservesRequestDestinationOptionsAsync()
        {
            // Arrange
            var authorizationHeaderProvider = new RecordingAuthorizationHeaderProvider();
            RequestInformation request = new()
            {
                URI = new Uri("https://graph.microsoft.com/v1.0/me/messages"),
                HttpMethod = Method.POST
            };
            GraphAuthenticationProvider graphAuthenticationProvider = new(
                authorizationHeaderProvider,
                new GraphServiceClientOptions { ProtocolScheme = "Bearer" });

            // Act
            await graphAuthenticationProvider.AuthenticateRequestAsync(request);

            // Assert
            Assert.Equal("graph.microsoft.com", authorizationHeaderProvider.CapturedOptions!.BaseUrl);
            Assert.Equal("/v1.0/me/messages", authorizationHeaderProvider.CapturedOptions.RelativePath);
            Assert.Equal("POST", authorizationHeaderProvider.CapturedOptions.HttpMethod);
        }

        private sealed class CustomGraphAuthenticationOptions : GraphAuthenticationOptions
        {
            public bool CloneInternalCalled { get; private set; }

            protected override AuthorizationHeaderProviderOptions CloneInternal()
            {
                CloneInternalCalled = true;
                return base.CloneInternal();
            }
        }

        private sealed class DerivedGraphAuthenticationOptions : GraphAuthenticationOptions
        {
            public string? Marker { get; set; }
        }

        private sealed class RecordingAuthorizationHeaderProvider : IAuthorizationHeaderProvider
        {
            public AuthorizationHeaderProviderOptions? CapturedOptions { get; private set; }

            public IEnumerable<string>? CapturedScopes { get; private set; }

            public Exception? Exception { get; init; }

            public List<AuthorizationHeaderProviderOptions> OptionsHistory { get; } = new();

            public Func<Task<string>>? PendingHeader { get; init; }

            public Task<string> CreateAuthorizationHeaderAsync(
                IEnumerable<string> scopes,
                AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
                ClaimsPrincipal? claimsPrincipal = null,
                CancellationToken cancellationToken = default)
            {
                CapturedScopes = scopes.ToArray();
                CapturedOptions = authorizationHeaderProviderOptions;
                OptionsHistory.Add(CapturedOptions!);
                CapturedOptions!.AcquireTokenOptions.ExtraParameters ??= new Dictionary<string, object>();
                CapturedOptions.AcquireTokenOptions.ExtraParameters["request"] = "value";
                if (CapturedOptions.AcquireTokenOptions.LongRunningWebApiSessionKey == AcquireTokenOptions.LongRunningWebApiSessionKeyAuto)
                {
                    CapturedOptions.AcquireTokenOptions.LongRunningWebApiSessionKey =
                        OptionsHistory.Count == 1 ? "generated-session-key" : "generated-session-key-2";
                }
                if (PendingHeader is not null)
                {
                    return PendingHeader();
                }

                return Exception is null
                    ? Task.FromResult("Bearer token")
                    : Task.FromException<string>(Exception);
            }

            public Task<string> CreateAuthorizationHeaderForAppAsync(
                string scopes,
                AuthorizationHeaderProviderOptions? downstreamApiOptions = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task<string> CreateAuthorizationHeaderForUserAsync(
                IEnumerable<string> scopes,
                AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null,
                ClaimsPrincipal? claimsPrincipal = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
