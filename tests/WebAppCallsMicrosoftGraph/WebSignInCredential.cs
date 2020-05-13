using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebAppCallsMicrosoftGraph
{
    public class WebSignInCredential : IAuthenticationProvider
    {
        public WebSignInCredential(ITokenAcquisition tokenAcquisition)
        {
            this.tokenAcquisition = tokenAcquisition;
        }

        ITokenAcquisition tokenAcquisition;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization",
                $"Bearer {await tokenAcquisition.GetAccessTokenForUserAsync(new string[0])}");
        }
    }
}