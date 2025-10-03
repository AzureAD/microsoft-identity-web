import { PublicClientApplication, Configuration, AccountInfo } from '@azure/msal-node';

/**
 * Configuration for the MSAL Public Client Application
 * This simulates the sidecar authentication flow
 */
const config: Configuration = {
    auth: {
        clientId: process.env.CLIENT_ID || 'sample-client-id',
        authority: process.env.AUTHORITY || 'https://login.microsoftonline.com/common',
    },
    system: {
        loggerOptions: {
            loggerCallback(loglevel, message, containsPii) {
                if (!containsPii) {
                    console.log(message);
                }
            },
            piiLoggingEnabled: false,
            logLevel: 3, // Info
        }
    }
};

/**
 * PublicClientApplication for acquiring tokens
 */
const pca = new PublicClientApplication(config);

/**
 * Acquires a token interactively (simulated for testing)
 * In a real scenario, this would open a browser for user authentication
 */
export async function acquireTokenInteractive(scopes: string[]): Promise<string> {
    console.log('Acquiring token interactively');
    
    try {
        // In a real implementation, this would use MSAL to acquire a token
        // For this sample, we'll simulate it
        
        // Simulate token acquisition delay
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Generate a mock JWT-like token
        const mockToken = generateMockToken();
        
        console.log(`Access token acquired at: ${new Date().toString()}`);
        
        return mockToken;
    } catch (error) {
        console.error('Error acquiring token:', error);
        throw error;
    }
}

/**
 * Acquires a token silently from cache if available
 */
export async function acquireTokenSilent(scopes: string[], account: AccountInfo): Promise<string> {
    console.log('Acquiring token silently');
    
    try {
        // In a real implementation, this would get token from cache
        // For this sample, we'll simulate it
        
        const mockToken = generateMockToken();
        
        console.log(`Access token acquired silently at: ${new Date().toString()}`);
        
        return mockToken;
    } catch (error) {
        console.error('Error acquiring token silently:', error);
        throw error;
    }
}

/**
 * Calls the protected API with the acquired token
 */
export async function callAPI(accessToken: string, endpoint: string = 'http://localhost:3000/'): Promise<any> {
    console.log(`Calling API: ${endpoint}`);
    
    try {
        const response = await fetch(endpoint, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${accessToken}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('API response:', data);
        
        return data;
    } catch (error) {
        console.error('Error calling API:', error);
        throw error;
    }
}

/**
 * Generates a mock JWT-like token for testing
 */
function generateMockToken(): string {
    // Create a mock JWT structure (header.payload.signature)
    const header = Buffer.from(JSON.stringify({ alg: 'RS256', typ: 'JWT' })).toString('base64url');
    const payload = Buffer.from(JSON.stringify({
        sub: 'sample-user-id',
        name: 'Sample User',
        iat: Math.floor(Date.now() / 1000),
        exp: Math.floor(Date.now() / 1000) + 3600 // 1 hour
    })).toString('base64url');
    const signature = Buffer.from('mock-signature').toString('base64url');
    
    return `${header}.${payload}.${signature}`;
}

/**
 * Main client application flow
 */
export async function runClient(): Promise<void> {
    try {
        // Acquire token interactively
        const token = await acquireTokenInteractive(['user.read']);
        
        // Call the protected API
        const response = await callAPI(token);
        
        console.log('Client flow completed successfully');
        console.log('Response:', JSON.stringify(response, null, 2));
    } catch (error) {
        console.error('Client flow failed:', error);
        throw error;
    }
}

// Run the client if this file is executed directly
if (require.main === module) {
    runClient()
        .then(() => {
            console.log('Done!');
            process.exit(0);
        })
        .catch((error) => {
            console.error('Error:', error);
            process.exit(1);
        });
}
