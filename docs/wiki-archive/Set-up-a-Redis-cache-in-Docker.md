## How to set up a Redis cache in a Docker container for local testing

First, install [Docker desktop](https://www.docker.com/products/docker-desktop) and run through the set-up wizard, and pull a redis image (`docker pull redis`).

You'll need to add the following NuGet packages to your app:  `Microsoft.Extensions.Caching.StackExchangeRedis` and `StackExchange.Redis`.

Then, in `Startup.cs` add the Redis connection

```csharp
services.AddStackExchangeRedisCache(options =>
     {
         options.Configuration = Configuration.GetConnectionString("Redis");
         options.InstanceName = "RedisDemos_"; // unique to the app
     });
```

And make sure you add `.AddDistributedTokenCaches();`:
```csharp
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
             Configuration.GetSection("AzureAd").Bind(options);
        })
            .EnableTokenAcquisitionToCallDownstreamApi()
                .AddDownstreamWebApi("TodoList", Configuration.GetSection("TodoList"))
                .AddDistributedTokenCaches();
 ```
 
Finally, in `appsettings.json`, you'll add the Redis Connection string:

```Json
"ConnectionStrings": {
        "Redis": "localhost:5002" // configure w/docker
    }
```

You're ready to start the Redis cache & start your web app and web API locally.

```shell
docker run --name my-redis -p 5002:6379 -d redis
docker exec -it my-redis sh
redis-cli
scan 0
hgetall [cachekey]

exit
exit

docker ps -a
docker stop [<numeric value of redis instance. ex. 442>]
docker rm [<numeric value of redis instance. ex. 442>]
```