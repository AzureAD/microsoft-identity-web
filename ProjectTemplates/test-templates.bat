echo "Build and Install templates"
dotnet pack AspNetCoreMicrosoftIdentityWebProjectTemplates.csproj
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.0.2.1-preview.nupkg

echo "Test templates"
mkdir tests
cd tests
dotnet new sln --name tests

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
dotnet new mvc2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add mvcwebapp-api.csproj
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, MVC, Multiple Orgs)"
mkdir mvcwebapp-multi-org-api
cd mvcwebapp-multi-org-api
dotnet new mvc2 --auth MultiOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
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
dotnet new webapp2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\tests.sln add webapp-api.csproj
cd ..

echo " Test Web app calling Web API  (Microsoft identity platform, Razor, Multiple Orgs)"
mkdir webapp-multi-org-api
cd webapp-multi-org-api
dotnet new webapp2 --auth MultiOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
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
dotnet new webapi2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
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


mkdir blazor
cd blazor

echo " Test Blazor app (No auth)"
mkdir blazorserver
cd blazorserver
dotnet new blazorserver2
dotnet sln ..\..\tests.sln add blazorserver.csproj
cd ..

echo " Test Blazor app (Microsoft identity platform, SingleOrg)"
mkdir blazorserver-SingleOrg
cd blazorserver-SingleOrg
dotnet new blazorserver2 --auth SingleOrg
dotnet sln ..\..\tests.sln add blazorserver-SingleOrg.csproj
cd ..

echo " Test Blazor app   (AzureAD B2C)"
mkdir blazorserver-b2c
cd blazorserver-b2c
dotnet new blazorserver2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add blazorserver-b2c.csproj
cd ..

echo " TTest Blazor app  calling Web API (Microsoft identity platform, SingleOrg)"
mkdir blazorserver-api
cd blazorserver-api
dotnet new blazorserver2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add blazorserver-api.csproj
cd ..

echo " Test Blazor app  calling Web API (AzureAD B2C)"
mkdir blazorserver-b2c-api
cd blazorserver-b2c-api
dotnet new blazorserver2 --auth IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add blazorserver-b2c-api.csproj
cd ..

echo " Test Blazor app  calling Graph (Microsoft identity platform, SingleOrg)"
mkdir blazorserver-graph
cd blazorserver-graph
dotnet new blazorserver2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add blazorserver-graph.csproj
cd ..

REM Blazor
cd ..

REM Blazor web assembly
mkdir blazorwasm2
cd blazorwasm2
echo "Test blazorwasm2, no authentication, "
mkdir blazorwasm2-noauth-nodownstreamapi
cd blazorwasm2-noauth-nodownstreamapi
dotnet new blazorwasm2
dotnet sln ..\..\tests.sln add blazorwasm2-noauth-nodownstreamapi.csproj
cd ..

echo "Test blazorwasm2, SingleOrg, "
mkdir blazorwasm2-singleorg-nodownstreamapi
cd blazorwasm2-singleorg-nodownstreamapi
dotnet new blazorwasm2 --auth SingleOrg
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-nodownstreamapi.csproj
cd ..

echo "Test blazorwasm2, SingleOrg, calling Microsoft Graph"
mkdir blazorwasm2-singleorg-callsgraph
cd blazorwasm2-singleorg-callsgraph
dotnet new blazorwasm2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-callsgraph.csproj
cd ..

echo "Test blazorwasm2, SingleOrg, calling a web API"
mkdir blazorwasm2-singleorg-callswebapi
cd blazorwasm2-singleorg-callswebapi
dotnet new blazorwasm2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-callswebapi.csproj
cd ..

echo "Test blazorwasm2, SingleOrg,  hosted"
mkdir blazorwasm2-singleorg-nodownstreamapi-hosted
cd blazorwasm2-singleorg-nodownstreamapi-hosted
dotnet new blazorwasm2 --auth SingleOrg  --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-nodownstreamapi-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-nodownstreamapi-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-nodownstreamapi-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, SingleOrg, calling Microsoft Graph, hosted"
mkdir blazorwasm2-singleorg-callsgraph-hosted
cd blazorwasm2-singleorg-callsgraph-hosted
dotnet new blazorwasm2 --auth SingleOrg --calls-graph --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-callsgraph-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-callsgraph-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-callsgraph-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, SingleOrg, calling a web API, hosted"
mkdir blazorwasm2-singleorg-callswebapi-hosted
cd blazorwasm2-singleorg-callswebapi-hosted
dotnet new blazorwasm2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read" --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-callswebapi-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-callswebapi-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-callswebapi-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, B2C, "
mkdir blazorwasm2-b2c-nodownstreamapi
cd blazorwasm2-b2c-nodownstreamapi
dotnet new blazorwasm2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add blazorwasm2-b2c-nodownstreamapi.csproj
cd ..

echo "Test blazorwasm2, B2C,  hosted"
mkdir blazorwasm2-b2c-nodownstreamapi-hosted
cd blazorwasm2-b2c-nodownstreamapi-hosted
dotnet new blazorwasm2 --auth IndividualB2C  --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-b2c-nodownstreamapi-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-b2c-nodownstreamapi-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-b2c-nodownstreamapi-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, B2C, calling a web API, hosted"
mkdir blazorwasm2-b2c-callswebapi-hosted
cd blazorwasm2-b2c-callswebapi-hosted
dotnet new blazorwasm2 --auth IndividualB2C --called-api-url "https://localhost:44332" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read" --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-b2c-callswebapi-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-b2c-callswebapi-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-b2c-callswebapi-hosted.Client.csproj
cd ..

REM Blazor web assembly
cd ..


echo "Build the solution with all the projects created by applying the templates"
dotnet build



echo "Uninstall templates"
cd ..
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
cd ..
cd ..
