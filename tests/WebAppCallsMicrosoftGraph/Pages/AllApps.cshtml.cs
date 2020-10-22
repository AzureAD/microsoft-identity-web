// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph.Pages
{
    /// <summary>
    /// Tests the app-only permissions - need to have been granted by the tenant admin first.
    /// </summary>
    public class AllAppsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public AllAppsModel(ILogger<IndexModel> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }

        public int NumberOfApps { get; private set; }

        public async Task OnGet()
        {
            var messages = await _graphServiceClient.Applications
                .Request()
                .WithAppOnly()
                .GetAsync();
            NumberOfApps = messages.Count;
        }
    }
}
