using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using WebApi;

// simple console logger
//var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
tokenAcquirerFactory.Services.AddLogging(builder => builder.AddConsole());

tokenAcquirerFactory.Services.AddDownstreamApi("MyApi", 
    tokenAcquirerFactory.Configuration.GetSection("MyWebApi"));
var sp = tokenAcquirerFactory.Build();

var api = sp.GetRequiredService<IDownstreamApi>();
var result = await api.GetForAppAsync<IEnumerable<WeatherForecast>>("MyApi");
Console.WriteLine($"result = {result?.Count()}");

