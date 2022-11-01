using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.Certificate
{
    internal class SignedAssertionFilePathCredentialsLoader : ICredentialSourceLoader
    {
        public CredentialSource CredentialSource => CredentialSource.SignedAssertionFilePath;

        public void LoadIfNeeded(CredentialDescription credentialDescription)
        {
            if (credentialDescription.SourceType == CredentialSource.SignedAssertionFilePath)
            {
                credentialDescription.CachedValue ??= new PodIdentityClientAssertion(credentialDescription.SignedAssertionFileDiskPath);
            }
        }
    }
}
