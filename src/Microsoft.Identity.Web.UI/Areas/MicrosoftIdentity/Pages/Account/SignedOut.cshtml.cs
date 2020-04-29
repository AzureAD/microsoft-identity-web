using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.Identity.Web.UI.Areas.MicrosoftIdentity.Pages.Account
{
    /// <summary>
    /// Model for the SignOut page
    /// </summary>
    [AllowAnonymous]
    public class SignedOutModel : PageModel
    {
#pragma warning disable CS1591 // Imposed by the Blazor framework
        public IActionResult OnGet()
#pragma warning restore CS1591 // // Imposed by the Blazor framework
        {
            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect("~/");
            }

            return Page();
        }
    }
}
