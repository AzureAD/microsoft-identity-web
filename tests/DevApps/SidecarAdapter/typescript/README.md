# Typescript devapp adapter

## Install dependencies

```sh
npm install
```

## Build

```sh
npm run build
```


## Start server

```sh
npm run start
```

## Test

To run the test:

```sh
npm test
```

This is an integration style test. Make sure the sidecar is running.

This will use msal-node to acquire a token and call the sample server application which will call the sidecar.

## Proof-of-Possession (PoP) tokens

The sample server validates both `Bearer` and `PoP` (Signed HTTP Request) tokens. When the inbound `Authorization` header uses the `PoP` scheme, `app.ts` forwards the request line to the sidecar via the `original-method` and `original-uri` headers so the SHR signature can be bound to the request. `SidecarClient` accepts arbitrary headers through the `headers` option:

```ts
await sidecarClient.validateAuthorizationHeader({
    authorizationHeader: `PoP ${shrToken}`,
    headers: {
        'original-method': 'GET',
        'original-uri': 'https://api.contoso.com/data',
    },
});
```
