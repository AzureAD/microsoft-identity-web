using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web.OWIN
{
    internal class OwinTokenAcquirerFactory : TokenAcquirerFactory 
    {
        protected override string DefineConfiguration(IConfigurationBuilder builder)
        {
            IConfigurationBuilder configurationBuilder = builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["AzureAd:Instance"] = ConfigurationManager.AppSettings["ida:Instance"] ?? "https://login.microsoftonline.com/",
                ["AzureAd:ClientId"] = ConfigurationManager.AppSettings["ida:ClientId"],
                ["AzureAd:TenantId"] = ConfigurationManager.AppSettings["ida:Tenant"],
                ["AzureAd:Audience"] = ConfigurationManager.AppSettings["ida:Audience"],
            });
            return HttpContext.Current.Request.PhysicalApplicationPath;
        }
    }
}
