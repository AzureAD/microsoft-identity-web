Build & run:
1.	npm init -y
2.	Add "type": "module" (if desired) and install types: npm i -D typescript @types/node
3.	tsc --init (target ES2022)
4.	tsc
5.	Use: import { SidecarClient } from './dist/sidecarClient.js';
Notes:
•	For local HTTPS with self-signed cert, use createInsecureForLocalhost() only in development.
•	Extend error handling as needed (retry, logging, tracing).
•	Omitted serializer/deserializer/callback delegates because they are not representable over HTTP per the OpenAPI (server-side concerns).