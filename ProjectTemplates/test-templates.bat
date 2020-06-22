echo "Build and Install templates"
dotnet pack AspNetCoreMicrosoftIdentityWebProjectTemplates.csproj
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.0.1.5.nupkg

echo "Test templates"
mkdir tests
cd tests

echo " Test Web app (No Auth)"
mkdir webapp-noauth
cd webapp-noauth
dotnet new webapp2
dotnet build
cd ..

echo " Test Web app (No Auth)"
mkdir mvcwebapp-noauth
cd mvcwebapp-noauth
dotnet new mvc2
dotnet build
cd ..

echo " Test Web API (No auth)"
mkdir webapi-noauth
cd webapi-noauth
dotnet new webapi2
dotnet build
cd ..


echo " Test Web app (Microsoft identity platform, MVC, Single Org)"
mkdir mvcwebapp
cd mvcwebapp
dotnet new mvc2 --auth SingleOrg
dotnet build
cd ..

echo " Test Web app (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org
cd mvcwebapp-multi-org
dotnet new mvc2 --auth MultiOrg
dotnet build
cd ..

echo " Test Web app (MVC, Azure AD B2C)"
mkdir mvcwebapp-b2c
cd mvcwebapp-b2c
dotnet new mvc2 --auth  IndividualB2C
dotnet build
cd ..


echo " Test Web app calling Web API (Microsoft identity platform, MVC, Single Org)"
mkdir mvcwebapp-api
cd mvcwebapp-api
dotnet new mvc2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet build
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org-api
cd mvcwebapp-multi-org-api
dotnet new mvc2 --auth MultiOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet build
cd ..

echo " Test Web app calling Web API  (MVC, Azure AD B2C)"
mkdir mvcwebapp-b2c-api
cd mvcwebapp-b2c-api
dotnet new mvc2 --auth  IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet build
cd ..

echo " Test Web app (Microsoft identity platform, Razor, Single Org)"
mkdir webapp
cd webapp
dotnet new webapp2 --auth SingleOrg
dotnet build
cd ..

echo " Test Web app (Microsoft identity platform, Razor, Multiple Orgs)"
mkdir webapp-multi-org
cd webapp-multi-org
dotnet new webapp2 --auth MultiOrg
dotnet build
cd ..

echo " Test Web app (Razor, Azure AD B2C)"
mkdir webapp-b2c
cd webapp-b2c
dotnet new webapp2 --auth  IndividualB2C
dotnet build
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, Razor, Single Org)"
mkdir webapp-api
cd webapp-api
dotnet new webapp2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet build
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, Razor, Multiple Orgs)"
mkdir webapp-multi-org-api
cd webapp-multi-org-api
dotnet new webapp2 --auth MultiOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet build
cd ..

echo " Test Web app calling Web API (Razor, Azure AD B2C)"
mkdir webapp-b2c-api
cd webapp-b2c-api
dotnet new webapp2 --auth  IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet build
cd ..



echo " Test Web API  (Microsoft identity platform, SingleOrg)"
mkdir webapi
cd webapi
dotnet new webapi2 --auth SingleOrg
dotnet build
cd ..

echo " Test Web API  (AzureAD B2C)"
mkdir webapi-b2c
cd webapi-b2c
dotnet new webapi2 --auth IndividualB2C
dotnet build
cd ..

echo " Test Web API calling Web API (Microsoft identity platform, SingleOrg)"
mkdir webapi-api
cd webapi-api
dotnet new webapi2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet build
cd ..

echo " Test Web API calling Web API (AzureAD B2C)"
mkdir webapi-b2c-api
cd webapi-b2c-api
dotnet new webapi2 --auth IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet build
cd ..


echo "Uninstall templates"
cd ..
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
cd ..
cd ..
