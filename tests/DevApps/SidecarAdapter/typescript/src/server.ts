import express, { Request, Response, NextFunction } from 'express';
import bearerToken from 'express-bearer-token';

const app = express();
const PORT = process.env.PORT || 3000;
const SIDECAR_PORT = process.env.SIDECAR_PORT || 5178;

// Middleware to parse bearer tokens
app.use(bearerToken());

// Middleware to validate authentication
const authenticateToken = async (req: Request, res: Response, next: NextFunction) => {
    const token = req.token;

    if (!token) {
        return res.status(401).json({ error: 'No token provided' });
    }

    try {
        // In a real scenario, this would validate the token against the sidecar service
        // For this sample, we'll perform a simple validation
        
        // Simulate sidecar token validation
        const isValid = await validateTokenWithSidecar(token);
        
        if (!isValid) {
            return res.status(403).json({ error: 'Invalid token' });
        }

        // Attach user info to request (in real scenario, this would come from token validation)
        (req as any).user = {
            userId: 'sample-user-id',
            name: 'Sample User'
        };

        next();
    } catch (error) {
        console.error('Authentication error:', error);
        return res.status(500).json({ error: 'Authentication failed' });
    }
};

// Simulated sidecar token validation
async function validateTokenWithSidecar(token: string): Promise<boolean> {
    // In a real implementation, this would make a call to the sidecar service
    // running on localhost:5178 to validate the token
    
    // For this sample, we'll do a simple check
    if (!token || token.length < 10) {
        return false;
    }

    // Simulate async validation
    return new Promise((resolve) => {
        setTimeout(() => {
            // Accept any token that starts with 'Bearer' or is long enough
            resolve(token.startsWith('eyJ') || token.length > 20);
        }, 10);
    });
}

// Public endpoint - no authentication required
app.get('/health', (req: Request, res: Response) => {
    res.json({ 
        status: 'healthy',
        sidecarBase: `http://localhost:${SIDECAR_PORT}`,
        timestamp: new Date().toISOString()
    });
});

// Protected endpoint - requires authentication
app.get('/', authenticateToken, (req: Request, res: Response) => {
    const user = (req as any).user;
    res.json({
        message: 'Successfully authenticated!',
        user: user,
        timestamp: new Date().toISOString()
    });
});

// Protected API endpoint
app.get('/api/data', authenticateToken, (req: Request, res: Response) => {
    const user = (req as any).user;
    res.json({
        data: [
            { id: 1, value: 'Sample data 1' },
            { id: 2, value: 'Sample data 2' },
            { id: 3, value: 'Sample data 3' }
        ],
        user: user,
        timestamp: new Date().toISOString()
    });
});

// Error handling middleware
app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
    console.error('Error:', err);
    res.status(500).json({ error: 'Internal server error' });
});

// Start server
const server = app.listen(PORT, () => {
    console.log(`Sidecar sample server listening on http://localhost:${PORT} (sidecar base: http://localhost:${SIDECAR_PORT})`);
});

// Graceful shutdown
process.on('SIGTERM', () => {
    console.log('SIGTERM signal received: closing HTTP server');
    server.close(() => {
        console.log('HTTP server closed');
    });
});

export { app, server };
