$xml = [Xml] (Get-Content src\Microsoft.Identity.Web\Microsoft.Identity.Web.csproj)

$proj = [xml](Get-Content src\Microsoft.Identity.Web\Microsoft.Identity.Web.csproj)
$proj.GetElementsByTagName("IdentityModelVersion") | foreach {
    $_."#text" = '6.*-*'
}
$proj.Save("src\Microsoft.Identity.Web\Microsoft.Identity.Web.csproj")
