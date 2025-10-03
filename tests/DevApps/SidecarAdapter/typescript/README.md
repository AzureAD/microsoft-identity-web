# Sidecar Adapter TypeScript Sample

This sample demonstrates how a TypeScript client application can interact with a Node.js server that uses sidecar authentication to validate user requests.

## Overview

The sample consists of two main components:

1. **Server Application** (`src/server.ts`): A Node.js Express server that validates authentication tokens using a sidecar pattern
2. **Client Application** (`src/client.ts`): A TypeScript client that acquires tokens and calls the protected server APIs

## Architecture

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│   Client    │ Token   │    Server    │ Verify  │   Sidecar   │
│ Application ├────────►│ (Express)    ├────────►│   Service   │
│             │         │              │         │ (localhost  │
└─────────────┘         └──────────────┘         │   :5178)    │
                                                  └─────────────┘
```

### Flow

1. Client acquires an authentication token (using MSAL or similar)
2. Client sends HTTP request to server with token in Authorization header
3. Server middleware extracts and validates the token
4. Server validates token against sidecar service (simulated in this sample)
5. If valid, server processes the request and returns response
6. If invalid, server returns 401/403 error

## Prerequisites

- Node.js 18+ installed
- npm or yarn package manager

## Installation

```bash
# Install dependencies
npm install
```

## Configuration

The sample uses environment variables for configuration:

- `PORT`: Server port (default: 3000)
- `SIDECAR_PORT`: Sidecar service port (default: 5178)
- `CLIENT_ID`: Azure AD application client ID (optional for sample)
- `AUTHORITY`: Azure AD authority URL (optional for sample)

## Running the Sample

### Start the Server

```bash
# Build TypeScript files
npm run build

# Start the server
npm start

# Or run in development mode with ts-node
npm run dev
```

The server will start on `http://localhost:3000` and will display:
```
Sidecar sample server listening on http://localhost:3000 (sidecar base: http://localhost:5178)
```

### Run the Client

In a separate terminal:

```bash
# Run the client application
npm run build && node dist/src/client.js
```

### Run Tests

```bash
# Run all tests
npm test

# Run tests in watch mode
npm run test:watch
```

## API Endpoints

### Public Endpoints

- **GET /health**: Health check endpoint (no authentication required)
  ```json
  {
    "status": "healthy",
    "sidecarBase": "http://localhost:5178",
    "timestamp": "2024-01-01T00:00:00.000Z"
  }
  ```

### Protected Endpoints (Require Authentication)

- **GET /**: Root endpoint returning user information
  ```json
  {
    "message": "Successfully authenticated!",
    "user": {
      "userId": "sample-user-id",
      "name": "Sample User"
    },
    "timestamp": "2024-01-01T00:00:00.000Z"
  }
  ```

- **GET /api/data**: Returns sample data
  ```json
  {
    "data": [
      { "id": 1, "value": "Sample data 1" },
      { "id": 2, "value": "Sample data 2" },
      { "id": 3, "value": "Sample data 3" }
    ],
    "user": {
      "userId": "sample-user-id",
      "name": "Sample User"
    },
    "timestamp": "2024-01-01T00:00:00.000Z"
  }
  ```

## Authentication Flow

### Client Side

1. **Token Acquisition**: The client uses `@azure/msal-node` to acquire tokens
   ```typescript
   const token = await acquireTokenInteractive(['user.read']);
   ```

2. **API Call**: The client includes the token in the Authorization header
   ```typescript
   const response = await fetch(endpoint, {
     headers: {
       'Authorization': `Bearer ${token}`
     }
   });
   ```

### Server Side

1. **Token Extraction**: Middleware extracts the bearer token from the request
   ```typescript
   const token = req.token;
   ```

2. **Token Validation**: Token is validated against the sidecar service
   ```typescript
   const isValid = await validateTokenWithSidecar(token);
   ```

3. **Request Processing**: If valid, the request is processed; otherwise, 401/403 is returned

## Sidecar Pattern

In this sample, the "sidecar" pattern refers to an auxiliary service that handles authentication validation. The sidecar service:

- Runs alongside the main application (typically in the same pod/container group)
- Handles token validation and authentication logic
- Keeps authentication concerns separated from business logic
- Can be shared across multiple services

### Real-World Integration

In a production environment, you would:

1. Deploy an actual sidecar service (e.g., Microsoft Identity Web sidecar)
2. Configure it to validate tokens against Azure AD
3. Update `validateTokenWithSidecar()` to call the real sidecar endpoint
4. Handle token caching and refresh logic

Example real implementation:
```typescript
async function validateTokenWithSidecar(token: string): Promise<boolean> {
    const response = await fetch(`http://localhost:5178/validate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ token })
    });
    
    if (!response.ok) {
        return false;
    }
    
    const result = await response.json();
    return result.valid === true;
}
```

## Testing

The test suite (`test/sidecar.test.ts`) validates:

1. Token acquisition flow
2. Protected endpoint access with valid token
3. Rejection of requests without token
4. Public endpoint accessibility
5. API data retrieval

## Project Structure

```
typescript/
├── src/
│   ├── server.ts          # Express server with authentication
│   └── client.ts          # Client application with token acquisition
├── test/
│   └── sidecar.test.ts    # Integration tests
├── dist/                  # Compiled JavaScript output
├── package.json           # Project dependencies and scripts
├── tsconfig.json          # TypeScript configuration
└── README.md             # This file
```

## Common Issues

### Server Closes Connection

If you see `SocketError: other side closed`, ensure:
- Server is properly started and listening
- No other service is using port 3000
- Server stays alive during the entire test

### Authentication Failures

If authentication fails:
- Check that token is properly formatted
- Verify token validation logic
- Ensure bearer token middleware is configured correctly

## Next Steps

To integrate with a real Azure AD application:

1. Register an application in Azure AD
2. Update `CLIENT_ID` and `AUTHORITY` in the configuration
3. Implement real MSAL token acquisition (remove mock implementation)
4. Deploy a real sidecar service for token validation
5. Update `validateTokenWithSidecar()` to call the real service

## Resources

- [Microsoft Identity Platform](https://docs.microsoft.com/azure/active-directory/develop/)
- [MSAL Node](https://github.com/AzureAD/microsoft-authentication-library-for-js/tree/dev/lib/msal-node)
- [Microsoft Identity Web](https://github.com/AzureAD/microsoft-identity-web)
- [Express.js](https://expressjs.com/)

## License

MIT
