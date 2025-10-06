In the Distributed cache, when a user signs-out of a web app, Microsoft Identity Web removes their account from the cache, but not cached values relating to the OBO tokens, the tokens acquired by the web API for downstream web API calls.

This is because MSAL.NET has no context into the cached OBO items on the web API.

### Distributed L2 Token Caching Sign-out & Sign-in

In this diagram, a user has signed-in to the web app, which calls a web API, which itself calls a downstream web API (please note both the web app and the web API are sharing the same Redis cache). There is both a user token and an OBO token in the Redis cache. The user signs-out, the user token is removed from the cache. The same user signs-in again, and a new user token is cached, as well as a new OBO token. However, notice the previous OBO token is still in the cache. Even though it may or may not be expired, it will not be useable in calling the downstream web API. The number of items in the Redis cache is growing. 

![image](https://user-images.githubusercontent.com/19942418/111669678-45df2300-87d4-11eb-8217-becc641ff10a.png)

Now two different users have signed-in to the web app. Notice the number of OBO tokens are increasing, even though only two are currently valid.

![image](https://user-images.githubusercontent.com/19942418/111669723-51cae500-87d4-11eb-8d04-688db7af836e.png)

### How to evict OBO cached items?

In order to remove the OBO tokens from the Distributed cache, please set the `AbsoluteExpirationRelativeToNow` and/or `SlidingExpiration` in the `DistributedCacheEntryOptions`. We recommend using the `SlidingExpiration`, as shown in the diagram below.

By default OBO tokens have a 1 hour lifetime.

![image](https://user-images.githubusercontent.com/19942418/111669787-614a2e00-87d4-11eb-9ebb-882187d239d8.png)

Or set the `AbsoluteExpirationRelativeToNow`, in conjunction with the `SlidingExpiration`.
![image](https://user-images.githubusercontent.com/19942418/111669835-6c9d5980-87d4-11eb-8195-60e9e2f1d136.png)