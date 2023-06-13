// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph.Pages
{
    [AuthorizeForScopes(Scopes = new[] { "User.Read.All" })]
    public class AllUsersModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public AllUsersModel(ILogger<IndexModel> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }

        public int NumberOfUsers { get; private set; }

        public async Task OnGet()
        {
            var messages = await _graphServiceClient.Users
                .GetAsync(b => b.Options.WithAppOnly());
            NumberOfUsers = messages.Value.Count;
        }
    }
}
