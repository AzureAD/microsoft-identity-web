// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquisitionOptionsHelperTests
    {
        [Fact]
        public void CreateTokenAcquisitionOptionsFromApiOptions_WithNullOptions_ReturnsDefaultValues()
        {
            // Arrange
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null;
            var cancellationToken = new CancellationToken();

            // Act
            var result = TokenAcquisitionOptionsHelper.CreateTokenAcquisitionOptionsFromApiOptions(
                downstreamApiOptions,
                cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.AuthenticationOptionsName);
            Assert.Equal(cancellationToken, result.CancellationToken);
            Assert.Null(result.Claims);
            Assert.Equal(Guid.Empty, result.CorrelationId);
            Assert.Null(result.ExtraHeadersParameters);
            Assert.Null(result.ExtraQueryParameters);
            Assert.Null(result.ExtraParameters);
            Assert.False(result.ForceRefresh);
            Assert.Null(result.LongRunningWebApiSessionKey);
            Assert.Null(result.ManagedIdentity);
            Assert.Null(result.Tenant);
            Assert.Null(result.UserFlow);
            Assert.Null(result.PopPublicKey);
            Assert.Null(result.FmiPath);
        }

        [Fact]
        public void CreateTokenAcquisitionOptionsFromApiOptions_WithValidOptions_MapsAllProperties()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();
            var extraHeaders = new Dictionary<string, string> { { "header1", "value1" } };
            var extraQuery = new Dictionary<string, string> { { "query1", "value1" } };
            var extraParams = new Dictionary<string, object> { { "param1", "value1" } };
            var managedIdentity = new ManagedIdentityOptions { UserAssignedClientId = "test-client-id" };

            var downstreamApiOptions = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "TestAuthOptions",
                    Claims = "custom-claims",
                    CorrelationId = correlationId,
                    ExtraHeadersParameters = extraHeaders,
                    ExtraQueryParameters = extraQuery,
                    ExtraParameters = extraParams,
                    ForceRefresh = true,
                    LongRunningWebApiSessionKey = "test-session-key",
                    ManagedIdentity = managedIdentity,
                    Tenant = "test-tenant",
                    UserFlow = "test-userflow",
                    PopPublicKey = "test-pop-key",
                    FmiPath = "test-fmi-path"
                }
            };

            // Act
            var result = TokenAcquisitionOptionsHelper.CreateTokenAcquisitionOptionsFromApiOptions(
                downstreamApiOptions,
                cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestAuthOptions", result.AuthenticationOptionsName);
            Assert.Equal(cancellationToken, result.CancellationToken);
            Assert.Equal("custom-claims", result.Claims);
            Assert.Equal(correlationId, result.CorrelationId);
            Assert.Same(extraHeaders, result.ExtraHeadersParameters);
            Assert.Same(extraQuery, result.ExtraQueryParameters);
            Assert.Same(extraParams, result.ExtraParameters);
            Assert.True(result.ForceRefresh);
            Assert.Equal("test-session-key", result.LongRunningWebApiSessionKey);
            Assert.Same(managedIdentity, result.ManagedIdentity);
            Assert.Equal("test-tenant", result.Tenant);
            Assert.Equal("test-userflow", result.UserFlow);
            Assert.Equal("test-pop-key", result.PopPublicKey);
            Assert.Equal("test-fmi-path", result.FmiPath);
        }

        [Fact]
        public void CreateTokenAcquisitionOptionsFromApiOptions_WithPartialOptions_HandlesNullProperties()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var downstreamApiOptions = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "TestAuthOptions",
                    // Leave other properties as null/default
                }
            };

            // Act
            var result = TokenAcquisitionOptionsHelper.CreateTokenAcquisitionOptionsFromApiOptions(
                downstreamApiOptions,
                cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestAuthOptions", result.AuthenticationOptionsName);
            Assert.Equal(cancellationToken, result.CancellationToken);
            Assert.Null(result.Claims);
            Assert.Equal(Guid.Empty, result.CorrelationId); // Default when null in source
            Assert.Null(result.ExtraHeadersParameters);
            Assert.Null(result.ExtraQueryParameters);
            Assert.Null(result.ExtraParameters);
            Assert.False(result.ForceRefresh); // Default when null in source
            Assert.Null(result.LongRunningWebApiSessionKey);
            Assert.Null(result.ManagedIdentity);
            Assert.Null(result.Tenant);
            Assert.Null(result.UserFlow);
            Assert.Null(result.PopPublicKey);
            Assert.Null(result.FmiPath);
        }

        [Fact]
        public void CreateTokenAcquisitionOptionsFromApiOptions_WithEmptyCorrelationId_UsesEmptyGuid()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var downstreamApiOptions = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    CorrelationId = null // Explicitly set to null
                }
            };

            // Act
            var result = TokenAcquisitionOptionsHelper.CreateTokenAcquisitionOptionsFromApiOptions(
                downstreamApiOptions,
                cancellationToken);

            // Assert
            Assert.Equal(Guid.Empty, result.CorrelationId);
        }

        [Fact]
        public void CreateTokenAcquisitionOptionsFromApiOptions_WithCancellationToken_SetsCancellationToken()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            AuthorizationHeaderProviderOptions? downstreamApiOptions = null;

            // Act
            var result = TokenAcquisitionOptionsHelper.CreateTokenAcquisitionOptionsFromApiOptions(
                downstreamApiOptions,
                cancellationToken);

            // Assert
            Assert.Equal(cancellationToken, result.CancellationToken);
        }

        [Fact]
        public void UpdateOriginalTokenAcquisitionOptions_WithNullAcquireTokenOptions_DoesNotThrow()
        {
            // Arrange
            AcquireTokenOptions? acquireTokenOptions = null;
            var newTokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                LongRunningWebApiSessionKey = "test-key"
            };

            // Act & Assert - Should not throw
            TokenAcquisitionOptionsHelper.UpdateOriginalTokenAcquisitionOptions(
                acquireTokenOptions,
                newTokenAcquisitionOptions);
        }

        [Fact]
        public void UpdateOriginalTokenAcquisitionOptions_WithNullNewOptions_DoesNotThrow()
        {
            // Arrange
            var acquireTokenOptions = new AcquireTokenOptions
            {
                LongRunningWebApiSessionKey = "original-key"
            };
            TokenAcquisitionOptions? newTokenAcquisitionOptions = null;

            // Act & Assert - Should not throw
            TokenAcquisitionOptionsHelper.UpdateOriginalTokenAcquisitionOptions(
                acquireTokenOptions,
                newTokenAcquisitionOptions!);

            // The original value should remain unchanged
            Assert.Equal("original-key", acquireTokenOptions.LongRunningWebApiSessionKey);
        }

        [Fact]
        public void UpdateOriginalTokenAcquisitionOptions_WithBothNull_DoesNotThrow()
        {
            // Arrange
            AcquireTokenOptions? acquireTokenOptions = null;
            TokenAcquisitionOptions? newTokenAcquisitionOptions = null;

            // Act & Assert - Should not throw
            TokenAcquisitionOptionsHelper.UpdateOriginalTokenAcquisitionOptions(
                acquireTokenOptions,
                newTokenAcquisitionOptions!);
        }

        [Fact]
        public void UpdateOriginalTokenAcquisitionOptions_WithValidOptions_UpdatesLongRunningSessionKey()
        {
            // Arrange
            var acquireTokenOptions = new AcquireTokenOptions
            {
                LongRunningWebApiSessionKey = "original-key",
                AuthenticationOptionsName = "original-auth"
            };

            var newTokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                LongRunningWebApiSessionKey = "updated-key"
            };

            // Act
            TokenAcquisitionOptionsHelper.UpdateOriginalTokenAcquisitionOptions(
                acquireTokenOptions,
                newTokenAcquisitionOptions);

            // Assert
            Assert.Equal("updated-key", acquireTokenOptions.LongRunningWebApiSessionKey);
            // Verify other properties are not affected
            Assert.Equal("original-auth", acquireTokenOptions.AuthenticationOptionsName);
        }

        [Fact]
        public void UpdateOriginalTokenAcquisitionOptions_WithNullSessionKey_UpdatesToNull()
        {
            // Arrange
            var acquireTokenOptions = new AcquireTokenOptions
            {
                LongRunningWebApiSessionKey = "original-key"
            };

            var newTokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                LongRunningWebApiSessionKey = null
            };

            // Act
            TokenAcquisitionOptionsHelper.UpdateOriginalTokenAcquisitionOptions(
                acquireTokenOptions,
                newTokenAcquisitionOptions);

            // Assert
            Assert.Null(acquireTokenOptions.LongRunningWebApiSessionKey);
        }
    }
}