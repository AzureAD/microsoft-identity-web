# Azure AD B2C limitations

Microsoft.Identity.Web makes the experience of developing web apps and web APIs for Azure AD and Azure AD B2C very similar. There are, however, limitations of the Azure AD B2C service which Microsoft.Identity.Web cannot work around:

## Azure AD B2C protected web APIs cannot call downstream APIs

As explained in [Request an access token in Azure Active Directory B2C](https://docs.microsoft.com/azure/active-directory-b2c/access-tokens),
Azure AD B2C does not support the [On behalf of flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow) used in [web APIs calling downwstream APIs](https://docs.microsoft.com/azure/active-directory/develop/scenario-web-api-call-api-overview). This means that Azure AD B2C Web Apis won't be able to call downstream web apis. Therefore, if you try to use `ITokenAcquisition.GetTokenForUserAsync` or `IDownstreamApi.CallWebApiForUserAsync`, you'll get the following exception

```Text
MSAL.NetCore.4.25.0.0.MsalServiceException: 
	ErrorCode: unsupported_grant_type
Microsoft.Identity.Client.MsalServiceException: AADB2C90086: The supplied grant_type [urn:ietf:params:oauth:grant-type:jwt-bearer] is not supported
```

The project **webapi** project template in .NET Core 5 (or **webapi2** in Microsoft.Identity.Web.ProjectTemplates NuGet package) is aware of this limitation and does not propose code that would call a downstream API.

![image](https://user-images.githubusercontent.com/13203188/95241423-03212400-080e-11eb-99a3-6fbb7a38cd0c.png)

## All the scopes need to be requested upfront

With Azure AD B2C, there is no incremental consent. The scopes need to all be requested when the user signs-in.

## Old limitations now fixed

### Azure AD B2C web apps can now call several web APIS

Azure AD B2C web apps could not call several web APIs without the users re-signing-in. This means that you'll need to handle the user challenge. See [Managing incremental consent and conditional access](https://github.com/AzureAD/microsoft-identity-web/wiki/Managing-incremental-consent-and-conditional-access)~~

A recent change in B2C makes it possible to acquire tokens successively for different web APIs

In other words, with B2C, it's **now** possible to trade a refresh token for a new access token for a different resource as it is in AAD.

## You can now use ITokenAcquisition.GetTokenForAppAsync or IDownstreamApi.CallWebApiForAppAsync in Azure AD B2C web apps

Azure AD B2C now supports the [Client credentials flow](https://learn.microsoft.com/en-us/azure/active-directory-b2c/client-credentials-grant-flow?pivots=b2c-user-flow#step-3-obtain-an-access-token) used in [daemon scenarios](https://docs.microsoft.com/azure/active-directory/develop/scenario-daemon-overview)
