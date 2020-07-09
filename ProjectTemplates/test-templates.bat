echo "Build and Install templates"
dotnet pack AspNetCoreMicrosoftIdentityWebProjectTemplates.csproj
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.0.2.0-preview.nupkg

echo "Test templates"
mkdir tests
cd tests
dotnet new sln

echo " Test Web app (No Auth)"
mkdir webapp-noauth
cd webapp-noauth
dotnet new webapp2
dotnet sln ..\tests.sln add webapp-noauth.csproj
cd ..

echo " Test Web app (No Auth)"
mkdir mvcwebapp-noauth
cd mvcwebapp-noauth
dotnet new mvc2
dotnet sln ..\tests.sln add mvcwebapp-noauth.csproj
cd ..

echo " Test Web API (No auth)"
mkdir webapi-noauth
cd webapi-noauth
dotnet new webapi2
dotnet sln ..\tests.sln add webapi-noauth.csproj
cd ..


echo " Test Web app (Microsoft identity platform, MVC, Single Org)"
mkdir mvcwebapp
cd mvcwebapp
dotnet new mvc2 --auth SingleOrg
dotnet sln ..\tests.sln add mvcwebapp.csproj
cd ..

echo " Test Web app (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org
cd mvcwebapp-multi-org
dotnet new mvc2 --auth MultiOrg
dotnet sln ..\tests.sln add mvcwebapp-multi-org.csproj
cd ..

echo " Test Web app (MVC, Azure AD B2C)"
mkdir mvcwebapp-b2c
cd mvcwebapp-b2c
dotnet new mvc2 --auth  IndividualB2C
dotnet sln ..\tests.sln add mvcwebapp-b2c.csproj
cd ..


echo " Test Web app calling Web API (Microsoft identity platform, MVC, Single Org)"
mkdir mvcwebapp-api
cd mvcwebapp-api
dotnet new mvc2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add mvcwebapp-api.csproj
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org-api
cd mvcwebapp-multi-org-api
dotnet new mvc2 --auth MultiOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add mvcwebapp-multi-org-api.csproj
cd ..


echo " Test Web app calling Microsoft Graph (Microsoft identity platform, MVC, Single Orgs)"
mkdir mvcwebapp-graph
cd mvcwebapp-graph
dotnet new mvc2 --auth SingleOrg --calls-graph --called-api-scopes "user.read"
dotnet sln ..\tests.sln add mvcwebapp-graph.csproj
cd ..

echo " Test Web app calling Microsoft Graph (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org-graph
cd mvcwebapp-multi-org-graph
dotnet new mvc2 --auth MultiOrg --calls-graph --called-api-scopes "user.read"
dotnet sln ..\tests.sln add mvcwebapp-multi-org-graph.csproj
cd ..


echo " Test Web app calling Web API  (MVC, Azure AD B2C)"
mkdir mvcwebapp-b2c-api
cd mvcwebapp-b2c-api
dotnet new mvc2 --auth  IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\tests.sln add mvcwebapp-b2c-api.csproj
cd ..


echo " Test Web app (Microsoft identity platform, Razor, Single Org)"
mkdir webapp
cd webapp
dotnet new webapp2 --auth SingleOrg
dotnet sln ..\tests.sln add webapp.csproj
cd ..

echo " Test Web app (Microsoft identity platform, Razor, Multiple Orgs)"
mkdir webapp-multi-org
cd webapp-multi-org
dotnet new webapp2 --auth MultiOrg
dotnet sln ..\tests.sln add webapp-multi-org.csproj
cd ..

echo " Test Web app (Razor, Azure AD B2C)"
mkdir webapp-b2c
cd webapp-b2c
dotnet new webapp2 --auth  IndividualB2C
dotnet sln ..\tests.sln add webapp-b2c.csproj
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, Razor, Single Org)"
mkdir webapp-api
cd webapp-api
dotnet new webapp2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add webapp-api.csproj
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, Razor, Multiple Orgs)"
mkdir webapp-multi-org-api
cd webapp-multi-org-api
dotnet new webapp2 --auth MultiOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add webapp-multi-org-api.csproj
cd ..

echo " Test Web app calling Web API (Razor, Azure AD B2C)"
mkdir webapp-b2c-api
cd webapp-b2c-api
dotnet new webapp2 --auth  IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\tests.sln add webapp-b2c-api.csproj
cd ..

echo " Test Web app calling Microsoft Graph (Microsoft identity platform, Razor, Single Org)"
mkdir webapp-graph
cd webapp-graph
dotnet new webapp2 --auth SingleOrg --calls-graph --called-api-scopes "user.read"
dotnet sln ..\tests.sln add webapp-graph.csproj
cd ..

echo " Test Web app calling Microsoft Graph (Microsoft identity platform, Razor, Single Org)"
mkdir webapp-graph-multiorg
cd webapp-graph-multiorg
dotnet new webapp2 --auth MultiOrg --calls-graph --called-api-scopes "user.read"
dotnet sln ..\tests.sln add webapp-graph-multiorg.csproj
cd ..


echo " Test Web API  (Microsoft identity platform, SingleOrg)"
mkdir webapi
cd webapi
dotnet new webapi2 --auth SingleOrg
dotnet sln ..\tests.sln add webapi.csproj
cd ..

echo " Test Web API  (AzureAD B2C)"
mkdir webapi-b2c
cd webapi-b2c
dotnet new webapi2 --auth IndividualB2C
dotnet sln ..\tests.sln add webapi-b2c.csproj
cd ..

echo " Test Web API calling Web API (Microsoft identity platform, SingleOrg)"
mkdir webapi-api
cd webapi-api
dotnet new webapi2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add webapi-api.csproj
cd ..

echo " Test Web API calling Web API (AzureAD B2C)"
mkdir webapi-b2c-api
cd webapi-b2c-api
dotnet new webapi2 --auth IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\tests.sln add webapi-b2c-api.csproj
cd ..

echo " Test Web API calling Graph (Microsoft identity platform, SingleOrg)"
mkdir webapi-graph
cd webapi-graph
dotnet new webapi2 --auth SingleOrg --calls-graph
dotnet sln ..\tests.sln add webapi-graph.csproj
cd ..

echo "Build the solution with all the projects created by applying the templates"
dotnet build

echo "Uninstall templates"
cd ..
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
cd ..
cd ..
