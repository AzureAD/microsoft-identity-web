echo "Build and Install templates"
dotnet pack AspNetCoreMicrosoftIdentityWebProjectTemplates.csproj
cd bin
cd Debug
dotnet new -i Microsoft.Identity.Web.ProjectTemplates.0.4.0-preview.nupkg

echo "Test templates"
mkdir tests
cd tests
dotnet new sln --name tests

REM Razor web app
mkdir webapp2
cd webapp2
echo "Test webapp2, no auth"
mkdir webapp2-noauth
cd webapp2-noauth
dotnet new webapp2
dotnet sln ..\..\tests.sln add webapp2-noauth.csproj
cd ..

echo "Test webapp2, single-org"
mkdir webapp2-singleorg
cd webapp2-singleorg
dotnet new webapp2 --auth SingleOrg
dotnet sln ..\..\tests.sln add webapp2-singleorg.csproj
cd ..

echo "Test webapp2, single-org, calling microsoft graph"
mkdir webapp2-singleorg-callsgraph
cd webapp2-singleorg-callsgraph
dotnet new webapp2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add webapp2-singleorg-callsgraph.csproj
cd ..

echo "Test webapp2, single-org, calling a downstream web api"
mkdir webapp2-singleorg-callswebapi
cd webapp2-singleorg-callswebapi
dotnet new webapp2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add webapp2-singleorg-callswebapi.csproj
cd ..

echo "Test webapp2, b2c"
mkdir webapp2-b2c
cd webapp2-b2c
dotnet new webapp2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add webapp2-b2c.csproj
cd ..

echo "Test webapp2, b2c, calling a downstream web api"
mkdir webapp2-b2c-callswebapi
cd webapp2-b2c-callswebapi
dotnet new webapp2 --auth IndividualB2C --called-api-url "https://localhost:44332/api/todolist" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add webapp2-b2c-callswebapi.csproj
cd ..

cd ..

REM Web api
mkdir webapi2
cd webapi2
echo "Test webapi2, no auth"
mkdir webapi2-noauth
cd webapi2-noauth
dotnet new webapi2
dotnet sln ..\..\tests.sln add webapi2-noauth.csproj
cd ..

echo "Test webapi2, single-org"
mkdir webapi2-singleorg
cd webapi2-singleorg
dotnet new webapi2 --auth SingleOrg
dotnet sln ..\..\tests.sln add webapi2-singleorg.csproj
cd ..

echo "Test webapi2, single-org, calling microsoft graph"
mkdir webapi2-singleorg-callsgraph
cd webapi2-singleorg-callsgraph
dotnet new webapi2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add webapi2-singleorg-callsgraph.csproj
cd ..

echo "Test webapi2, single-org, calling a downstream web api"
mkdir webapi2-singleorg-callswebapi
cd webapi2-singleorg-callswebapi
dotnet new webapi2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add webapi2-singleorg-callswebapi.csproj
cd ..

echo "Test webapi2, b2c"
mkdir webapi2-b2c
cd webapi2-b2c
dotnet new webapi2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add webapi2-b2c.csproj
cd ..

cd ..

REM MVC Web app
mkdir mvc2
cd mvc2
echo "Test mvc2, no auth"
mkdir mvc2-noauth
cd mvc2-noauth
dotnet new mvc2
dotnet sln ..\..\tests.sln add mvc2-noauth.csproj
cd ..

echo "Test mvc2, single-org"
mkdir mvc2-singleorg
cd mvc2-singleorg
dotnet new mvc2 --auth SingleOrg
dotnet sln ..\..\tests.sln add mvc2-singleorg.csproj
cd ..

echo "Test mvc2, single-org, calling microsoft graph"
mkdir mvc2-singleorg-callsgraph
cd mvc2-singleorg-callsgraph
dotnet new mvc2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add mvc2-singleorg-callsgraph.csproj
cd ..

echo "Test mvc2, single-org, calling a downstream web api"
mkdir mvc2-singleorg-callswebapi
cd mvc2-singleorg-callswebapi
dotnet new mvc2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add mvc2-singleorg-callswebapi.csproj
cd ..

echo "Test mvc2, b2c"
mkdir mvc2-b2c
cd mvc2-b2c
dotnet new mvc2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add mvc2-b2c.csproj
cd ..

echo "Test mvc2, b2c, calling a downstream web api"
mkdir mvc2-b2c-callswebapi
cd mvc2-b2c-callswebapi
dotnet new mvc2 --auth IndividualB2C --called-api-url "https://localhost:44332/api/todolist" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add mvc2-b2c-callswebapi.csproj
cd ..

cd ..

REM Blazor server app
mkdir blazorserver2
cd blazorserver2
echo "Test blazorserver2, no auth"
mkdir blazorserver2-noauth
cd blazorserver2-noauth
dotnet new blazorserver2
dotnet sln ..\..\tests.sln add blazorserver2-noauth.csproj
cd ..

echo "Test blazorserver2, single-org"
mkdir blazorserver2-singleorg
cd blazorserver2-singleorg
dotnet new blazorserver2 --auth SingleOrg
dotnet sln ..\..\tests.sln add blazorserver2-singleorg.csproj
cd ..

echo "Test blazorserver2, single-org, calling microsoft graph"
mkdir blazorserver2-singleorg-callsgraph
cd blazorserver2-singleorg-callsgraph
dotnet new blazorserver2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add blazorserver2-singleorg-callsgraph.csproj
cd ..

echo "Test blazorserver2, single-org, calling a downstream web api"
mkdir blazorserver2-singleorg-callswebapi
cd blazorserver2-singleorg-callswebapi
dotnet new blazorserver2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add blazorserver2-singleorg-callswebapi.csproj
cd ..

echo "Test blazorserver2, b2c"
mkdir blazorserver2-b2c
cd blazorserver2-b2c
dotnet new blazorserver2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add blazorserver2-b2c.csproj
cd ..

echo "Test blazorserver2, b2c, calling a downstream web api"
mkdir blazorserver2-b2c-callswebapi
cd blazorserver2-b2c-callswebapi
dotnet new blazorserver2 --auth IndividualB2C --called-api-url "https://localhost:44332/api/todolist" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add blazorserver2-b2c-callswebapi.csproj
cd ..

cd ..

REM Blazor web assembly app
mkdir blazorwasm2
cd blazorwasm2
echo "Test blazorwasm2, no auth"
mkdir blazorwasm2-noauth
cd blazorwasm2-noauth
dotnet new blazorwasm2
dotnet sln ..\..\tests.sln add blazorwasm2-noauth.csproj
cd ..

echo "Test blazorwasm2, single-org"
mkdir blazorwasm2-singleorg
cd blazorwasm2-singleorg
dotnet new blazorwasm2 --auth SingleOrg
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg.csproj
cd ..

echo "Test blazorwasm2, single-org, calling microsoft graph"
mkdir blazorwasm2-singleorg-callsgraph
cd blazorwasm2-singleorg-callsgraph
dotnet new blazorwasm2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-callsgraph.csproj
cd ..

echo "Test blazorwasm2, single-org, calling a downstream web api"
mkdir blazorwasm2-singleorg-callswebapi
cd blazorwasm2-singleorg-callswebapi
dotnet new blazorwasm2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-callswebapi.csproj
cd ..

echo "Test blazorwasm2, single-org, with hosted blazor web server web api"
mkdir blazorwasm2-singleorg-hosted
cd blazorwasm2-singleorg-hosted
dotnet new blazorwasm2 --auth SingleOrg  --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, single-org, with hosted blazor web server web api, calling microsoft graph"
mkdir blazorwasm2-singleorg-callsgraph-hosted
cd blazorwasm2-singleorg-callsgraph-hosted
dotnet new blazorwasm2 --auth SingleOrg --calls-graph --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-callsgraph-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-callsgraph-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-callsgraph-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, single-org, with hosted blazor web server web api, calling a downstream web api"
mkdir blazorwasm2-singleorg-callswebapi-hosted
cd blazorwasm2-singleorg-callswebapi-hosted
dotnet new blazorwasm2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read" --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-callswebapi-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-callswebapi-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-callswebapi-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, b2c"
mkdir blazorwasm2-b2c
cd blazorwasm2-b2c
dotnet new blazorwasm2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add blazorwasm2-b2c.csproj
cd ..

echo "Test blazorwasm2, b2c, with hosted blazor web server web api"
mkdir blazorwasm2-b2c-hosted
cd blazorwasm2-b2c-hosted
dotnet new blazorwasm2 --auth IndividualB2C  --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-b2c-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-b2c-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-b2c-hosted.Client.csproj
cd ..

cd ..

echo "Configure the applications"
..\..\..\..\tools\ConfigureGeneratedApplications\bin\Debug\net5.0\ConfigureGeneratedApplications.exe

echo "Build the solution with all the projects created by applying the templates"
dotnet build



echo "Uninstall templates"
cd ..
dotnet new -u Microsoft.Identity.Web.ProjectTemplates
cd ..
cd ..

