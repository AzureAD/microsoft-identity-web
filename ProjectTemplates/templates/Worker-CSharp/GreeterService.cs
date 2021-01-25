using System.Threading.Tasks;
using Grpc.Core;
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
#endif
#if (GenerateApi)
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Net;
using System.Net.Http;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif
using Microsoft.Extensions.Logging;
#if (!NoAuth)
using Microsoft.Identity.Web.Resource;
#endif

namespace Company.Application1
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;

#if (!NoAuth)
        // The web API will only accept tokens 1) for users, and 2) having the "api-scope" scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "api-scope" };
#endif

#if (GenerateApi)
        private readonly IDownstreamWebApi _downstreamWebApi;

        public GreeterService(ILogger<GreeterService> logger,
                        IDownstreamWebApi downstreamWebApi)
        {
            _logger = logger;
            _downstreamWebApi = downstreamWebApi;
        }

        [Authorize]
        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            using var response = await _downstreamWebApi.CallWebApiForUserAsync("DownstreamApi").ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                // Do something with apiResult
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
            }

            return new HelloReply()
            {
                Message = "Hello " + request.Name
            };
        }

#elseif (GenerateGraph)
        private readonly GraphServiceClient _graphServiceClient;

        public GreeterService(ILogger<GreeterService> logger,
                   GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }
        
        [Authorize]
        public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            var user = await _graphServiceClient.Me.Request().GetAsync();

            return new HelloReply()
            {
                Message = "Hello " + user.DisplayName
            };
        }
#else
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

#if (!NoAuth)
        [Authorize]
#endif
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
#if (!NoAuth)
            var httpContext = context.GetHttpContext();
            httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
#endif
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
#endif
    }
}
