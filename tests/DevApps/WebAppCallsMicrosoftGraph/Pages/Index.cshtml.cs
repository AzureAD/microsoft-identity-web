// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph.Pages
{
    [AuthorizeForScopes(Scopes = new[] { "user.read" })]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IDownstreamApi _downstreamApi;

        public IndexModel(ILogger<IndexModel> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
            _downstreamApi = downstreamApi;
        }

        public async Task OnGet()
        {
            var user = await _graphServiceClient.Me.GetAsync(r => 
            r.Options.WithScopes("user.read")
            //.WithUser(User)
            );
         
            try
            {
                using (var photoStream = await _graphServiceClient.Me.Photo.Content.GetAsync())
                {
                    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                    ViewData["photo"] = Convert.ToBase64String(photoByte);
                }
                ViewData["name"] = user.DisplayName;

                var graphData = await _downstreamApi.CallApiForUserAsync(
                    "GraphBeta"
                    );
                ViewData["json"] = await graphData.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                ViewData["photo"] = null;
            }

            // Or - Call a downstream directly with the IDownstreamApi helper (uses the authorization header provider, encapsulates MSAL.NET)
            // See https://aka.ms/ms-id-web/downstream-web-api
            IDownstreamApi downstreamApi = HttpContext.RequestServices.GetService(typeof(IDownstreamApi)) as IDownstreamApi;
            var result = await downstreamApi.CallApiForUserAsync(string.Empty,
                options =>
                {
                    options.BaseUrl = "https://graph.microsoft.com/v1.0/me";
                    options.Scopes = new[] { "user.read" };
                });

            // Or - Get an authorization header (uses the token acquirer)
            IAuthorizationHeaderProvider authorizationHeaderProvider = HttpContext.RequestServices.GetService(typeof(IAuthorizationHeaderProvider)) as IAuthorizationHeaderProvider;
            string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(
                new[] { "user.read" },
                new AuthorizationHeaderProviderOptions { BaseUrl = "https://graph.microsoft.com/v1.0/me"} );
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            HttpResponseMessage response = await client.GetAsync("https://graph.microsoft.com/v1.0/users");

            // Or - Get a token if an SDK needs it (uses MSAL.NET)
            ITokenAcquirerFactory tokenAcquirerFactory = HttpContext.RequestServices.GetService(typeof(ITokenAcquirerFactory)) as ITokenAcquirerFactory;
            ITokenAcquirer acquirer = tokenAcquirerFactory.GetTokenAcquirer();
            AcquireTokenResult tokenResult = await acquirer.GetTokenForUserAsync(new[] { "user.read" });
            string accessToken = tokenResult.AccessToken;

        }
    }
}
