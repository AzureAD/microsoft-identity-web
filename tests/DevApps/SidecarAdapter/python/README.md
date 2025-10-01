# Sidecar Adapter Python

This folder contains helper scripts for interacting with the Microsoft Identity Web Sidecar from Python.

## Contents

- `MicrosoftIdentityWebSidecarClient.py` – Typed client covering the Sidecar's `/Validate`, `/AuthorizationHeader`, and `/DownstreamApi` endpoints.
- `main.py` – Command-line harness that exercises the client and prints JSON responses.
- `get_token.py` – Helper for obtaining a user token via MSAL.
```

## Usage

Display the available commands:

```sh
uv run --with requests main.py --help
```

Example: validate an authorization header returned by `get_token.py`:

```sh
$token = uv run --with msal get_token.py
uv run --with requests main.py --base-url https://localhost:5001/sidecar --authorization-header "Bearer $token" validate
```

Invoke a downstream API by name, supplying an override scope and a JSON payload stored in `body.json`:

```sh
uv run --with requests main.py --base-url https://localhost:5001/sidecar --authorization-header "Bearer $token" `
  --scope User.Read invoke-downstream graphApi --body-file body.json
```

Invoke a downstream API by name, use the credentials configured by the application:

```sh
uv run --with requests main.py --base-url=http://localhost:5178 --agent-username=username --agent-identity=id invoke-downstream-unauth me
```

For client-credential flows, omit `--authorization-header` and use the unauthenticated commands such as `get-auth-header-unauth` or `invoke-downstream-unauth`.
