echo "Build and Install templates"
msbuild /t:restore AspNetCoreMicrosoftIdentityWebProjectTemplates.csproj
msbuild /t:pack AspNetCoreMicrosoftIdentityWebProjectTemplates.csproj
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.0.1.0.nupkg

echo "Test templates"
mkdir tests
cd tests

echo " Test Web app (Microsoft identity platform, MVC, Single Org)"
mkdir mvcwebapp
cd mvcwebapp
dotnet new mvc2 --auth SingleOrg
msbuild
cd ..

echo " Test Web app (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org
cd mvcwebapp-multi-org
dotnet new mvc2 --auth MultiOrg
msbuild
cd ..

echo " Test Web app (MVC, Azure AD B2C)"
mkdir mvcwebapp-b2c
cd mvcwebapp-b2c
dotnet new mvc2 --auth  IndividualB2C
msbuild
cd ..


echo " Test Web app (Microsoft identity platform, Razor, Single Org)"
mkdir webapp
cd webapp
dotnet new webapp2 --auth SingleOrg
msbuild
cd ..

echo " Test Web app (Microsoft identity platform, Razor, Multiple Orgs)"
mkdir webapp-multi-org
cd webapp-multi-org
dotnet new webapp2 --auth MultiOrg
msbuild
cd ..

echo " Test Web app Razor, Azure AD B2C)"
mkdir webapp-b2c
cd webapp-b2c
dotnet new webapp2 --auth  IndividualB2C
msbuild
cd ..


echo " Test Web API  (Microsoft identity platform, SingleOrg)"
mkdir webapi
cd webapi
dotnet new webapi2 --auth SingleOrg
msbuild
cd ..

echo " Test Web API  (AzureAD B2C)"
mkdir webapi-b2c
cd webapi-b2c
dotnet new webapi2 --auth IndividualB2C
msbuild
cd ..

echo "Uninstall templates"
cd ..
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
cd ..
cd ..
