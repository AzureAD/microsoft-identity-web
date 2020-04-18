// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.Identity.Web.UI.Areas.MicrosoftIdentity.Pages.Account
{
    /// <summary>
    /// Page presenting the Access denied error.
    /// </summary>
    [AllowAnonymous]
    public class AccessDeniedModel : PageModel
    {
        /// <summary>
        /// Method handling the HTTP GET method.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
