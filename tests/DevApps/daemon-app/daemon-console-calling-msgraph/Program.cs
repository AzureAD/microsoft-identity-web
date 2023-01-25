using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;

var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
tokenAcquirerFactory.Services.AddMicrosoftGraph();
var sp = tokenAcquirerFactory.Build();

var graphServiceClient = sp.GetRequiredService<GraphServiceClient>();
var users = await graphServiceClient.Users
    .Request()
    .WithAppOnly()
    .GetAsync();
Console.WriteLine($"{users.Count} users");
