// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimulateOidc.Controllers
{
    // We are using a controller rather than static pages here to serve the OIDC metadata
    // so that we can dynamically replace the jwks_uri with the correct URL for this service.
    [Route("v2.0/.well-known")]
    [ApiController]
    [AllowAnonymous]
    public class MetadataController : ControllerBase
    {
        [HttpGet("/v2.0/.well-known/openid-configuration")]
        public IActionResult  OpenIdConnectConfiguration()
        {
            // Get the openIdConnectConfiguration from the embedded resource
            string openIdConnectDocumentString = System.Text.Encoding.UTF8.GetString(Properties.Resource.openid_configuration);

            // Replace the jwks URI based on the base URL of this service.
            string hostUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/";
            string openIdConnectDocumentJsonString = openIdConnectDocumentString.Replace("https://localhost/", hostUrl, System.StringComparison.InvariantCulture);
         
            // The openIdConnectConfiguration is served as a JSON string
            return new ContentResult
            {
                ContentType = "application/json",
                Content = openIdConnectDocumentJsonString,
                StatusCode = 200
            };
        }

        [HttpGet("/v2.0/.well-known/keys.json")]
        public async Task<IActionResult> KeysAsync()
        {
            HttpClient client = new HttpClient();
            string keysDocument = await client.GetStringAsync("https://login.microsoftonline.com/common/discovery/v2.0/keys").ConfigureAwait(false);
            keysDocument = keysDocument.Replace("https://login.microsoftonline.com/{tenantid}/v2.0", "invalidIssuer", StringComparison.OrdinalIgnoreCase);
            // byte[] keysDocument = Properties.Resource.keys;
            return new FileContentResult(Encoding.ASCII.GetBytes(keysDocument), "application/json");
        }
    }
}
