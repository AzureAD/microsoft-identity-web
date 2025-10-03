import express from 'express';
import type { Server } from 'http';

interface ServerOptions {
    port: number;
    sidecarBaseUrl: string;
}

export function startServer(options: ServerOptions): Promise<Server> {
    return new Promise((resolve, reject) => {
        const app = express();

        // Add middleware
        app.use(express.json());

        // Health check endpoint
        app.get('/health', (req, res) => {
            res.json({ status: 'ok' });
        });

        // Main endpoint
        app.get('/', (req, res) => {
            console.log('Received request with headers:', req.headers);

            // Check for authorization header
            const authHeader = req.headers.authorization;
            if (!authHeader || !authHeader.startsWith('Bearer ')) {
                return res.status(401).json({ error: 'Unauthorized' });
            }

            res.json({
                message: 'Server is running',
                timestamp: new Date().toISOString()
            });
        });

        const server = app.listen(options.port, () => {
            console.log(`Server listening on port ${options.port}`);
            resolve(server);
        });

        server.on('error', (error) => {
            console.error('Server error:', error);
            reject(error);
        });
    });
}
