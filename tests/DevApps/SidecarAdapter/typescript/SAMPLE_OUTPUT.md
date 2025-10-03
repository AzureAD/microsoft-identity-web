# Sample Output and Demonstration

This document shows the actual output from running the Sidecar Adapter TypeScript sample.

## Test Execution

Running `npm test` executes all integration tests:

```
> sidecar-adapter-sample@1.0.0 test
> vitest run

 RUN  v1.6.1 /home/runner/work/microsoft-identity-web/microsoft-identity-web/tests/DevApps/SidecarAdapter/typescript

stdout | test/sidecar.test.ts > PublicClientApplication
Sidecar sample server listening on http://localhost:3000 (sidecar base: http://localhost:5178)

stdout | test/sidecar.test.ts > PublicClientApplication
Test setup complete - server is ready

stdout | test/sidecar.test.ts > PublicClientApplication > calls acquireTokenInteractive
Acquiring token interactively
Access token acquired at: Fri Oct 03 2025 17:07:29 GMT+0000 (Coordinated Universal Time)
Calling API: http://localhost:3000/

stdout | test/sidecar.test.ts > PublicClientApplication > calls acquireTokenInteractive
API response: {
  message: 'Successfully authenticated!',
  user: { userId: 'sample-user-id', name: 'Sample User' },
  timestamp: '2025-10-03T17:07:29.118Z'
}
Test completed successfully

stdout | test/sidecar.test.ts > PublicClientApplication > calls protected API endpoint
Acquiring token interactively
Access token acquired at: Fri Oct 03 2025 17:07:29 GMT+0000 (Coordinated Universal Time)
Calling API: http://localhost:3000/api/data

stdout | test/sidecar.test.ts > PublicClientApplication > calls protected API endpoint
API response: {
  data: [
    { id: 1, value: 'Sample data 1' },
    { id: 2, value: 'Sample data 2' },
    { id: 3, value: 'Sample data 3' }
  ],
  user: { userId: 'sample-user-id', name: 'Sample User' },
  timestamp: '2025-10-03T17:07:29.233Z'
}

stdout | test/sidecar.test.ts > PublicClientApplication
Server closed

 ✓ test/sidecar.test.ts  (4 tests) 345ms

 Test Files  1 passed (1)
      Tests  4 passed (4)
   Start at  17:07:28
   Duration  742ms (transform 57ms, setup 0ms, collect 131ms, tests 345ms, environment 0ms, prepare 99ms)
```

## Manual Testing Examples

### 1. Health Endpoint (Public - No Authentication Required)

```bash
$ curl http://localhost:3000/health
```

**Response:**
```json
{
  "status": "healthy",
  "sidecarBase": "http://localhost:5178",
  "timestamp": "2025-10-03T17:09:35.081Z"
}
```

### 2. Protected Endpoint WITHOUT Token (Should Fail)

```bash
$ curl http://localhost:3000/
```

**Response:**
```json
{
  "error": "No token provided"
}
```

### 3. Protected Endpoint WITH Token (Should Succeed)

```bash
$ curl -H "Authorization: Bearer eyJhbGci..." http://localhost:3000/
```

**Response:**
```json
{
  "message": "Successfully authenticated!",
  "user": {
    "userId": "sample-user-id",
    "name": "Sample User"
  },
  "timestamp": "2025-10-03T17:10:20.437Z"
}
```

### 4. Data Endpoint WITH Token

```bash
$ curl -H "Authorization: Bearer eyJhbGci..." http://localhost:3000/api/data
```

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "value": "Sample data 1"
    },
    {
      "id": 2,
      "value": "Sample data 2"
    },
    {
      "id": 3,
      "value": "Sample data 3"
    }
  ],
  "user": {
    "userId": "sample-user-id",
    "name": "Sample User"
  },
  "timestamp": "2025-10-03T17:10:44.099Z"
}
```

## Client Application Output

Running the client application with `node dist/src/client.js`:

```
Acquiring token interactively
Access token acquired at: Fri Oct 03 2025 17:10:55 GMT+0000 (Coordinated Universal Time)
Calling API: http://localhost:3000/
API response: {
  message: 'Successfully authenticated!',
  user: { userId: 'sample-user-id', name: 'Sample User' },
  timestamp: '2025-10-03T17:10:55.504Z'
}
Client flow completed successfully
Response: {
  "message": "Successfully authenticated!",
  "user": {
    "userId": "sample-user-id",
    "name": "Sample User"
  },
  "timestamp": "2025-10-03T17:10:55.504Z"
}
Done!
```

## Flow Visualization

The sample demonstrates this complete flow:

```
┌────────────────┐
│ Client         │
│ (client.ts)    │
└───────┬────────┘
        │ 1. acquireTokenInteractive()
        │    → Simulates MSAL token acquisition
        │
        │ 2. Token acquired
        │
        ▼
┌────────────────┐
│ HTTP Request   │
│ with Bearer    │
│ Token          │
└───────┬────────┘
        │ 3. GET / with Authorization header
        │
        ▼
┌────────────────┐         ┌─────────────────┐
│ Express        │         │ Sidecar Service │
│ Server         │────────▶│ (simulated at   │
│ (server.ts)    │ Validate│ localhost:5178) │
└───────┬────────┘  Token  └─────────────────┘
        │ 4. Token validated
        │
        │ 5. Request processed
        │
        ▼
┌────────────────┐
│ Response with  │
│ User Data      │
└────────────────┘
```

## Key Features Demonstrated

1. ✅ **Token Acquisition**: Simulated MSAL-based token acquisition
2. ✅ **Bearer Token Authentication**: Proper Authorization header handling
3. ✅ **Sidecar Pattern**: Token validation delegated to sidecar service
4. ✅ **Protected Endpoints**: Middleware-based authentication
5. ✅ **Public Endpoints**: Health check without authentication
6. ✅ **Error Handling**: Proper 401/403 responses for invalid requests
7. ✅ **Integration Tests**: Comprehensive test coverage
8. ✅ **Client-Server Flow**: Complete end-to-end demonstration

## Success Metrics

- All 4 tests pass ✅
- Server starts and responds correctly ✅
- Authentication middleware works as expected ✅
- Public endpoints accessible without auth ✅
- Protected endpoints reject requests without tokens ✅
- Protected endpoints accept requests with valid tokens ✅
- Client successfully acquires token and calls API ✅
