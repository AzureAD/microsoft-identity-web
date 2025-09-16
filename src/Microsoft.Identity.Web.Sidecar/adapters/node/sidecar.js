/* Thin wrapper for Microsoft.Identity.Web.Sidecar | v1 */
import https from 'https';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  [k: string]: any;
}

export interface AuthorizationHeaderResult {
  authorizationHeader: string;
}

export interface ValidateAuthorizationHeaderResult {
  protocol: string;
  token: string;
  claims: any; // JsonNode => unknown shape
}

export interface ManagedIdentityOptions {
  userAssignedClientId?: string | null;
}

export interface AcquireTokenOptions {
  authenticationOptionsName?: string | null;
  correlationId?: string | null;
  extraQueryParameters?: Record<string, string> | null;
  extraParameters?: Record<string, any> | null;
  extraHeadersParameters?: Record<string, string> | null;
  claims?: string | null;
  fmiPath?: string | null;
  forceRefresh?: boolean;
  popPublicKey?: string | null;
  popClaim?: string | null;
  managedIdentity?: ManagedIdentityOptions | null;
  longRunningWebApiSessionKey?: string | null;
  tenant?: string | null;
  userFlow?: string | null;
}

export interface DownstreamApiOptions {
  scopes?: string[] | null;
  // serializer / deserializer / customizeHttpRequestMessage omitted (not representable here)
  acceptHeader?: string;
  contentType?: string;
  extraHeaderParameters?: Record<string, string> | null;
  extraQueryParameters?: Record<string, string> | null;
  baseUrl?: string | null;
  relativePath?: string;
  httpMethod?: string; // default "Get"
  acquireTokenOptions?: AcquireTokenOptions;
  protocolScheme?: string; // default "Bearer"
  requestAppToken?: boolean;
}

export interface AuthorizationHeaderRequest {
  apiName: string;
  agentIdentity?: string;
  agentUsername?: string;
  tenant?: string;
  options?: DownstreamApiOptions;
  signal?: AbortSignal;
}

export interface SidecarClientOptions {
  baseUrl?: string;
  defaultHeaders?: Record<string, string>;
  fetchImpl?: typeof fetch;
  insecureHttpsDevOnly?: boolean;
}

export class SidecarClient {
  private baseUrl: string;
  private defaultHeaders: Record<string, string>;
  private fetchImpl: typeof fetch;
  private agent?: https.Agent;

  constructor(opts: SidecarClientOptions = {}) {
    this.baseUrl = (opts.baseUrl ?? 'https://localhost:7255').replace(/\/+$/, '');
    this.defaultHeaders = {
      'content-type': 'application/json',
      ...opts.defaultHeaders
    };
    this.fetchImpl = opts.fetchImpl ?? fetch;
    if (opts.insecureHttpsDevOnly) {
      // For local self-signed dev certs ONLY. Do not use in production.
      this.agent = new https.Agent({ rejectUnauthorized: false });
    }
  }

  static createInsecureForLocalhost(): SidecarClient {
    return new SidecarClient({ insecureHttpsDevOnly: true });
  }

  async validate(signal?: AbortSignal): Promise<ValidateAuthorizationHeaderResult> {
    const url = `${this.baseUrl}/Validate`;
    const res = await this.fetchImpl(url, {
      method: 'GET',
      headers: this.defaultHeaders,
      signal,
      // @ts-ignore Node fetch: pass agent
      agent: this.agent
    });
    return this.handleResponse<ValidateAuthorizationHeaderResult>(res);
  }

  async getAuthorizationHeader(req: AuthorizationHeaderRequest): Promise<AuthorizationHeaderResult> {
    if (!req || !req.apiName) throw new Error('apiName is required');
    const qp = new URLSearchParams();
    if (req.agentIdentity) qp.set('agentIdentity', req.agentIdentity);
    if (req.agentUsername) qp.set('agentUsername', req.agentUsername);
    if (req.tenant) qp.set('tenant', req.tenant);
    const query = qp.toString();
    const url = `${this.baseUrl}/AuthorizationHeader/${encodeURIComponent(req.apiName)}${query ? '?' + query : ''}`;

    const res = await this.fetchImpl(url, {
      method: 'POST',
      headers: this.defaultHeaders,
      body: req.options ? JSON.stringify(req.options) : '{}',
      signal: req.signal,
      // @ts-ignore
      agent: this.agent
    });

    return this.handleResponse<AuthorizationHeaderResult>(res);
  }

  private async handleResponse<T>(res: Response): Promise<T> {
    const text = await res.text();
    let data: any = undefined;
    if (text.length) {
      try { data = JSON.parse(text); } catch { /* leave raw */ }
    }
    if (!res.ok) {
      const problem: ProblemDetails = (data && typeof data === 'object') ? data : { title: 'HTTP error', detail: text };
      problem.status = res.status;
      throw Object.assign(new Error(problem.title || `HTTP ${res.status}`), { problem });
    }
    return data as T;
  }
}

/* Example usage (remove in production):
(async () => {
  const client = SidecarClient.createInsecureForLocalhost();
  try {
    const validated = await client.validate();
    console.log('Validate:', validated);
    const header = await client.getAuthorizationHeader({
      apiName: 'MyDownstreamApi',
      options: {
        scopes: ['api://guid/.default'],
        relativePath: '/v1/data',
        httpMethod: 'Get'
      }
    });
    console.log('Auth header:', header.authorizationHeader);
  } catch (e: any) {
    console.error('Error', e.problem ?? e);
  }
})();
*/