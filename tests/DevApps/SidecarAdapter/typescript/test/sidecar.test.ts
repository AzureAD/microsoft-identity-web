import { PublicClientApplication } from "@azure/msal-node";
import open from "open";
import { describe, expect, test, afterAll, beforeAll } from "vitest";
import { startServer } from "../src/app";
import type { Server } from "http";

let server: Server;

beforeAll(async () => {
    server = await startServer({ port: 3000, sidecarBaseUrl: "http://localhost:5178" });
});

afterAll(async () => {
    await new Promise<void>((resolve, reject) =>
        server.close((error) => (error ? reject(error) : resolve())),
    );
});

const callAPI = async (accessToken: string) => {
    console.log("Access token acquired at: " + new Date().toString());
    await fetch("http://localhost:3000/", {
        method: "GET",
        headers: {
            Authorization: `Bearer ${accessToken}`
        }
    }).then((res: Response) => {
        console.log("Response status:", res.status);
        expect(res.status).toBe(200);
    });
};

// Open browser to sign user in and consent to scopes needed for application
const openBrowser = async (url: string) => {
    // You can open a browser window with any library or method you wish to use - the 'open' npm package is used here for demonstration purposes.
    open(url);
};

const loginRequest = {
    scopes: ["api://556d438d-2f4b-4add-9713-ede4e5f5d7da/access_as_user"],
    openBrowser,
    successTemplate: "Successfully signed in! You can close this window now." // Will be shown in the browser window after authentication is complete
};

// Create msal application object
const pca = new PublicClientApplication({
    auth: {
        clientId: "f79f9db9-c582-4b7b-9d4c-0e8fd40623f0",
        authority: "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
    }
});

describe("PublicClientApplication", () => {
    test("calls acquireTokenInteractive", async () => {

        console.log("Acquiring token interactively");

        await pca.acquireTokenInteractive(loginRequest).then(async (response) => {
            await callAPI(response.accessToken);
        }).catch((error) => {
            throw error;
        });
    }, 120000);
});
