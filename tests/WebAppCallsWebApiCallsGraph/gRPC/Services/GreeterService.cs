// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Resource;

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

        [Authorize]
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByAPI);

            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
