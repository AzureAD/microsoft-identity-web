namespace DotnetTool.MicrosoftIdentityPlatformApplication
{
    internal class ResourceAndScope
    {
        public ResourceAndScope(string resource, string scope)
        {
            Resource = resource;
            Scope = scope;
        }
        public string Resource { get; set; }
        public string Scope { get; set; }
        public string ResourceServicePrincipalId { get; set; }
    }
}
