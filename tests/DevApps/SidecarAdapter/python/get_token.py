import argparse
import os

from msal import PublicClientApplication, SerializableTokenCache

# Persistent token cache
cache = SerializableTokenCache()

# Load cache from file if exists
if os.path.exists("token_cache.bin"):
    cache.deserialize(open("token_cache.bin", "r").read())

parser = argparse.ArgumentParser(
    description="Acquire a token using MSAL with a persistent cache."
)
parser.add_argument(
    "--client-id",
    required=True,
    help="The application (client) ID registered in Azure AD."
)
parser.add_argument(
    "--authority",
    required=True,
    help="The authority URL, e.g. https://login.microsoftonline.com/<tenant>."
)
parser.add_argument(
    "--scope",
    required=True,
    help="The scope for the access token."
)
args = parser.parse_args()

client_id = args.client_id
authority = args.authority
scope = args.scope

app = PublicClientApplication(
    client_id=client_id,
    authority=authority,
    token_cache=cache
)

# Try silent acquisition first
accounts = app.get_accounts()
result = None

if accounts:
    result = app.acquire_token_silent(
        scopes=[scope],
        account=accounts[0]
    )

if not result:
    result = app.acquire_token_interactive(scopes=[scope])

# Save cache after acquisition
if cache.has_state_changed:
    with open("token_cache.bin", "w") as cache_file:
        cache_file.write(cache.serialize())

if (result):
    print(result["access_token"])
else:
    print("Failed to acquire token:", result.get("error"), result.get("error_description"))
