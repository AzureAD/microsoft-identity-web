using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
#if (GenerateApi)
using System.Net.Http;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif
using Microsoft.Extensions.Logging;
#if (!NoAuth)
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Threading.Tasks;
#endif

namespace Company.FunctionApp1
{
    public class SampleFunc
    {
        private readonly ILogger<SampleFunc> _logger;
#if (GenerateApi)
        private readonly IDownstreamWebApi _downstreamWebApi;

        public SampleFunc(ILogger<SampleFunc> logger,
            IDownstreamWebApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
            _logger = logger;
        }

        [FunctionName("SampleFunc")]
        [RequiredScope("access_as_user")] // The Azure Function will only accept tokens 1) for users, and 2) having the "access_as_user" scope for this API
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var (authenticationStatus, authenticationResponse) =
                await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;

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

            string name = req.HttpContext.User.Identity.IsAuthenticated ? req.HttpContext.User.GetDisplayName() : null;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new JsonResult(responseMessage);
        }

#elseif (GenerateGraph)
        private readonly GraphServiceClient _graphServiceClient;

        public SampleFunc(ILogger<SampleFunc> logger,
            GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
            _logger = logger;
        }

        [FunctionName("SampleFunc")]
        [RequiredScope("access_as_user")] // The Azure Function will only accept tokens 1) for users, and 2) having the "access_as_user" scope for this API
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var (authenticationStatus, authenticationResponse) =
                await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;

            var user = await _graphServiceClient.Me.Request().GetAsync();

            string responseMessage = string.IsNullOrEmpty(user.DisplayName)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {user.DisplayName}. This HTTP triggered function executed successfully.";

            return new JsonResult(responseMessage);
        }

#elseif (!NoAuth)
        public SampleFunc(ILogger<SampleFunc> logger)
        {
            _logger = logger;
        }

        [FunctionName("SampleFunc")]
        [RequiredScope("access_as_user")] // The Azure Function will only accept tokens 1) for users, and 2) having the "access_as_user" scope for this API
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var (authenticationStatus, authenticationResponse) =
                await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus) return authenticationResponse;

            string name = req.HttpContext.User.Identity.IsAuthenticated ? req.HttpContext.User.GetDisplayName() : null;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new JsonResult(responseMessage);
        }

#else
        public SampleFunc(ILogger<SampleFunc> logger)
        {
            _logger = logger;
        }

        [FunctionName("SampleFunc")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string responseMessage = "This HTTP triggered function executed successfully.";

            return new JsonResult(responseMessage);
        }
#endif
    }
}
