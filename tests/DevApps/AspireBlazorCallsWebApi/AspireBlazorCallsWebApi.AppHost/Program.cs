var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject("apiservice", "../AspireBlazorCallsWebApi.ApiService/AspireBlazorCallsWebApi.ApiService.csproj");

builder.AddProject("webfrontend", "../AspireBlazorCallsWebApi.Web/AspireBlazorCallsWebApi.Web.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
