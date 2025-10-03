# Quick Start Guide

This guide shows you how to quickly run the Sidecar Adapter sample.

## Running the Sample

### Option 1: Automated Test (Recommended)

The easiest way to see the sample in action is to run the automated tests:

```bash
npm install
npm test
```

This will:
1. Start the server automatically
2. Run the client tests
3. Validate the complete authentication flow
4. Clean up and close the server

### Option 2: Manual Testing

To manually test the server and client:

#### Step 1: Start the Server

```bash
npm install
npm run build
npm start
```

You should see:
```
Sidecar sample server listening on http://localhost:3000 (sidecar base: http://localhost:5178)
```

#### Step 2: Test with Client

In a new terminal:

```bash
cd tests/DevApps/SidecarAdapter/typescript
node dist/src/client.js
```

You should see output like:
```
Acquiring token interactively
Access token acquired at: [timestamp]
Calling API: http://localhost:3000/
API response: {
  message: 'Successfully authenticated!',
  user: { userId: 'sample-user-id', name: 'Sample User' },
  timestamp: '[timestamp]'
}
Client flow completed successfully
Done!
```

#### Step 3: Test with curl

Test the health endpoint (public):
```bash
curl http://localhost:3000/health
```

Expected response:
```json
{
  "status": "healthy",
  "sidecarBase": "http://localhost:5178",
  "timestamp": "2025-10-03T17:00:00.000Z"
}
```

Test the protected endpoint without token (should fail):
```bash
curl http://localhost:3000/
```

Expected response:
```json
{
  "error": "No token provided"
}
```

Test the protected endpoint with token (should succeed):
```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" http://localhost:3000/
```

Expected response:
```json
{
  "message": "Successfully authenticated!",
  "user": {
    "userId": "sample-user-id",
    "name": "Sample User"
  },
  "timestamp": "2025-10-03T17:00:00.000Z"
}
```

## Understanding the Flow

1. **Client acquires token**: The client uses MSAL (or mock implementation) to get an authentication token
2. **Client calls API**: The client includes the token in the `Authorization` header
3. **Server validates token**: The server extracts and validates the token via the sidecar pattern
4. **Server responds**: If valid, the server processes the request and returns data

## Key Files

- `src/server.ts`: Express server with authentication middleware
- `src/client.ts`: Client application with token acquisition
- `test/sidecar.test.ts`: Automated integration tests
- `README.md`: Comprehensive documentation

## Troubleshooting

### Port Already in Use

If port 3000 is already in use:
```bash
PORT=3001 npm start
```

Then update the client endpoint accordingly.

### Tests Failing

Ensure no other service is running on port 3000:
```bash
lsof -i :3000
# Kill any process using the port
kill -9 <PID>
```

### Dependencies Not Installing

Try clearing npm cache:
```bash
rm -rf node_modules package-lock.json
npm install
```

## Next Steps

See the [README.md](README.md) for:
- Complete architecture overview
- Integration with real Azure AD
- Production deployment considerations
- Security best practices
