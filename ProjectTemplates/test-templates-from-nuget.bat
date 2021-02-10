echo "Ensure ClientSemVer"
if "%ClientSemVer%" == "" (
set ClientSemVer=1.6.0
)

REM: This is to test Microsoft.Identity.Web templates
Set TemplateNugetPackageName="Microsoft.Identity.Web.ProjectTemplates"
Set templatePostFix=2

REM: Uncomment the 3 following lines to test ASP.NET Core SDK templates
REM ClientSemVer="5.0.5.0.0-ci"
REM TemplateNugetPackageName="Microsoft.DotNet.Web.ProjectTemplates"
REM Set templatePostFix=

echo "Ensure the tool to configure the templates is built"
dotnet build ..\tools\ConfigureGeneratedApplications

echo "Build and Install templates"
cd bin
cd Debug
dotnet new -u %TemplateNugetPackageName%
dotnet new -i %TemplateNugetPackageName%::%ClientSemVer%

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
dotnet new webapp%templatePostFix%
dotnet sln ..\..\tests.sln add webapp2-noauth.csproj
cd ..

echo "Test webapp2, single-org"
mkdir webapp2-singleorg
cd webapp2-singleorg
dotnet new webapp%templatePostFix% --auth SingleOrg
dotnet sln ..\..\tests.sln add webapp2-singleorg.csproj
cd ..

echo "Test webapp2, single-org, calling microsoft graph"
mkdir webapp2-singleorg-callsgraph
cd webapp2-singleorg-callsgraph
dotnet new webapp%templatePostFix% --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add webapp2-singleorg-callsgraph.csproj
cd ..

echo "Test webapp2, single-org, calling a downstream web api"
mkdir webapp2-singleorg-callswebapi
cd webapp2-singleorg-callswebapi
dotnet new webapp%templatePostFix% --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add webapp2-singleorg-callswebapi.csproj
cd ..

echo "Test webapp2, b2c"
mkdir webapp2-b2c
cd webapp2-b2c
dotnet new webapp%templatePostFix% --auth IndividualB2C
dotnet sln ..\..\tests.sln add webapp2-b2c.csproj
cd ..

echo "Test webapp2, b2c, calling a downstream web api"
mkdir webapp2-b2c-callswebapi
cd webapp2-b2c-callswebapi
dotnet new webapp%templatePostFix% --auth IndividualB2C --called-api-url "https://localhost:44332/api/todolist" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add webapp2-b2c-callswebapi.csproj
cd ..

cd ..

REM Web api
mkdir webapi2
cd webapi2
echo "Test webapi2, no auth"
mkdir webapi2-noauth
cd webapi2-noauth
dotnet new webapi%templatePostFix%
dotnet sln ..\..\tests.sln add webapi2-noauth.csproj
cd ..

echo "Test webapi2, single-org"
mkdir webapi2-singleorg
cd webapi2-singleorg
dotnet new webapi%templatePostFix% --auth SingleOrg
dotnet sln ..\..\tests.sln add webapi2-singleorg.csproj
cd ..

echo "Test webapi2, single-org, calling microsoft graph"
mkdir webapi2-singleorg-callsgraph
cd webapi2-singleorg-callsgraph
dotnet new webapi%templatePostFix% --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add webapi2-singleorg-callsgraph.csproj
cd ..

echo "Test webapi2, single-org, calling a downstream web api"
mkdir webapi2-singleorg-callswebapi
cd webapi2-singleorg-callswebapi
dotnet new webapi%templatePostFix% --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add webapi2-singleorg-callswebapi.csproj
cd ..

echo "Test webapi2, b2c"
mkdir webapi2-b2c
cd webapi2-b2c
dotnet new webapi%templatePostFix% --auth IndividualB2C
dotnet sln ..\..\tests.sln add webapi2-b2c.csproj
cd ..

cd ..

REM MVC Web app
mkdir mvc2
cd mvc2
echo "Test mvc2, no auth"
mkdir mvc2-noauth
cd mvc2-noauth
dotnet new mvc%templatePostFix%
dotnet sln ..\..\tests.sln add mvc2-noauth.csproj
cd ..

echo "Test mvc2, single-org"
mkdir mvc2-singleorg
cd mvc2-singleorg
dotnet new mvc%templatePostFix% --auth SingleOrg
dotnet sln ..\..\tests.sln add mvc2-singleorg.csproj
cd ..

echo "Test mvc2, single-org, calling microsoft graph"
mkdir mvc2-singleorg-callsgraph
cd mvc2-singleorg-callsgraph
dotnet new mvc%templatePostFix% --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add mvc2-singleorg-callsgraph.csproj
cd ..

echo "Test mvc2, single-org, calling a downstream web api"
mkdir mvc2-singleorg-callswebapi
cd mvc2-singleorg-callswebapi
dotnet new mvc%templatePostFix% --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add mvc2-singleorg-callswebapi.csproj
cd ..

echo "Test mvc2, b2c"
mkdir mvc2-b2c
cd mvc2-b2c
dotnet new mvc%templatePostFix% --auth IndividualB2C
dotnet sln ..\..\tests.sln add mvc2-b2c.csproj
cd ..

echo "Test mvc2, b2c, calling a downstream web api"
mkdir mvc2-b2c-callswebapi
cd mvc2-b2c-callswebapi
dotnet new mvc%templatePostFix% --auth IndividualB2C --called-api-url "https://localhost:44332/api/todolist" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add mvc2-b2c-callswebapi.csproj
cd ..

cd ..

REM Blazor server app
mkdir blazorserver2
cd blazorserver2
echo "Test blazorserver2, no auth"
mkdir blazorserver2-noauth
cd blazorserver2-noauth
dotnet new blazorserver%templatePostFix%
dotnet sln ..\..\tests.sln add blazorserver2-noauth.csproj
cd ..

echo "Test blazorserver2, single-org"
mkdir blazorserver2-singleorg
cd blazorserver2-singleorg
dotnet new blazorserver%templatePostFix% --auth SingleOrg
dotnet sln ..\..\tests.sln add blazorserver2-singleorg.csproj
cd ..

echo "Test blazorserver2, single-org, calling microsoft graph"
mkdir blazorserver2-singleorg-callsgraph
cd blazorserver2-singleorg-callsgraph
dotnet new blazorserver%templatePostFix% --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add blazorserver2-singleorg-callsgraph.csproj
cd ..

echo "Test blazorserver2, single-org, calling a downstream web api"
mkdir blazorserver2-singleorg-callswebapi
cd blazorserver2-singleorg-callswebapi
dotnet new blazorserver%templatePostFix% --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add blazorserver2-singleorg-callswebapi.csproj
cd ..

echo "Test blazorserver2, b2c"
mkdir blazorserver2-b2c
cd blazorserver2-b2c
dotnet new blazorserver%templatePostFix% --auth IndividualB2C
dotnet sln ..\..\tests.sln add blazorserver2-b2c.csproj
cd ..

echo "Test blazorserver2, b2c, calling a downstream web api"
mkdir blazorserver2-b2c-callswebapi
cd blazorserver2-b2c-callswebapi
dotnet new blazorserver%templatePostFix% --auth IndividualB2C --called-api-url "https://localhost:44332/api/todolist" --called-api-scopes "https://fabrikamb2c.onmicrosoft.com/tasks/read"
dotnet sln ..\..\tests.sln add blazorserver2-b2c-callswebapi.csproj
cd ..

cd ..

REM Blazor web assembly app
mkdir blazorwasm2
cd blazorwasm2
echo "Test blazorwasm2, no auth"
mkdir blazorwasm2-noauth
cd blazorwasm2-noauth
dotnet new blazorwasm%templatePostFix%
dotnet sln ..\..\tests.sln add blazorwasm2-noauth.csproj
cd ..

echo "Test blazorwasm2, single-org"
mkdir blazorwasm2-singleorg
cd blazorwasm2-singleorg
dotnet new blazorwasm%templatePostFix% --auth SingleOrg
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg.csproj
cd ..

echo "Test blazorwasm2, single-org, calling microsoft graph"
mkdir blazorwasm2-singleorg-callsgraph
cd blazorwasm2-singleorg-callsgraph
dotnet new blazorwasm%templatePostFix% --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-callsgraph.csproj
cd ..

echo "Test blazorwasm2, single-org, calling a downstream web api"
mkdir blazorwasm2-singleorg-callswebapi
cd blazorwasm2-singleorg-callswebapi
dotnet new blazorwasm%templatePostFix% --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add blazorwasm2-singleorg-callswebapi.csproj
cd ..

echo "Test blazorwasm2, single-org, with hosted blazor web server web api"
mkdir blazorwasm2-singleorg-hosted
cd blazorwasm2-singleorg-hosted
dotnet new blazorwasm%templatePostFix% --auth SingleOrg  --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, single-org, with hosted blazor web server web api, calling microsoft graph"
mkdir blazorwasm2-singleorg-callsgraph-hosted
cd blazorwasm2-singleorg-callsgraph-hosted
dotnet new blazorwasm%templatePostFix% --auth SingleOrg --calls-graph --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-callsgraph-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-callsgraph-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-callsgraph-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, single-org, with hosted blazor web server web api, calling a downstream web api"
mkdir blazorwasm2-singleorg-callswebapi-hosted
cd blazorwasm2-singleorg-callswebapi-hosted
dotnet new blazorwasm%templatePostFix% --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read" --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-singleorg-callswebapi-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-singleorg-callswebapi-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-singleorg-callswebapi-hosted.Client.csproj
cd ..

echo "Test blazorwasm2, b2c"
mkdir blazorwasm2-b2c
cd blazorwasm2-b2c
dotnet new blazorwasm%templatePostFix% --auth IndividualB2C
dotnet sln ..\..\tests.sln add blazorwasm2-b2c.csproj
cd ..

echo "Test blazorwasm2, b2c, with hosted blazor web server web api"
mkdir blazorwasm2-b2c-hosted
cd blazorwasm2-b2c-hosted
dotnet new blazorwasm%templatePostFix% --auth IndividualB2C  --hosted
dotnet sln ..\..\tests.sln add Shared\blazorwasm2-b2c-hosted.Shared.csproj
dotnet sln ..\..\tests.sln add Server\blazorwasm2-b2c-hosted.Server.csproj
dotnet sln ..\..\tests.sln add Client\blazorwasm2-b2c-hosted.Client.csproj
cd ..
cd ..

REM gRPC
mkdir worker2
cd worker2
echo "Test worker2, no auth"
mkdir worker2-noauth
cd worker2-noauth
dotnet new worker2
dotnet sln ..\..\tests.sln add worker2-noauth.csproj
cd ..

echo "Test worker2, single-org"
mkdir worker2-singleorg
cd worker2-singleorg
dotnet new worker2 --auth SingleOrg
dotnet sln ..\..\tests.sln add worker2-singleorg.csproj
cd ..

echo "Test worker2, b2c"
mkdir worker2-b2c
cd worker2-b2c
dotnet new worker2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add worker2-b2c.csproj
cd ..

echo "Test worker2, single-org, calling microsoft graph"
mkdir worker2-singleorg-callsgraph
cd worker2-singleorg-callsgraph
dotnet new worker2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add worker2-singleorg-callsgraph.csproj
cd ..

echo "Test worker2, single-org, calling a downstream web api"
mkdir worker2-singleorg-callswebapi
cd worker2-singleorg-callswebapi
dotnet new worker2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add worker2-singleorg-callswebapi.csproj
cd ..

cd ..

REM Azure Functions
mkdir func2
cd func2
echo "Test func2, no auth"
mkdir func2-noauth
cd func2-noauth
dotnet new func2
dotnet sln ..\..\tests.sln add func2-noauth.csproj
cd ..

echo "Test func2, single-org"
mkdir func2-singleorg
cd func2-singleorg
dotnet new func2 --auth SingleOrg
dotnet sln ..\..\tests.sln add func2-singleorg.csproj
cd ..

echo "Test func2, single-org, calling microsoft graph"
mkdir func2-singleorg-callsgraph
cd func2-singleorg-callsgraph
dotnet new func2 --auth SingleOrg --calls-graph
dotnet sln ..\..\tests.sln add func2-singleorg-callsgraph.csproj
cd ..

echo "Test func2, single-org, calling a downstream web api"
mkdir func2-singleorg-callswebapi
cd func2-singleorg-callswebapi
dotnet new func2 --auth SingleOrg --called-api-url "https://graph.microsoft.com/beta/me" --called-api-scopes "user.read"
dotnet sln ..\..\tests.sln add func2-singleorg-callswebapi.csproj
cd ..

echo "Test func2, b2c"
mkdir func2-b2c
cd func2-b2c
dotnet new func2 --auth IndividualB2C
dotnet sln ..\..\tests.sln add func2-b2c.csproj
cd ..

cd ..

echo "Configure the applications"
..\..\..\..\tools\ConfigureGeneratedApplications\bin\Debug\netcoreapp3.1\ConfigureGeneratedApplications.exe

echo "Build the solution with all the projects created by applying the templates"
dotnet build



echo "Uninstall templates"
cd ..
dotnet new -u %TemplateNugetPackageName%
cd ..
cd ..

