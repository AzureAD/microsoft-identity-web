using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace WebAppCallsMicrosoftGraph.Pages
{
    public class ClientCredentialsModel : PageModel
    {
        public ClientCredentialsModel(IOptionsMonitor<MicrosoftIdentityOptions> optionsMonitor)
        {
            OptionsMonitor = optionsMonitor;
        }

        class CertificateJsonConverter : JsonConverter<X509Certificate2>
        {
            public override X509Certificate2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return null;
            }

            public override void Write(Utf8JsonWriter writer, X509Certificate2 value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("Thumbprint", value.Thumbprint);
                writer.WriteEndObject();
            }
        }

        public IOptionsMonitor<MicrosoftIdentityOptions> OptionsMonitor { get; }

        public void OnGet()
        {
            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            jsonSerializerOptions.Converters.Add(new CertificateJsonConverter());


            MicrosoftIdentityOptions options = OptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
            string json = JsonSerializer.Serialize(options.ClientCredentials, jsonSerializerOptions);
            ViewData["json"] = json;
        }
    }
}
