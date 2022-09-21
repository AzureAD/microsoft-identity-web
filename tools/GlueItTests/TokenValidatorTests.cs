using GlueIt;
using GlueItConsoleApp;

namespace GlueItTests
{
    public class TokenValidatorTests
    {
        [Fact]
        public async void ValidateTokenTestAsync()
        {
            string clientId = "64afbdd4-bed7-4fa9-b7ff-678a52a0f112";
            string tenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
            string[] webApiScopes = new[]
            {
                "api://a4c2469b-cf84-4145-8f5f-cb7bacf814bc/access_as_user"
            };
            string instance = "https://login.microsoftonline.com";
            string audience = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc";

            GetAuthResult authResult = new();
            var result = await authResult.GetAuthenticationResultAsync(
                clientId,
                tenantId,
                webApiScopes
                ).ConfigureAwait(false);
 
            var testResult = TokenValidator.ValidateToken(
                instance,
                tenantId,
                audience,
                result.AccessToken);
            Assert.NotNull(testResult);
        }
    }
}
