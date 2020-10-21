using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Graph;

namespace Microsoft.Identity.Web
{
    internal class TokenAcquisitionAuthenticationProviderOption : IAuthenticationProviderOption
    {
        public string[]? Scopes { get; set; }
        public bool? AppOnly { get; set; }
    }
}
