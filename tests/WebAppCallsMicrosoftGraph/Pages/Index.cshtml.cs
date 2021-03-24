// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph.Pages
{
    [AuthorizeForScopes(Scopes = new[] { "user.read" })]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IDownstreamWebApi _downstreamWebApi;

        public IndexModel(ILogger<IndexModel> logger, GraphServiceClient graphServiceClient, IDownstreamWebApi downstreamWebApi)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
            _downstreamWebApi = downstreamWebApi;
        }

        public async Task OnGet()
        {
            var user = await _graphServiceClient.Me.Request().GetAsync();
         
            try
            {
                using (var photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync())
                {
                    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                    ViewData["photo"] = Convert.ToBase64String(photoByte);
                }
                ViewData["name"] = user.DisplayName;

                var graphData = await _downstreamWebApi.CallWebApiForUserAsync("GraphBeta");
            }
            catch (Exception)
            {
                ViewData["photo"] = null;
            }
        }
    }
}
