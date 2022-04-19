using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace OwinWebApi.Controllers
{
    [Authorize]
    public class TodoListController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            GraphServiceClient graphServiceClient = HttpContext.Current.GetGraphServiceClient();
            var me = graphServiceClient.Me.Request().GetAsync().GetAwaiter().GetResult();

            return new string[] { "value1", "value2", me.DisplayName };

            //ITokenAcquisition tokenAcquisition = HttpContext.Current.GetTokenAcquisition();

        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
