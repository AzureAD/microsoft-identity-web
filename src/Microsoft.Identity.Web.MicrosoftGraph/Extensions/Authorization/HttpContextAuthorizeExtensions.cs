using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Microsoft.Identity.Web.MicrosoftGraph.Extensions.Authorization
{
    /// <summary>
    /// Extensions of HttpContext for Graph-based authorization
    /// </summary>
    public static class HttpContextAuthorizeExtensions
    {
        private const int _checkMemberGroupMaximum = 20;
        private const string _graphDefaultScope = "https://graph.microsoft.com/.default";
        /// <summary>
        /// Checks if user is authorized based on provided Azure AD Group IDs or tenant roles based on their template IDs
        /// </summary>
        /// <param name="httpContext">HttpContext</param>
        /// <param name="groupIds">List of group IDs to check the membership against. Currently accepts maximum of 20 groups at once.</param>
        /// <param name="tenantRoleTemplateIds">List of role template IDs from Azure AD to check against.</param>
        /// <param name="useAppOnly">Should the permissions be app only or not.</param>
        /// <param name="tenant">Tenant ID or domain for which we want to make the call.</param>
        /// <returns>Whether user is member of provided list of groups or role templates.</returns>
        public static async Task<bool> AuthorizeUserAsync(this HttpContext httpContext, string[]? groupIds = null, string[]? tenantRoleTemplateIds = null, bool useAppOnly = false, string? tenant = null)
        {
            if (groupIds == null && tenantRoleTemplateIds == null)
            {
                throw new ArgumentNullException();
            }
            if(groupIds?.Length > _checkMemberGroupMaximum)
            {
                throw new ArgumentException($"You can currently check access for maximum of {_checkMemberGroupMaximum} groups at once.");
            }

            var microsoftGraph = httpContext.RequestServices.GetService<GraphServiceClient>();

            if (tenantRoleTemplateIds != null)
            {
                var filterQuery = string.Join(" or ", tenantRoleTemplateIds.Select(x => $"roleTemplateId eq '{x}'"));
                var roles = await microsoftGraph.DirectoryRoles.Request()
                    .Filter(filterQuery)
                    .WithScopes(_graphDefaultScope)
                    .WithAppOnly(useAppOnly, tenant)
                    .GetAsync();
                foreach (var role in roles)
                {
                    var roleMembers = await microsoftGraph.DirectoryRoles[role.Id].Members.Request()
                        .WithScopes(_graphDefaultScope)
                        .WithAppOnly(useAppOnly, tenant)
                        .GetAsync();
                    if (roleMembers.Where(x => x.Id == httpContext.User.GetObjectId()).Any())
                    {
                        return true;
                    }
                }
            }
            else if (groupIds != null)
            {
                var memberGroups = await microsoftGraph.Users[httpContext.User.GetObjectId()].CheckMemberGroups(groupIds).Request()
                    .WithScopes(_graphDefaultScope)
                    .WithAppOnly(useAppOnly, tenant)
                    .PostAsync();
                if (memberGroups.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
