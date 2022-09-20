using GlueIt;

namespace GlueItTests
{
    public class TokenValidatorTests
    {
        [Fact]
        public void ValidateTokenTest()
        {
            string instance = "https://login.microsoftonline.com";
            string tenant = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
            string audience = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc";
            string token = "";
            var result = TokenValidator.ValidateToken(
                instance,
                tenant,
                audience,
                token);
            Assert.NotNull(result);
        }
    }
}
