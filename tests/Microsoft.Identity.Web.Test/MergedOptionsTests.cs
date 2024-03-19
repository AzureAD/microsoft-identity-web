// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MergedOptionsTests
    {
        // appliation options
        private readonly string _appOptionsAuthority = "microsoftIdentityApplicationOptionsAuthority";
        private readonly string _appOptionsAzureRegion = "microsoftIdentityApplicationOptionsAzureRegion";
        private readonly string[] _appOptionsClientCapabilities = new string[] { "microsoftIdentityApplicationOptionsClientCapabilities" };
        private readonly string _appOptionsClientId = "microsoftIdentityApplicationOptionsClientId";
        private readonly string _appOptionsDomain = "microsoftIdentityApplicationOptionsDomain";
        private readonly string _appOptionsEditProfile = "microsoftIdentityApplicationOptionsEditProfilePolicyId";
        private readonly string _appOptionsErrorPath = "/microsoftIdentityApplicationOptionsErrorPath";
        private readonly IEnumerable<CredentialDescription> _appOptionsClientCredentials = new CredentialDescription[] { new CredentialDescription() { SourceType = CredentialSource.ClientSecret, ClientSecret = "blah" } };
        private readonly string _appOptionsInstance = "microsoftIdentityApplicationOptionsInstance";
        private readonly string _appOptionsResetPath = "/microsoftIdentityApplicationOptionsResetPasswordPath";
        private readonly string _appOptionsPasswordRestId = "microsoftIdentityApplicationOptionsResetPasswordPolicyId";
        private readonly string _appOptionsSuSiPolicyId = "microsoftIdentityApplicationOptionsSignUpSignInPolicyId";
        private readonly string _appOptionsTenantId = "microsoftIdentityApplicationOptionsTenantId";
        private readonly IEnumerable<CredentialDescription> _appOptionsTokenDecyrptCreds = new CredentialDescription[] { new CredentialDescription() };

        // MS Identity options
        private readonly string _msIdentityOptionsAccessDeniedPath = "/microsoftIdentityOptionsAccessDeniedPath";
        private readonly string _msIdentityOptionsAuthority = "/microsoftIdentityOptionsAuthority";
        private readonly string _msIdentityCallbackPath = "/microsoftIdentityOptionsCallbackPath";
        private readonly string _msIdentityClaimsIssuer = "/microsoftIdentityOptionsClaimsIssuer";
        private readonly IEnumerable<CertificateDescription> _msIdentityClientCertificates = new CertificateDescription[] { new CertificateDescription() { } };
        private readonly IEnumerable<CredentialDescription> _msIdentityClientCredentials = new CredentialDescription[] { new CredentialDescription() { SourceType = CredentialSource.ClientSecret, ClientSecret = "blah" } };
        private readonly CertificatelessOptions _msIdentityClientCredsUsingMI = new CertificatelessOptions();
        private readonly string _msIdentityClientId = "/microsoftIdentityOptionsClientId";
        private readonly string _msIdentityClientSecret = "/microsoftIdentityOptionsClientSecret";
        private readonly string _msIdentityDomain = "/microsoftIdentityOptionsDomain";
        private readonly string _msIdentityEditProfile = "/microsoftIdentityOptionsEditProfilePolicyId";
        private readonly string _msIdentityForwardAuth = "/microsoftIdentityOptionsForwardAuthenticate";
        private readonly string _msIdentityForwardChallenge = "/microsoftIdentityOptionsForwardChallenge";
        private readonly string _msIdentityForwardDefault = "/microsoftIdentityOptionsForwardDefault";
        private readonly string _msIdentityForwardForbid = "/microsoftIdentityOptionsForwardForbid";
        private readonly string _msIdentityForwardSignIn = "/microsoftIdentityOptionsForwardSignIn";
        private readonly string _msIdentityForwardSignOut = "/microsoftIdentityOptionsForwardSignOut";
        private readonly string _msIdentityInstance = "/microsoftIdentityOptionsInstance";
        private readonly string _msIdentityMetadata = "/microsoftIdentityOptionsMetadataAddress";
        private readonly string _msIdentityPrompt = "/microsoftIdentityOptionsPrompt";
        private readonly string _msIdentityRemoteSignOutPath = "/microsoftIdentityOptionsRemoteSignOutPath";
        private readonly string _msIdentityResetPasswordId = "/microsoftIdentityOptionsResetPasswordPolicyId";
        private readonly string _msIdentityResource = "/microsoftIdentityOptionsResource";
        private readonly string _msIdentityResponseMode = "/microsoftIdentityOptionsResponseMode";
        private readonly string _msIdentityResponseType = "/microsoftIdentityOptionsResponseType";
        private readonly string _msIdentityReturnUrlParameter = "/microsoftIdentityOptionsReturnUrlParameter";
        private readonly string _msIdentitySignedOutCallbackPath = "/microsoftIdentityOptionsSignedOutCallbackPath";
        private readonly string _msIdentitySignedOutRedirectUri = "/microsoftIdentityOptionsSignedOutRedirectUri";
        private readonly string _msIdentitySignedInScheme = "/microsoftIdentityOptionsSignInScheme";
        private readonly string _msIdentitySignedOutScheme = "/microsoftIdentityOptionsSignOutScheme";
        private readonly string _msIdentitySuSiPolicyId = "/microsoftIdentityOptionsSignUpSignInPolicyId";
        private readonly string _msIdentityTenantId = "/microsoftIdentityOptionsTenantId";
        private readonly IEnumerable<CertificateDescription> _msIdentityTokenDecryptCertificates = new CertificateDescription[] { new CertificateDescription() { } };
        private readonly IEnumerable<CredentialDescription> _msIdentityTokenDecryptDescription = new CredentialDescription[] { new CredentialDescription() { SourceType = CredentialSource.ClientSecret, ClientSecret = "blah" } };

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions_Then_DefaultMicrosoftIdentityOptions_Test(bool microsoftIdentityApplicationOptionsFirst)
        {
            // Arrange
            MicrosoftIdentityApplicationOptions microsoftIdentityApplicationOptions = new MicrosoftIdentityApplicationOptions();
            microsoftIdentityApplicationOptions.AllowWebApiToBeAuthorizedByACL = true;
            microsoftIdentityApplicationOptions.Authority = _appOptionsAuthority;
            microsoftIdentityApplicationOptions.AzureRegion = _appOptionsAzureRegion;
            microsoftIdentityApplicationOptions.ClientCapabilities = _appOptionsClientCapabilities;
            microsoftIdentityApplicationOptions.ClientCredentials = _appOptionsClientCredentials;
            microsoftIdentityApplicationOptions.ClientId = _appOptionsClientId;
            microsoftIdentityApplicationOptions.Domain = _appOptionsDomain;
            microsoftIdentityApplicationOptions.EditProfilePolicyId = _appOptionsEditProfile;
            microsoftIdentityApplicationOptions.EnablePiiLogging = true;
            microsoftIdentityApplicationOptions.ErrorPath = _appOptionsErrorPath;
            microsoftIdentityApplicationOptions.Instance = _appOptionsInstance;
            microsoftIdentityApplicationOptions.ResetPasswordPath = _appOptionsResetPath;
            microsoftIdentityApplicationOptions.ResetPasswordPolicyId = _appOptionsPasswordRestId;
            microsoftIdentityApplicationOptions.SendX5C = true;
            microsoftIdentityApplicationOptions.SignUpSignInPolicyId = _appOptionsSuSiPolicyId;
            microsoftIdentityApplicationOptions.TenantId = _appOptionsTenantId;
            microsoftIdentityApplicationOptions.TokenDecryptionCredentials = _appOptionsTokenDecyrptCreds;
            microsoftIdentityApplicationOptions.WithSpaAuthCode = true;

            // Act
            MergedOptions mergedOptions = new();
            if (microsoftIdentityApplicationOptionsFirst)
            {
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(microsoftIdentityApplicationOptions, mergedOptions);
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(new MicrosoftIdentityOptions(), mergedOptions);
            }
            else
            {
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(new MicrosoftIdentityOptions(), mergedOptions);
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(microsoftIdentityApplicationOptions, mergedOptions);
            }

            // Assert
            Assert.True(mergedOptions.AllowWebApiToBeAuthorizedByACL);
            Assert.Equal(_appOptionsAuthority, mergedOptions.Authority);
            Assert.Equal(_appOptionsAzureRegion, mergedOptions.AzureRegion);
            Assert.Equal(_appOptionsClientCapabilities, mergedOptions.ClientCapabilities!);
            Assert.Equal(_appOptionsClientId, mergedOptions.ClientId);
            Assert.Equal(_appOptionsDomain, mergedOptions.Domain);
            Assert.Equal(_appOptionsEditProfile, mergedOptions.EditProfilePolicyId);
            Assert.True(mergedOptions.EnablePiiLogging);
            Assert.Equal(_appOptionsErrorPath, mergedOptions.ErrorPath);
            Assert.Equal(_appOptionsClientCredentials, mergedOptions.ClientCredentials!);
            Assert.Equal(_appOptionsInstance, mergedOptions.Instance);
            Assert.Equal(_appOptionsResetPath, mergedOptions.ResetPasswordPath);
            Assert.Equal(_appOptionsPasswordRestId, mergedOptions.ResetPasswordPolicyId);
            Assert.True(mergedOptions.SendX5C);
            Assert.Equal(_appOptionsSuSiPolicyId, mergedOptions.SignUpSignInPolicyId);
            Assert.Equal(_appOptionsTenantId, mergedOptions.TenantId);
            Assert.Equal(_appOptionsTokenDecyrptCreds, mergedOptions.TokenDecryptionCredentials!);
            Assert.True(mergedOptions.WithSpaAuthCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UpdateMergedOptionsFromMicrosoftIdentityOptions_ThenMicrosoftIdentityApplicationOptions_Test(bool microsoftIdentityOptionsFirst)
        {
            // Arrange
            MicrosoftIdentityOptions microsoftIdentityOptions = new MicrosoftIdentityOptions();
            microsoftIdentityOptions.AccessDeniedPath = _msIdentityOptionsAccessDeniedPath;
            microsoftIdentityOptions.AllowWebApiToBeAuthorizedByACL = true;
            microsoftIdentityOptions.Authority = _msIdentityOptionsAuthority;
            microsoftIdentityOptions.CallbackPath = _msIdentityCallbackPath;
            microsoftIdentityOptions.ClaimsIssuer = _msIdentityClaimsIssuer;
            microsoftIdentityOptions.ClientCertificates = _msIdentityClientCertificates;
            microsoftIdentityOptions.ClientCredentials = _msIdentityClientCredentials;
            microsoftIdentityOptions.ClientCredentialsUsingManagedIdentity = _msIdentityClientCredsUsingMI;
            microsoftIdentityOptions.ClientId = _msIdentityClientId;
            microsoftIdentityOptions.ClientSecret = _msIdentityClientSecret;
            microsoftIdentityOptions.DisableTelemetry = true;
            microsoftIdentityOptions.Domain = _msIdentityDomain;
            microsoftIdentityOptions.EditProfilePolicyId = _msIdentityEditProfile;
            microsoftIdentityOptions.ForwardAuthenticate = _msIdentityForwardAuth;
            microsoftIdentityOptions.ForwardChallenge = _msIdentityForwardChallenge;
            microsoftIdentityOptions.ForwardDefault = _msIdentityForwardDefault;
            microsoftIdentityOptions.ForwardForbid = _msIdentityForwardForbid;
            microsoftIdentityOptions.ForwardSignIn = _msIdentityForwardSignIn;
            microsoftIdentityOptions.ForwardSignOut = _msIdentityForwardSignOut;
            microsoftIdentityOptions.GetClaimsFromUserInfoEndpoint = true;
            microsoftIdentityOptions.Instance = _msIdentityInstance;
            microsoftIdentityOptions.LegacyCacheCompatibilityEnabled = true;
            microsoftIdentityOptions.MapInboundClaims = true;
            microsoftIdentityOptions.MetadataAddress = _msIdentityMetadata;
            microsoftIdentityOptions.Prompt = _msIdentityPrompt;
            microsoftIdentityOptions.RefreshOnIssuerKeyNotFound = true;
            microsoftIdentityOptions.RemoteSignOutPath = _msIdentityRemoteSignOutPath;
            microsoftIdentityOptions.RequireHttpsMetadata = true;
            microsoftIdentityOptions.ResetPasswordPolicyId = _msIdentityResetPasswordId;
            microsoftIdentityOptions.Resource = _msIdentityResource;
            microsoftIdentityOptions.ResponseMode = _msIdentityResponseMode;
            microsoftIdentityOptions.ResponseType = _msIdentityResponseType;
            microsoftIdentityOptions.ReturnUrlParameter = _msIdentityReturnUrlParameter;
            microsoftIdentityOptions.SaveTokens = true;
            microsoftIdentityOptions.SendX5C = true;
            microsoftIdentityOptions.SignedOutCallbackPath = _msIdentitySignedOutCallbackPath;
            microsoftIdentityOptions.SignedOutRedirectUri = _msIdentitySignedOutRedirectUri;
            microsoftIdentityOptions.SignInScheme = _msIdentitySignedInScheme;
            microsoftIdentityOptions.SignOutScheme = _msIdentitySignedOutScheme;
            microsoftIdentityOptions.SignUpSignInPolicyId = _msIdentitySuSiPolicyId;
            microsoftIdentityOptions.SkipUnrecognizedRequests = true;
            microsoftIdentityOptions.TenantId = _msIdentityTenantId;
            microsoftIdentityOptions.TokenDecryptionCertificates = _msIdentityTokenDecryptCertificates;
            //TODO: add another test for this case microsoftIdentityOptions.TokenDecryptionCredentials = _msIdentityTokenDecryptDescription;
            microsoftIdentityOptions.UsePkce = true;
            microsoftIdentityOptions.UseTokenLifetime = true;
            microsoftIdentityOptions.WithSpaAuthCode = true;

            // Act
            MergedOptions mergedOptions = new MergedOptions();
            if (microsoftIdentityOptionsFirst)
            {
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(microsoftIdentityOptions, mergedOptions);
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(new MicrosoftIdentityApplicationOptions(), mergedOptions);
            }
            else
            {
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(new MicrosoftIdentityApplicationOptions(), mergedOptions);
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(microsoftIdentityOptions, mergedOptions);
            }

            // Assert
            Assert.Equal(_msIdentityOptionsAccessDeniedPath, microsoftIdentityOptions.AccessDeniedPath);
            Assert.Equal(_msIdentityOptionsAuthority, mergedOptions.Authority);
            Assert.Equal(_msIdentityCallbackPath, mergedOptions.CallbackPath);
            Assert.Equal(_msIdentityClaimsIssuer, microsoftIdentityOptions.ClaimsIssuer);
            Assert.Null(microsoftIdentityOptions.ClientCertificates);
            Assert.Equal(_msIdentityClientCredentials, mergedOptions.ClientCredentials!);
            Assert.Equal(_msIdentityClientCredsUsingMI, microsoftIdentityOptions.ClientCredentialsUsingManagedIdentity);
            Assert.Equal(_msIdentityClientId, mergedOptions.ClientId);
            Assert.Null(microsoftIdentityOptions.ClientSecret); // client secret no longer used directly, but through clientCredentials
            Assert.Equal(_msIdentityDomain, mergedOptions.Domain);
            Assert.Equal(_msIdentityEditProfile, mergedOptions.EditProfilePolicyId);
            Assert.Equal(_msIdentityForwardAuth, microsoftIdentityOptions.ForwardAuthenticate);
            Assert.Equal(_msIdentityForwardChallenge, microsoftIdentityOptions.ForwardChallenge);
            Assert.Equal(_msIdentityForwardDefault, microsoftIdentityOptions.ForwardDefault);
            Assert.Equal(_msIdentityForwardForbid, microsoftIdentityOptions.ForwardForbid);
            Assert.Equal(_msIdentityForwardSignIn, microsoftIdentityOptions.ForwardSignIn);
            Assert.Equal(_msIdentityForwardSignOut, microsoftIdentityOptions.ForwardSignOut);
            Assert.Equal(_msIdentityInstance, mergedOptions.Instance);
            Assert.Equal(_msIdentityMetadata, microsoftIdentityOptions.MetadataAddress);
            Assert.Equal(_msIdentityPrompt, microsoftIdentityOptions.Prompt);
            Assert.Equal(_msIdentityRemoteSignOutPath, microsoftIdentityOptions.RemoteSignOutPath);
            Assert.Equal(_msIdentityResetPasswordId, mergedOptions.ResetPasswordPolicyId);
            Assert.Equal(_msIdentityResource, microsoftIdentityOptions.Resource);
            Assert.Equal(_msIdentityResponseMode, microsoftIdentityOptions.ResponseMode);
            Assert.Equal(_msIdentityResponseType, microsoftIdentityOptions.ResponseType);
            Assert.Equal(_msIdentityReturnUrlParameter, microsoftIdentityOptions.ReturnUrlParameter);
            Assert.Equal(_msIdentitySignedOutCallbackPath, microsoftIdentityOptions.SignedOutCallbackPath);
            Assert.Equal(_msIdentitySignedOutRedirectUri, microsoftIdentityOptions.SignedOutRedirectUri);
            Assert.Equal(_msIdentitySignedInScheme, microsoftIdentityOptions.SignInScheme);
            Assert.Equal(_msIdentitySignedOutScheme, microsoftIdentityOptions.SignOutScheme);
            Assert.Equal(_msIdentitySuSiPolicyId, mergedOptions.SignUpSignInPolicyId);
            Assert.Equal(_msIdentityTenantId, mergedOptions.TenantId);
            Assert.Equal(_msIdentityTokenDecryptCertificates, microsoftIdentityOptions.TokenDecryptionCertificates!);
            Assert.Equal(_msIdentityTokenDecryptCertificates!.First(), mergedOptions.TokenDecryptionCredentials!.First());
            Assert.True(mergedOptions.AllowWebApiToBeAuthorizedByACL);
            Assert.True(microsoftIdentityOptions.DisableTelemetry);
            Assert.True(microsoftIdentityOptions.GetClaimsFromUserInfoEndpoint);
            Assert.True(microsoftIdentityOptions.LegacyCacheCompatibilityEnabled);
            Assert.True(microsoftIdentityOptions.MapInboundClaims);
            Assert.True(microsoftIdentityOptions.RefreshOnIssuerKeyNotFound);
            Assert.True(microsoftIdentityOptions.RequireHttpsMetadata);
            Assert.True(microsoftIdentityOptions.SaveTokens);
            Assert.True(microsoftIdentityOptions.SendX5C);
            Assert.True(microsoftIdentityOptions.SkipUnrecognizedRequests);
            Assert.True(microsoftIdentityOptions.UsePkce);
            Assert.True(microsoftIdentityOptions.UseTokenLifetime);
            Assert.True(microsoftIdentityOptions.WithSpaAuthCode);
        }
    }
}
