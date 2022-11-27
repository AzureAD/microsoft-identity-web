namespace Microsoft.Identity.Web
{
    internal interface IMergedOptionsStore
    {
        public MergedOptions Get(string name);
    }
}
