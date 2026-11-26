// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Experimental;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using NSubstitute;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.Identity.Web.Test;

[Collection(nameof(TokenAcquirerFactorySingletonProtection))]
public class CertificateCredentialRecoveryTests
{
    private const string Scope = "https://graph.microsoft.com/.default";

    [Theory]
    [InlineData(700027)]
    [InlineData(700024)]
    [InlineData(7000214)]
    [InlineData(1000502)]
    [InlineData(7000274)]
    [InlineData(7000277)]
    public async Task InMemoryCertificate_TransientRejection_RecoversAndRemainsCached(int errorCode)
    {
        // Arrange
        using var certificate = CreateCertificate();
        var credential = new CredentialDescription { SourceType = CredentialSource.Certificate, Certificate = certificate };
        string originalId = credential.Id;
        var factory = CreateFactory(credential);
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        var rejection = http.AddMockHandler(CreateRejection(errorCode));
        var success = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
        var acquisition = services.GetRequiredService<ITokenAcquisition>();

        // Act
        var result = await acquisition.GetAuthenticationResultForAppAsync(Scope);
        var cached = await acquisition.GetAuthenticationResultForAppAsync(Scope);

        // Assert
        Assert.NotNull(rejection.ActualRequestPostData);
        Assert.NotNull(success.ActualRequestPostData);
        Assert.Same(certificate, credential.Certificate);
        Assert.Equal(originalId, credential.Id);
        Assert.Equal(result.AccessToken, cached.AccessToken);
        Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
    }

    [Theory]
    [InlineData("app")]
    [InlineData("user")]
    [InlineData("authCode")]
    public async Task InMemoryCertificate_PersistentRejection_PreservesErrorAndLaterRecovers(string flow)
    {
        // Arrange
        using var certificate = CreateCertificate();
        var credential = new CredentialDescription { SourceType = CredentialSource.Certificate, Certificate = certificate };
        string originalId = credential.Id;
        var factory = CreateFactory(credential);
        var logger = Substitute.For<ILogger<TokenAcquisition>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        factory.Services.AddSingleton<ILogger<TokenAcquisition>>(new LoggerMock<TokenAcquisition>(logger));
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        http.AddMockHandler(CreateRejection(700027));
        http.AddMockHandler(CreateRejection(700027));
        var acquisition = (TokenAcquisition)services.GetRequiredService<ITokenAcquisition>();

        // Act
        var exception = await Assert.ThrowsAsync<MsalServiceException>(() => AcquireAsync(acquisition, flow));
        var success = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
        await AcquireAsync(acquisition, flow);

        // Assert
        Assert.Equal("invalid_client", exception.ErrorCode);
        Assert.Equal(401, exception.StatusCode);
        Assert.Contains("700027", exception.ResponseBody, StringComparison.Ordinal);
        Assert.Same(certificate, credential.Certificate);
        Assert.Equal(originalId, credential.Id);
        Assert.NotNull(success.ActualRequestPostData);
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Is<EventId>(id => id.Id == 300),
            Arg.Is<object>(state => state.ToString()!.Contains("Certificate error detected", StringComparison.Ordinal)),
            Arg.Any<MsalServiceException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData("app", false)]
    [InlineData("app", true)]
    [InlineData("user", false)]
    [InlineData("user", true)]
    [InlineData("authCode", false)]
    [InlineData("authCode", true)]
    public async Task RetryCannotLoadCertificate_PreservesServiceAndLoadingErrors(string flow, bool loaderThrows)
    {
        // Arrange
        using var certificate = CreateCertificate();
        var credential = new CredentialDescription { SourceType = CredentialSource.Base64Encoded };
        var factory = CreateFactory(credential);
        var loadingException = new InvalidOperationException("Certificate source unavailable");
        var loader = Substitute.For<ICredentialSourceLoader>();
        loader.CredentialSource.Returns(CredentialSource.Base64Encoded);
        int loads = 0;
        loader.LoadIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
            .Returns(call =>
            {
                if (++loads == 1)
                {
                    var description = call.Arg<CredentialDescription>();
                    description.Certificate = certificate;
                    description.CachedValue = certificate;
                }
                else if (loaderThrows)
                {
                    throw loadingException;
                }
                return Task.CompletedTask;
            });
        factory.Services.AddSingleton(loader);
        var events = new List<CertificateChangeEventArg>();
        var observer = Substitute.For<ICertificatesObserver>();
        observer.When(value => value.OnClientCertificateChanged(Arg.Any<CertificateChangeEventArg>()))
            .Do(call => events.Add(call.Arg<CertificateChangeEventArg>()));
        factory.Services.AddSingleton(observer);
        var logger = Substitute.For<ILogger<TokenAcquisition>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        factory.Services.AddSingleton<ILogger<TokenAcquisition>>(new LoggerMock<TokenAcquisition>(logger));
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        http.AddMockHandler(CreateRejection(700027));
        var acquisition = (TokenAcquisition)services.GetRequiredService<ITokenAcquisition>();

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => AcquireAsync(acquisition, flow));

        // Assert
        Assert.StartsWith(IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded, exception.Message, StringComparison.Ordinal);
        var deselection = Assert.Single(events, value => value.Action == CerticateObserverAction.Deselected);
        var serviceException = Assert.IsType<MsalServiceException>(deselection.ThrownException);
        Assert.Equal(401, serviceException.StatusCode);
        Assert.Contains("700027", serviceException.ResponseBody, StringComparison.Ordinal);
        if (loaderThrows)
        {
            var aggregate = Assert.IsType<AggregateException>(exception.InnerException);
            Assert.Collection(aggregate.InnerExceptions,
                value => Assert.Same(serviceException, value),
                value => Assert.Same(loadingException, value));
        }
        else
        {
            Assert.Same(serviceException, exception.InnerException);
        }
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Is<EventId>(id => id.Id == 300),
            Arg.Is<object>(state => state.ToString()!.Contains("IDW10501", StringComparison.Ordinal)),
            Arg.Any<ArgumentException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InMemoryCertificate_RejectionDoesNotDisableSharedDescriptionOrCachedTokens()
    {
        // Arrange
        using var certificate = CreateCertificate();
        var credential = new CredentialDescription { SourceType = CredentialSource.Certificate, Certificate = certificate };
        var factory = CreateFactory(credential);
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        var first = services.GetRequiredService<ITokenAcquisition>();
        var second = ActivatorUtilities.CreateInstance<TokenAcquisition>(services);
        http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
        var initial = await first.GetAuthenticationResultForAppAsync(Scope);
        http.AddMockHandler(CreateRejection(700027));
        http.AddMockHandler(CreateRejection(700027));

        // Act
        await Assert.ThrowsAsync<MsalServiceException>(() => second.GetAuthenticationResultForAppAsync(
            Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions { ForceRefresh = true }));
        var cached = await first.GetAuthenticationResultForAppAsync(Scope);
        http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
        var recovered = await second.GetAuthenticationResultForAppAsync(
            Scope, tokenAcquisitionOptions: new TokenAcquisitionOptions { ForceRefresh = true });

        // Assert
        Assert.Same(certificate, credential.Certificate);
        Assert.Equal(initial.AccessToken, cached.AccessToken);
        Assert.Equal(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
        Assert.Equal(TokenSource.IdentityProvider, recovered.AuthenticationResultMetadata.TokenSource);
    }

    [Theory]
    [InlineData(CredentialSource.Certificate)]
    [InlineData(CredentialSource.Base64Encoded)]
    public async Task CustomLoader_CanRotateCertificateAfterRejection(CredentialSource source)
    {
        // Arrange
        using var original = CreateCertificate();
        using var replacement = CreateCertificate();
        var credential = new CredentialDescription { SourceType = source, Certificate = original };
        var factory = CreateFactory(credential);
        var loader = Substitute.For<ICredentialSourceLoader>();
        loader.CredentialSource.Returns(source);
        int loads = 0;
        loader.LoadIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
            .Returns(call =>
            {
                var description = call.Arg<CredentialDescription>();
                description.Certificate = ++loads == 1 ? original : replacement;
                description.CachedValue = description.Certificate;
                return Task.CompletedTask;
            });
        factory.Services.AddSingleton(loader);
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        http.AddMockHandler(CreateRejection(700027));
        var success = http.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());

        // Act
        await services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(Scope);

        // Assert
        Assert.Equal(2, loads);
        Assert.Same(replacement, credential.Certificate);
        Assert.NotNull(success.ActualRequestPostData);
    }

    [Fact]
    public async Task InMemoryCertificate_UnrelatedRejection_DoesNotRetryOrMutateCredential()
    {
        // Arrange
        using var certificate = CreateCertificate();
        var credential = new CredentialDescription { SourceType = CredentialSource.Certificate, Certificate = certificate };
        var factory = CreateFactory(credential);
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        http.AddMockHandler(CreateRejection(500011));

        // Act
        var exception = await Assert.ThrowsAsync<MsalServiceException>(() =>
            services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(Scope));

        // Assert
        Assert.Contains("500011", exception.ResponseBody, StringComparison.Ordinal);
        Assert.Same(certificate, credential.Certificate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryThrowsUnrelatedArgumentException_PropagatesUnchanged(bool matchesMessage)
    {
        // Arrange
        using var certificate = CreateCertificate();
        var credential = new CredentialDescription { SourceType = CredentialSource.Certificate, Certificate = certificate };
        var factory = CreateFactory(credential);
        using var services = (ServiceProvider)factory.Build();
        using var http = (MockHttpClientFactory)services.GetRequiredService<IMsalHttpClientFactory>();
        var expected = new ArgumentException(
            matchesMessage ? IDWebErrorMessage.ClientCertificatesHaveExpiredOrCannotBeLoaded : "Unrelated failure",
            matchesMessage ? "otherParameter" : "clientCredentials");
        http.AddMockHandler(CreateRejection(700027));
        http.AddMockHandler(new MockHttpMessageHandler { ExceptionToThrow = expected });

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            services.GetRequiredService<ITokenAcquisition>().GetAuthenticationResultForAppAsync(Scope));

        // Assert
        Assert.Same(expected, exception);
    }

    private static TokenAcquirerFactory CreateFactory(CredentialDescription credential)
    {
        TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
        var factory = TokenAcquirerFactory.GetDefaultInstance();
        factory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
            options.ClientId = Guid.NewGuid().ToString();
            options.ClientCredentials = [credential];
        });
        factory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();
        return factory;
    }

    private static Task AcquireAsync(TokenAcquisition acquisition, string flow)
    {
        return flow switch
        {
            "app" => acquisition.GetAuthenticationResultForAppAsync(Scope),
            "user" => acquisition.GetAuthenticationResultForUserAsync(
                [Scope],
                user: new ClaimsPrincipal(new Microsoft.IdentityModel.Tokens.CaseSensitiveClaimsIdentity(
                [
                    new Claim(ClaimConstants.Username, "user@example.com"),
                    new Claim(ClaimConstants.Password, "test-password"),
                ]))),
            "authCode" => acquisition.AddAccountToCacheFromAuthorizationCodeAndGetAccountAsync(
                new AuthCodeRedemptionParameters([Scope], "test-code", string.Empty, null, null, null)),
            _ => throw new ArgumentOutOfRangeException(nameof(flow)),
        };
    }

    private static MockHttpMessageHandler CreateRejection(int errorCode)
    {
        return new MockHttpMessageHandler
        {
            ExpectedMethod = HttpMethod.Post,
            ResponseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(
                    $"{{\"error\":\"invalid_client\",\"error_description\":\"AADSTS{errorCode}: Rejected assertion\",\"error_codes\":[{errorCode}]}}"),
            },
        };
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=CertificateRecoveryTest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
#pragma warning disable SYSLIB0057 // Keep the test compatible with net8.0.
        return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
#pragma warning restore SYSLIB0057
    }
}
