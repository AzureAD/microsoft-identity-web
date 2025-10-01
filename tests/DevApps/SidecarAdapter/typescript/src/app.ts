import express from "express";
import type { NextFunction, Request, Response } from "express";
import {
    SidecarClient,
    SidecarRequestError,
    type ValidateAuthorizationHeaderResult,
} from "./sidecar";

declare module "express-serve-static-core" {
    interface Request {
        sidecarValidation?: ValidateAuthorizationHeaderResult;
    }
}

const port = Number(process.env.PORT ?? "3000");
const app = express();

const sidecarBaseUrl = process.env.SIDECAR_BASE_URL ?? "http://localhost:5178";
const sidecarClient = new SidecarClient({
    baseUrl: sidecarBaseUrl
});

app.use(async (req: Request, res: Response, next: NextFunction) => {
    const authorization = req.get("authorization");

    if (!authorization) {
        res.status(401).json({ error: "Missing Authorization header" });
        return;
    }

    try {
        const validation = await sidecarClient.validateAuthorizationHeader({
            authorizationHeader: authorization,
        });

        req.sidecarValidation = validation;
        next();
    } catch (error) {
        if (error instanceof SidecarRequestError) {
            res
                .status(error.status ?? 502)
                .json(error.problemDetails ?? { message: error.message });
            return;
        }

        next(error);
    }
});

app.get("/", (req: Request, res: Response) => {
    res.json({
        message: "Request authenticated via Microsoft Identity Web Sidecar",
        protocol: req.sidecarValidation?.protocol ?? null,
        token: req.sidecarValidation?.token ? "***redacted***" : null,
        claims: req.sidecarValidation?.claims ?? null,
    });
});

app.use((error: unknown, _req: Request, res: Response, _: NextFunction) => {
    console.error("Unhandled error serving request", error);
    res.status(500).json({ error: "Unexpected server error" });
});

app.listen(port, () => {
    console.log(
        `Sidecar sample server listening on http://localhost:${port} (sidecar base: ${sidecarBaseUrl})`,
    );
});
