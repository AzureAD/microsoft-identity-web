using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.Identity.Web.MicrosoftGraph.Extensions.Authorization
{
    /// <summary>
    /// Microsoft Graph Authorization Attribute for use with ASP.NET Core Controllers and Pages.
    /// </summary>
    public class MicrosoftGraphAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private string[]? _groups { get; set; }
        private string[]? _roles { get; set; }
        private bool _useAppOnly { get; set; }
        private string? _tenant { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="groups">List of group IDs to check the membership against. Currently accepts maximum of 20 groups at once.</param>
        /// <param name="roles">List of role template IDs from Azure AD to check against.</param>
        /// <param name="useAppOnly">Should the permissions be app only or not.</param>
        /// <param name="tenant">Tenant ID or domain for which we want to make the call.</param>
        public MicrosoftGraphAuthorizationAttribute(string[]? groups = null, string[]? roles = null, bool useAppOnly = false, string? tenant = null)
        {
            _groups = groups;
            _roles = roles;
            _useAppOnly = useAppOnly;
            _tenant = tenant;
        }
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var result = await context.HttpContext.AuthorizeUserAsync(_groups, _roles, _useAppOnly, _tenant);

            if(!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new ChallengeResult();
            }
            
            if (!result)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
