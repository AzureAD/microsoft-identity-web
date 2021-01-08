using System.Threading.Tasks;
using Grpc.Core;
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web.Resource;
#endif

namespace grpc
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        static string[] scopeRequiredByAPI = new string[] { "access_as_user" };

#if (!NoAuth)
        [Authorize]
#endif
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
#if (OrganizationalAuth || IndividualB2CAuth)
            var httpContext = context.GetHttpContext();
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByAPI);
#endif
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
