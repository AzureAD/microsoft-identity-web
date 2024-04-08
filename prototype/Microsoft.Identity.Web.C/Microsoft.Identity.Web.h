// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#ifndef MICROSOFT_IDENTITY_WEB_DLL_H
#define MICROSOFT_IDENTITY_WEB_DLL_H

#ifdef _cplusplus
#define MICROSOFT_IDENTITY_WEB_EXTERN_C extern "C"
#else
#define MICROSOFT_IDENTITY_WEB_EXTERN_C
#endif // _cplusplus

#ifdef _WIN32
#define MICROSOFT_IDENTITY_WEB_API __declspec(dllimport)
#define MICROSOFT_IDENTITY_WEB_CALLTYPE __stdcall
#else
#define MICROSOFT_IDENTITY_WEB_API
#define MICROSOFT_IDENTITY_WEB_CALLTYPE
#endif // _WIN32

MICROSOFT_IDENTITY_WEB_EXTERN_C typedef struct _MicrosoftIdentityApplicationOptions
{
    /**
     * Gets or sets the Azure Active Directory instance, e.g. <c>"https://login.microsoftonline.com/"</c>.
    */
    const char* instance;

    /**
     * Gets or sets the tenant ID. If your application is multi-tenant, you can also use "common" if it supports
     * both work and school, or personal accounts accounts, or "organizations" if your application supports only work
     * and school accounts. If your application is single tenant, set this property to the tenant ID or domain name.
     * If your application works only for Microsoft personal accounts, use "consumers".
    */
    const char* tenantId;

    /**
     * Gets or sets the Authority to use when making OpenIdConnect calls. By default the authority is computed
     * from the <see cref="Instance"/> and <see cref="TenantId"/> properties, by concatenating them, and appending "v2.0".
     * If your authority is not an Azure AD authority, you can set it directly here.
    */
    const char* authority;

    /**
     * In a web API, audience of the tokens that will be accepted by the web API.
     * If your web API accepts several audiences, see <see cref="Audiences"
     * If both Audience and <see cref="Audiences"/>, are expressed, the effective audiences is the
     * union of these properties.
     * */
    const char* audience;

    /**
     * In a web API, accepted audiences for the tokens received by the web API.
    */
    const char** audiences;
    unsigned int audiencesCount;

} MicrosoftIdentityApplicationOptions;

#endif // !MICROSOFT_IDENTITY_WEB_DLL_H
