from msal import PublicClientApplication, SerializableTokenCache
import os

# Persistent token cache
cache = SerializableTokenCache()

# Load cache from file if exists
if os.path.exists("token_cache.bin"):
    cache.deserialize(open("token_cache.bin", "r").read())

app = PublicClientApplication(
    client_id="f79f9db9-c582-4b7b-9d4c-0e8fd40623f0",
    authority="https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
    token_cache=cache
)

# Try silent acquisition first
accounts = app.get_accounts()
result = None

if accounts:
    result = app.acquire_token_silent(
        scopes=["api://556d438d-2f4b-4add-9713-ede4e5f5d7da/access_as_user"],
        account=accounts[0]
    )

if not result:
    result = app.acquire_token_interactive(scopes=["api://556d438d-2f4b-4add-9713-ede4e5f5d7da/access_as_user"])

# Save cache after acquisition
if cache.has_state_changed:
    with open("token_cache.bin", "w") as cache_file:
        cache_file.write(cache.serialize())

if (result):
    print("Access token acquired:", result["access_token"])
else:
    print("Failed to acquire token:", result.get("error"), result.get("error_description"))
