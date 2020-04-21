echo "Build and Install templates"
msbuild /t:pack AspNetCoreMicrosoftIdentityPlatformTemplates.csproj
cd bin
cd Debug
dotnet new -i Microsoft.IdentityPlatform.Templates.0.1.0.nupkg

echo "Test templates"
mkdir tests
cd tests

echo "Test Web app (Microsoft identity platform, Single Org)"
mkdir webapp
cd webapp
dotnet new mvc2 --auth SingleOrg
msbuild
cd ..

echo "Test Web app (Microsoft identity platform, Mutiple Orgs)"
mkdir webapp-multi-org
cd webapp-multi-org
dotnet new mvc2 --auth MultiOrg
msbuild
cd ..

echo "Test Web app (Azure AD B2C)"
mkdir webapp-b2c
cd webapp-b2c
dotnet new mvc2 --auth  IndividualB2C
msbuild
cd ..


echo "Test Web API  (Microsoft identity platform, SingleOrg)"
mkdir webapi
cd webapi
dotnet new webapi2 --auth SingleOrg
msbuild
cd ..

echo "Test Web API  (AzureAD B2C)"
mkdir webapi-b2c
cd webapi-b2c
dotnet new webapi2 --auth IndividualB2C
msbuild
cd ..

echo "Uninstall templates"
cd ..
dotnet new -u Microsoft.IdentityPlatform.Templates
cd ..
cd ..
