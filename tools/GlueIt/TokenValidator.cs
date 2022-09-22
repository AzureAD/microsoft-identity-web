using System.Runtime.InteropServices;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace GlueIt;
public static class TokenValidator
{
    /// <summary>
    /// Validate token method.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="tenant"></param>
    /// <param name="audience"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static string ValidateToken(string instance, string tenant, string audience, string token)
    {
        IdentityModelEventSource.ShowPII = true;

        ConfigurationManager<OpenIdConnectConfiguration> configManager =
            new ConfigurationManager<OpenIdConnectConfiguration>($"{instance}/{tenant}/v2.0/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
        OpenIdConnectConfiguration config;
        try
        {
            config = configManager.GetConfigurationAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex) 
        {
            return BuildErrorMessage(ex);
        }
        TokenValidationParameters validationParameters = new TokenValidationParameters()
        {
            IssuerSigningKeys = config.SigningKeys, 
            ValidAudience = audience,
            IssuerValidator = AadIssuerValidator.GetAadIssuerValidator($"{instance}/{tenant}").Validate,
        };

        JsonWebTokenHandler jsonWebTokenHandler = new();
        var result = jsonWebTokenHandler.ValidateToken(token, validationParameters);
        if (result.Exception != null && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            configManager.RequestRefresh();
            config = configManager.GetConfigurationAsync().GetAwaiter().GetResult();
            validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKeys = config.SigningKeys,
                ValidAudience = audience,
                IssuerValidator = AadIssuerValidator.GetAadIssuerValidator($"{instance}/{tenant}").Validate,
            };
            // attempt to validate token again after refresh
            result = jsonWebTokenHandler.ValidateToken(token, validationParameters);
        }
        if (result.Exception != null)
        {
            return BuildErrorMessage(result.Exception);
        }
        else
        {
            return result.Issuer;
        }
    }

    private static string BuildErrorMessage(Exception ex)
    {
        StringBuilder sb = new StringBuilder();
        BuildErrorMessage(sb, string.Empty, ex);
        return sb.ToString();
    }



    private static void BuildErrorMessage(StringBuilder sb, string tab, Exception ex)
    {
        sb.Append(tab);
        sb.Append(ex.ToString());
        if (ex.InnerException != null)
        {
            BuildErrorMessage(sb, tab+"   ", ex.InnerException);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ValidateToken")]
    public static IntPtr ValidateTokenNative(IntPtr instancePtr, IntPtr tenantPtr, IntPtr audiencePtr, IntPtr tokenPtr)
    {
        string? instance = Marshal.PtrToStringAnsi(instancePtr);
        string? tenant = Marshal.PtrToStringAnsi(tenantPtr);
        string? audience = Marshal.PtrToStringAnsi(audiencePtr);
        string? token = Marshal.PtrToStringAnsi(tokenPtr);

        string result = ValidateToken(instance!, tenant!, audience!, token!);
        return Marshal.StringToCoTaskMemUTF8(result);
    }
}
