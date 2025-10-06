# Samples using Microsoft.Identity.Web

## Web applications

The following samples illustrate web applications that sign in users. Some samples also demonstrate the application calling Microsoft Graph, or your own web API with the user's identity.

> | Language/<br/>Platform | Code sample(s)<br/> on GitHub | Auth<br/> libraries | Auth flow |
> | ------- | --------------------------- | ------------- | -------------- |
> | ASP.NET Core <br/> [![Build](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/actions/workflows/dotnet.yml) | ASP.NET Core Series <br/> &#8226; [Sign in users](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/1-WebApp-OIDC/README.md) <br/> &#8226; [Sign in users (B2C)](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/1-WebApp-OIDC/1-5-B2C/README.md) <br/> &#8226; [Call Microsoft Graph](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/2-WebApp-graph-user/2-1-Call-MSGraph/README.md) <br/> &#8226; [Customize token cache](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/2-WebApp-graph-user/2-2-TokenCache/README.md) <br/> &#8226; [Call Graph (multi-tenant)](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/2-WebApp-graph-user/2-3-Multi-Tenant/README.md) <br/> &#8226; [Call Azure REST APIs](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/3-WebApp-multi-APIs/README.md) <br/> &#8226; [Protect web API](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/4-WebApp-your-API/4-1-MyOrg/README.md) <br/> &#8226; [Protect web API (B2C)](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/4-WebApp-your-API/4-2-B2C/README.md) <br/> &#8226; [Protect multi-tenant web API](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/4-WebApp-your-API/4-3-AnyOrg/Readme.md) <br/> &#8226; [Use App Roles for access control](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/5-WebApp-AuthZ/5-1-Roles/README.md) <br/> &#8226; [Use Security Groups for access control](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/5-WebApp-AuthZ/5-2-Groups/README.md) <br/> &#8226; [Deploy to Azure Storage and App Service](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/blob/master/6-Deploy-to-Azure/README.md) | &#8226; MSAL.NET<br/> &#8226; Microsoft.Identity.Web | &#8226; OpenID connect <br/> &#8226; Authorization code <br/> &#8226; On-Behalf-Of|
> | Blazor Server <br/> [![Build](https://github.com/Azure-Samples/ms-identity-blazor-server/actions/workflows/dotnet-core.yml/badge.svg)](https://github.com/Azure-Samples/ms-identity-blazor-server/actions/workflows/dotnet-core.yml) | Blazor Server series<br/> &#8226; [Sign in users](https://github.com/Azure-Samples/ms-identity-blazor-server/tree/main/WebApp-OIDC/MyOrg) <br/> &#8226; [Sign in users (B2C)](https://github.com/Azure-Samples/ms-identity-blazor-server/tree/main/WebApp-OIDC/B2C) <br/> &#8226; [Call Microsoft Graph](https://github.com/Azure-Samples/ms-identity-blazor-server/tree/main/WebApp-graph-user/Call-MSGraph) <br/> &#8226; [Call web API](https://github.com/Azure-Samples/ms-identity-blazor-server/tree/main/WebApp-your-API/MyOrg) <br/> &#8226; [Call web API (B2C)](https://github.com/Azure-Samples/ms-identity-blazor-server/tree/main/WebApp-your-API/B2C) | MSAL.NET | Authorization code|

## Web API

The following samples show how to protect a web API with the Microsoft identity platform, and how to call a downstream API from the web API.

> | Language/<br/>Platform | Code sample(s) <br/> on GitHub |Auth<br/> libraries |Auth flow |
> | ----------- | ----------- |----------- |----------- |
> | ASP.NET | [Call Microsoft Graph](https://github.com/Azure-Samples/ms-identity-aspnet-webapi-onbehalfof) | MSAL.NET | On-Behalf-Of (OBO) |
> | ASP.NET Core | [Sign in users and call Microsoft Graph](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/2.%20Web%20API%20now%20calls%20Microsoft%20Graph) | MSAL.NET | On-Behalf-Of (OBO) |

## Service / daemon

The following samples show an application that accesses the Microsoft Graph API with its own identity (with no user).

> | Language/<br/>Platform | Code sample(s) <br/> on GitHub |Auth<br/> libraries |Auth flow |
> | ----------- | ----------- |----------- |----------- |
> |.NET Core| &#8226; [Call Microsoft Graph](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/1-Call-MSGraph) <br/> &#8226; [Call web API](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/2-Call-OwnApi) <br/> &#8226; [Using managed identity and Azure key vault](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/3-Using-KeyVault)| MSAL.NET | Client credentials grant|
> | ASP.NET|[Multi-tenant with Microsoft identity platform endpoint](https://github.com/Azure-Samples/ms-identity-aspnet-daemon-webapp) | MSAL.NET | Client credentials grant|
Client credentials grant|

## Multi-tenant SaaS

The following samples show how to configure your application to accept sign-ins from any Azure Active Directory (Azure AD) tenant. Configuring your application to be _multi-tenant_ means that you can offer a **Software as a Service** (SaaS) application to many organizations, allowing their users to be able to sign-in to your application after providing consent.

> | Language/<br/>Platform | Code sample(s) <br/> on GitHub |Auth<br/> libraries |Auth flow |
> | ----------- | ----------- |----------- |----------- |
> | ASP.NET Core | [ASP.NET Core MVC web application calls Microsoft Graph API](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/2-WebApp-graph-user/2-3-Multi-Tenant) | MSAL.NET | OpenID connect |
> | ASP.NET Core | [ASP.NET Core MVC web application calls ASP.NET Core web API](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/4-WebApp-your-API/4-3-AnyOrg) | MSAL.NET | Authorization code |
