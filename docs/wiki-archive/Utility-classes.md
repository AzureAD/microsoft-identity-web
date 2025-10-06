Microsoft Identity Web contains additional classes that you might find useful.

### ClaimsPrincipalExtensions

In web apps that sign in users, ASP.NET Core transforms the claims in the ID token to a `ClaimsPrincipal` instance, held by the `HttpContext.User` property. In the same way, in protected web APIs, the claims from the JWT bearer token used to call the API are available in `HttpContext.User`.

The library provides extension methods to retrieve some of the relevant information about the user in the `ClaimsPrincipalExtensions` class.

<img alt="ClaimsPrincipalExtensions" src="https://user-images.githubusercontent.com/34331512/87454449-d5369580-c5b8-11ea-8a8c-51b47ddc349b.png" width="60%"/>

If you want to implement your own token cache serialization, you might want to use this class, for instance to get the key of the token cache to serialize (typically `GetMsalAccountId()`).

### ClaimsPrincipalFactory

In the other direction, `ClaimsPrincipalFactory` instantiates a `ClaimsPrincipal` from an account object ID and tenant ID. These methods can be useful when the web app or the web API subscribes to another service on behalf of the user, and then is called back by a notification where the users are identified by only their tenant ID and object ID. This is the case, for instance, for [Microsoft Graph Web Hooks](https://docs.microsoft.com/graph/api/resources/webhooks) [notifications](https://docs.microsoft.com/graph/webhooks#notification-example).

<img alt="ClaimsPrincipalFactory" src="https://user-images.githubusercontent.com/13203188/62538251-2fef3b00-b807-11e9-912f-2674972e9f48.png" width="70%"/>

### AccountExtensions

Finally, you can create a `ClaimsPrincipal` from an instance of MSAL.NET `IAccount`, using the `ToClaimsPrincipal` method in `AccountExtensions`.

<img alt="AccountExtensions" src="https://user-images.githubusercontent.com/13203188/62538259-341b5880-b807-11e9-9328-a094f79a0874.png" width="60%"/>
