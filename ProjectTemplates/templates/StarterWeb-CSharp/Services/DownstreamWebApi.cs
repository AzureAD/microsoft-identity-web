using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace test.Services
{
    public interface IDownstreamWebApi
    {
        Task<string> CallWebApi();
    }

    public static class DownstreamWebApiExtensions
    {
        public static void AddDownstreamWebApiService(this IServiceCollection services, IConfiguration configuration)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddHttpClient<IDownstreamWebApi, DownstreamWebApi>();
        }
    }

    public class DownstreamWebApi : IDownstreamWebApi
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        private readonly IConfiguration _configuration;

        private readonly HttpClient _httpClient;

        public DownstreamWebApi(
            ITokenAcquisition tokenAcquisition,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _tokenAcquisition = tokenAcquisition;
            _configuration = configuration;
            _httpClient = httpClient;
        }


        public async Task<string> CallWebApi()
        {
            string[] scopes = _configuration["CalledApi:CalledApiScopes"]?.Split(' ');
            string apiUrl = _configuration["CalledApi:CalledApiUrl"];

            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {accessToken}");

            string apiResult;
            var response = await _httpClient.GetAsync($"{apiUrl}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                apiResult = await response.Content.ReadAsStringAsync();
            }
            else
            {
                apiResult = $"Error calling the API '{apiUrl}'";
            }

            return apiResult;
        }
    }
}
