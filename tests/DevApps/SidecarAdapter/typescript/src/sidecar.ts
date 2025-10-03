/*
 * Types and helper client for Microsoft Identity Web Sidecar interactions.
 * Generated manually from the OpenAPI description focusing on the Validate endpoint.
 */

export interface AuthorizationHeaderResult {
    authorizationHeader: string;
}

export type HeaderDictionary = Record<string, string[]>;

export interface DownstreamApiResult {
    statusCode: number;
    headers: HeaderDictionary;
    content: string | null;
}

export interface ProblemDetails {
    type?: string | null;
    title?: string | null;
    status?: number | null;
    detail?: string | null;
    instance?: string | null;
    [extension: string]: unknown;
}

export interface ValidateAuthorizationHeaderResult {
    protocol: string;
    token: string;
    claims: unknown;
}

export interface AcquireTokenOptionsOverrides {
    tenant?: string;
    forceRefresh?: boolean;
    claims?: string;
    correlationId?: string;
    longRunningWebApiSessionKey?: string;
    fmiPath?: string;
    popPublicKey?: string;
    managedIdentity?: {
        userAssignedClientId?: string;
    };
}

export interface RequestOptionsOverrides {
    scopes?: string[];
    requestAppToken?: boolean;
    baseUrl?: string;
    relativePath?: string;
    httpMethod?: string;
    acceptHeader?: string;
    contentType?: string;
    acquireTokenOptions?: AcquireTokenOptionsOverrides;
}

export interface RequestContextOverrides {
    agentIdentity?: string;
    agentUsername?: string;
    agentUserId?: string;
}

export interface ValidateRequestOptions {
    contextOverrides?: RequestContextOverrides;
    headers?: Record<string, string | undefined>;
    authorizationHeader?: string;
}

export interface SidecarClientOptions {
    /** Base URL pointing to the sidecar service (e.g. https://localhost:5001). */
    baseUrl: string;
    /** Optional default headers that will be sent with every request. */
    defaultHeaders?: Record<string, string>;
    /**
     * Override the fetch implementation. Defaults to the global fetch available in Node 18+ / browsers.
     */
    fetchImpl?: typeof fetch;
    /** Agent identity applied when no per-request override is provided. */
    agentIdentity?: string;
    /** Agent username applied when no per-request override is provided. */
    agentUsername?: string;
    /** Agent user object id applied when no per-request override is provided. */
    agentUserId?: string;
}

export class SidecarRequestError extends Error {
    readonly status: number | undefined;
    readonly problemDetails: ProblemDetails | undefined;

    constructor(message: string, status?: number, problemDetails?: ProblemDetails, cause?: unknown) {
        super(message, cause !== undefined ? { cause } : undefined);
        this.name = "SidecarRequestError";
        this.status = status;
        this.problemDetails = problemDetails;
    }
}

export class SidecarClient {
    private readonly baseUrl: string;
    private readonly defaultHeaders: Record<string, string>;
    private readonly fetchImpl: typeof fetch;
    private readonly defaultAgentIdentity: string | undefined;
    private readonly defaultAgentUsername: string | undefined;
    private readonly defaultAgentUserId: string | undefined;

    constructor(options: SidecarClientOptions) {
        this.baseUrl = SidecarClient.ensureBaseUrl(options.baseUrl);
        this.defaultHeaders = { ...(options.defaultHeaders ?? {}) };
        this.fetchImpl = options.fetchImpl ?? globalThis.fetch;
        if (typeof this.fetchImpl !== "function") {
            throw new Error("A fetch implementation is required. Ensure you are running on Node.js 18+ or provide fetchImpl explicitly.");
        }

        this.defaultAgentIdentity = options.agentIdentity;
        this.defaultAgentUsername = options.agentUsername;
    this.defaultAgentUserId = options.agentUserId;
    }

    async validateAuthorizationHeader(options?: ValidateRequestOptions): Promise<ValidateAuthorizationHeaderResult> {
        const params = this.buildQueryParameters(options?.contextOverrides);
        const url = this.createUrl("/Validate", params);

        const headers = new Headers(this.defaultHeaders);
        if (options?.headers) {
            for (const [key, value] of Object.entries(options.headers)) {
                if (value !== undefined) {
                    headers.set(key, value);
                }
            }
        }

        const authorizationHeader = options?.authorizationHeader;
        if (authorizationHeader) {
            headers.set("Authorization", authorizationHeader);
        }

        const response = await this.performRequest(url, { method: "GET", headers });
        return (await response.json()) as ValidateAuthorizationHeaderResult;
    }

    private async performRequest(url: URL, init: RequestInit): Promise<Response> {
        try {
            const response = await this.fetchImpl(url, init);
            if (!response.ok) {
                const problemDetails = await SidecarClient.tryParseProblemDetails(response);
                const message = problemDetails?.title ?? `${response.status} ${response.statusText}`;
                throw new SidecarRequestError(message, response.status, problemDetails);
            }

            return response;
        } catch (error) {
            if (error instanceof SidecarRequestError) {
                throw error;
            }

            throw new SidecarRequestError("Sidecar request failed", undefined, undefined, error);
        }
    }

    private buildQueryParameters(overrides?: RequestContextOverrides): URLSearchParams {
        const params = new URLSearchParams();

        const agentIdentity = overrides?.agentIdentity ?? this.defaultAgentIdentity;
        const agentUsername = overrides?.agentUsername ?? this.defaultAgentUsername;
        const agentUserId = overrides?.agentUserId ?? this.defaultAgentUserId;

        if (agentIdentity) {
            params.set("AgentIdentity", agentIdentity);
        }

        if (agentUsername) {
            params.set("AgentUsername", agentUsername);
        }

        if (agentUserId) {
            params.set("AgentUserId", agentUserId);
        }

        return params;
    }

    private createUrl(path: string, params: URLSearchParams): URL {
        const url = new URL(path, this.baseUrl);
        const query = params.toString();
        if (query) {
            url.search = query;
        }

        return url;
    }

    private static ensureBaseUrl(baseUrl: string): string {
        if (!baseUrl) {
            throw new Error("Sidecar baseUrl must be provided");
        }

        return baseUrl.endsWith("/") ? baseUrl : `${baseUrl}/`;
    }

    private static async tryParseProblemDetails(response: Response): Promise<ProblemDetails | undefined> {
        const contentType = response.headers.get("content-type") ?? "";
        if (contentType.includes("application/json")) {
            try {
                return (await response.json()) as ProblemDetails;
            } catch (error) {
                // Ignore JSON parse failures and fall through to undefined.
                console.error("Failed to parse ProblemDetails from response", error);
            }
        }

        return undefined;
    }
}
