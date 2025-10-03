import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { Server } from 'http';
import { acquireTokenInteractive, callAPI } from '../src/client';

// Import server components
let server: Server;
let app: any;

// Helper function to wait for server to be ready
async function waitForServer(port: number, maxRetries: number = 10): Promise<void> {
    for (let i = 0; i < maxRetries; i++) {
        try {
            const response = await fetch(`http://localhost:${port}/health`);
            if (response.ok) {
                return;
            }
        } catch (error) {
            // Server not ready yet
        }
        await new Promise(resolve => setTimeout(resolve, 500));
    }
    throw new Error('Server is not running.');
}

describe('PublicClientApplication', () => {
    beforeAll(async () => {
        // Start the server
        const serverModule = await import('../src/server');
        app = serverModule.app;
        server = serverModule.server;
        
        // Wait for server to be ready
        await waitForServer(3000);
        console.log('Test setup complete - server is ready');
    });

    afterAll(async () => {
        // Close the server
        if (server) {
            await new Promise<void>((resolve) => {
                server.close(() => {
                    console.log('Server closed');
                    resolve();
                });
            });
        }
    });

    it('calls acquireTokenInteractive', async () => {
        // Arrange
        console.log('Acquiring token interactively');
        
        // Act
        const token = await acquireTokenInteractive(['user.read']);
        
        // Assert
        expect(token).toBeDefined();
        expect(token.length).toBeGreaterThan(0);
        console.log(`Access token acquired at: ${new Date().toString()}`);
        
        // Now call the API with the token
        const response = await callAPI(token);
        
        // Assert API response
        expect(response).toBeDefined();
        expect(response.message).toBe('Successfully authenticated!');
        expect(response.user).toBeDefined();
        expect(response.user.userId).toBe('sample-user-id');
        
        console.log('Test completed successfully');
    });

    it('calls protected API endpoint', async () => {
        // Arrange
        const token = await acquireTokenInteractive(['user.read']);
        
        // Act
        const response = await callAPI(token, 'http://localhost:3000/api/data');
        
        // Assert
        expect(response).toBeDefined();
        expect(response.data).toBeDefined();
        expect(Array.isArray(response.data)).toBe(true);
        expect(response.data.length).toBe(3);
        expect(response.user).toBeDefined();
    });

    it('rejects requests without token', async () => {
        // Act & Assert
        await expect(async () => {
            const response = await fetch('http://localhost:3000/', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
        }).rejects.toThrow();
    });

    it('validates health endpoint is public', async () => {
        // Act
        const response = await fetch('http://localhost:3000/health');
        const data: any = await response.json();
        
        // Assert
        expect(response.ok).toBe(true);
        expect(data.status).toBe('healthy');
        expect(data.sidecarBase).toBe('http://localhost:5178');
    });
});
