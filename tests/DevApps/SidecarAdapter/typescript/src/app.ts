import express from 'express';
import type { Server } from 'http';
import type { NextFunction, Request, Response } from 'express';
import {
    SidecarClient,
    SidecarRequestError,
    type ValidateAuthorizationHeaderResult,
} from './sidecar';

declare module 'express-serve-static-core' {
    interface Request {
        sidecarValidation?: ValidateAuthorizationHeaderResult;
    }
}

export interface StartOptions {
    port?: number;
    sidecarBaseUrl?: string;
}

export const startServer = async ({
    port = Number(process.env.PORT ?? '3000'),
    sidecarBaseUrl = process.env.SIDECAR_BASE_URL ?? 'http://localhost:5178',
}: StartOptions = {}): Promise<Server> => {
    return new Promise((resolve) => {
        const app = express();
        const sidecarClient = new SidecarClient({ baseUrl: sidecarBaseUrl });

        app.use(async (req: Request, res: Response, next: NextFunction) => {
            console.log('Validating request via sidecar at:', new Date().toString());
            console.log('Request URL:', req.url);
            console.log('Request method:', req.method);

            const authorization = req.get('authorization');
            if (!authorization) {
                console.log('Missing Authorization header');
                res.status(401).json({ error: 'Missing Authorization header' });
                return;
            }

            console.log('Authorization header present, calling sidecar...');
            try {
                const validation = await sidecarClient.validateAuthorizationHeader({
                    authorizationHeader: authorization,
                });

                console.log('Sidecar validation successful');
                req.sidecarValidation = validation;
                next();
            } catch (error) {
                console.error('Sidecar validation error:', error);
                if (error instanceof SidecarRequestError) {
                    console.log('Returning SidecarRequestError response');
                    res
                        .status(error.status ?? 502)
                        .json(error.problemDetails ?? { message: error.message });
                    return;
                }

                console.log('Passing error to next handler');
                next(error);
            }
        });

        app.get('/', (req: Request, res: Response) => {
            console.log('Handling GET / request');
            console.log('Sidecar validation:', req.sidecarValidation);

            const responseData = {
                message: 'Request authenticated via Microsoft Identity Web Sidecar',
                protocol: req.sidecarValidation?.protocol ?? null,
                token: req.sidecarValidation?.token ? '***redacted***' : null,
                claims: req.sidecarValidation?.claims ?? null,
            };

            console.log('Sending response:', responseData);
            res.json(responseData);
        });

        app.use((error: unknown, _req: Request, res: Response, _next: NextFunction) => {
            console.error('Unhandled error serving request', error);
            res.status(500).json({ error: 'Unexpected server error' });
        });

        const server = app.listen(port, () => {
            console.log(
                `Sidecar sample server listening on http://localhost:${port} (sidecar base: ${sidecarBaseUrl})`,
            );
            resolve(server);
        });
    });
};

if (require.main === module) {
    void startServer();
}