// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web.Sidecar.Configuration;
using Microsoft.Identity.Web.Sidecar.Pop;
using Microsoft.IdentityModel.Protocols.SignedHttpRequest;
using Xunit;

namespace Sidecar.Tests;

/// <summary>
/// Unit tests for the operator-configurable SHR PoP validation flags. Verifies that (a) with no
/// <c>Sidecar:PopValidation</c> config the DTO -&gt; <see cref="SignedHttpRequestValidationParameters"/>
/// mapping reproduces the secure defaults, and (b) a bound config section overrides exactly the
/// specified flags and nothing else (body-hash <c>b</c> stays off, server nonce stays unset).
/// </summary>
public class PopValidationOptionsMappingTests
{
    [Fact]
    public void CreateValidationParameters_WithDefaultOptions_MatchesSecureDefaults()
    {
        // Arrange: a brand-new options object is exactly what binding produces when no
        // Sidecar:PopValidation section is present.
        var options = new PopValidationOptions();

        // Act
        SignedHttpRequestValidationParameters parameters =
            ShrPopValidationService.CreateValidationParameters(options);

        // Assert: byte-for-byte the pre-config behavior (m/u/p/ts ON; q/h/b OFF; accept-unsigned ON;
        // 5-minute lifetime).
        Assert.True(parameters.ValidateM);
        Assert.True(parameters.ValidateU);
        Assert.True(parameters.ValidateP);
        Assert.True(parameters.ValidateTs);
        Assert.False(parameters.ValidateQ);
        Assert.False(parameters.ValidateH);
        Assert.False(parameters.ValidateB);
        Assert.True(parameters.AcceptUnsignedHeaders);
        Assert.True(parameters.AcceptUnsignedQueryParameters);
        Assert.Equal(TimeSpan.FromMinutes(5), parameters.SignedHttpRequestLifetime);
        Assert.False(parameters.ValidatePresentClaims);
        Assert.Equal(new[] { "m", "p" }, parameters.ClaimsToValidateWhenPresent);

        // Server nonce is out of scope (timestamp-only): the delegate must stay unset, and jku key
        // resolution must stay off (no outbound egress).
        Assert.Null(parameters.NonceValidatorAsync);
        Assert.False(parameters.AllowResolvingPopKeyFromJku);
    }

    [Fact]
    public void CreateValidationParameters_WithBoundConfigOverrides_ReflectsOnlyThoseOverrides()
    {
        // Arrange: bind a Sidecar:PopValidation subsection that flips a subset of flags, exactly as an
        // operator would in appsettings.json.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sidecar:PopValidation:ValidateQ"] = "true",
                ["Sidecar:PopValidation:SignedHttpRequestLifetime"] = "00:02:00",

                // Header binding is not a supported option: the sidecar receives only the request
                // line, never the signed headers. These keys are set to insecure values to prove
                // they have no effect and cannot enable header binding.
                ["Sidecar:PopValidation:ValidateH"] = "true",
                ["Sidecar:PopValidation:AcceptUnsignedHeaders"] = "false",
            })
            .Build();

        var sidecarOptions = new SidecarOptions();
        configuration.GetSection("Sidecar").Bind(sidecarOptions);

        // Act
        SignedHttpRequestValidationParameters parameters =
            ShrPopValidationService.CreateValidationParameters(sidecarOptions.PopValidation);

        // Assert: exactly the two supported overrides took effect...
        Assert.True(parameters.ValidateQ);
        Assert.Equal(TimeSpan.FromMinutes(2), parameters.SignedHttpRequestLifetime);

        // ...header binding is not operator-configurable (only the request line reaches the
        // sidecar), so it stays off regardless of the keys set above...
        Assert.False(parameters.ValidateH);
        Assert.True(parameters.AcceptUnsignedHeaders);

        // ...and nothing else drifted from the secure defaults.
        Assert.True(parameters.ValidateM);
        Assert.True(parameters.ValidateU);
        Assert.True(parameters.ValidateP);
        Assert.True(parameters.ValidateTs);
        Assert.True(parameters.AcceptUnsignedQueryParameters);
        Assert.False(parameters.ValidatePresentClaims);
        Assert.Equal(new[] { "m", "p" }, parameters.ClaimsToValidateWhenPresent);

        // Body-hash (b) is never operator-configurable and must remain OFF, and server nonce stays unset.
        Assert.False(parameters.ValidateB);
        Assert.Null(parameters.NonceValidatorAsync);
    }
}
