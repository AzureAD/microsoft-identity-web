using Microsoft.Identity.Abstractions;

namespace ContosoWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDownstreamApi _downstreamApi;

    public Worker(ILogger<Worker> logger, IDownstreamApi downstreamApi)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                var result = await _downstreamApi.CallApiAsync("MyWebApi");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
