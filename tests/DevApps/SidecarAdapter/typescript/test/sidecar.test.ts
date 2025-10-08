import { PublicClientApplication, AuthenticationResult } from "@azure/msal-node";
import open from "open";
import { describe, expect, test, afterAll, beforeAll } from "vitest";
import { startServer } from "../src/app";
import type { Server } from "http";

let server: Server;

// Open browser to sign user in and consent to scopes needed for application
const openBrowser = async (url: string): Promise<void> => {
    await open(url);
};

interface LoginRequest {
    scopes: string[];
    openBrowser: (url: string) => Promise<void>;
    successTemplate: string;
}

const loginRequest: LoginRequest = {
    scopes: ["api://556d438d-2f4b-4add-9713-ede4e5f5d7da/access_as_user"],
    openBrowser,
    successTemplate: "Successfully signed in! You can close this window now."
};

// Create msal application object
const pca = new PublicClientApplication({
    auth: {
        clientId: "f79f9db9-c582-4b7b-9d4c-0e8fd40623f0",
        authority: "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
    }
});

describe("PublicClientApplication", () => {
    beforeAll(async () => {
        console.log("Starting server...");
        server = await startServer({ port: 5555, sidecarBaseUrl: "http://localhost:5178" });
        console.log("Server started successfully");

        // Give the server a moment to fully initialize
        await new Promise(resolve => setTimeout(resolve, 1000));
    });

    afterAll(async () => {
        console.log("Shutting down server...");
        if (server) {
            await new Promise<void>((resolve, reject) =>
                server.close((error) => (error ? reject(error) : resolve())),
            );
        }
    });

    test("calls acquireTokenInteractive", async () => {
        console.log("Acquiring token interactively");

        try {
            const response: AuthenticationResult = await pca.acquireTokenInteractive(loginRequest);
            console.log("Access token acquired at: " + new Date().toString());

            await callAPI(response.accessToken);
        } catch (error) {
            console.error("Authentication error:", error);
            throw error;
        }
    }, 120000);
});

async function callAPI(accessToken: string): Promise<void> {
    console.log("Making request to server with token...");

    try {
        const response = await fetch("http://localhost:5555/", {
            method: "GET",
            headers: {
                Authorization: `Bearer ${accessToken}`,
                "Content-Type": "application/json"
            }
        });

        console.log("Response status:", response.status);
        expect(response.status).toBe(200);

        const responseBody = await response.json();

        expect(responseBody).toHaveProperty("claims");
        expect(responseBody).toHaveProperty("token");
        expect(responseBody).toHaveProperty("protocol", "Bearer");

        console.log("Response body:", responseBody);
    } catch (error) {
        console.error("Fetch error:", error);
        throw error;
    }
}
