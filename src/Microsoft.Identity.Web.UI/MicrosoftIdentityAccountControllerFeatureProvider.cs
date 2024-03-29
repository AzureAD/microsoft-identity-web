﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Identity.Web.UI.Areas.MicrosoftIdentity.Controllers;

namespace Microsoft.Identity.Web.UI
{
    internal class MicrosoftIdentityAccountControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            if (!feature.Controllers.Contains(typeof(AccountController).GetTypeInfo()))
            {
                feature.Controllers.Add(typeof(AccountController).GetTypeInfo());
            }
        }
    }
}
