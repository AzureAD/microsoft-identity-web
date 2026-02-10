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
    scopes: ["api://a021aff4-57ad-453a-bae8-e4192e5860f3/access_as_user"],
    openBrowser,
    successTemplate: "Successfully signed in! You can close this window now."
};

// Create msal application object
const pca = new PublicClientApplication({
    auth: {
        clientId: "9808c2f0-4555-4dc2-beea-b4dc3212d39e",
        authority: "https://login.microsoftonline.com/10c419d4-4a50-45b2-aa4e-919fb84df24f",
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
