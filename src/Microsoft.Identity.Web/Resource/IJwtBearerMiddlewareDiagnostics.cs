// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.Identity.Web.Resource
{
    public interface IJwtBearerMiddlewareDiagnostics
    {
        JwtBearerEvents Subscribe(JwtBearerEvents events);
    }
}
