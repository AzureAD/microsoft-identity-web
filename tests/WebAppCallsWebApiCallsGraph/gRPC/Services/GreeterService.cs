// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;

namespace grpc
{
    public class GreeterService : Greeter.GreeterBase
    {
        public GreeterService()
        {
        }

        [Authorize]
        [RequiredScope("access_as_user")]
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            context.GetHttpContext();

            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
