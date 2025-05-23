# VM-Hosted Key Vault Secret Retriever

Tiny console app that runs **inside an Azure VM** configured with a **User-Assigned Managed Identity (UAMI)**.  
Its only job is to fetch **Secret** from an Azure Key Vault section in *appsettings.json*.

---

## 1. Prerequisites

| Requirement | Notes |
|-------------|-------|
| Azure VM     | Any OS; the UAMI must be **assigned** to the VM. |
| User-Assigned Managed Identity | Needs ** `Get`** permission on the Key Vault’s **Secrets**. |
| Key Vault    | Secret named `secret` (or whatever your *AzureKeyVault* section points to). |
| .NET 8 SDK   | Build / run the app locally or on the VM. |
| *appsettings.json* | Contains an `AzureKeyVault` block with `BaseUrl`, `RelativePath`, etc. |

> 💡 **Least privilege**: grant the UAMI only the `secrets/get` permission.

---

## 2. How the Code Works

```txt
┌─────────────┐    1. TokenAcquirerFactory auto-binds
│ appsettings │─── to Azure credentials (UAMI) on the VM.
└─────────────┘
        │
        ▼
┌────────────────────┐   2. Register “AzureKeyVault” downstream API
│  DI Service graph  │── using the config section.
└────────────────────┘
        │
        ▼
┌───────────────────────┐
│  IDownstreamApi.Call  │ 3. GET {vault-url}/secrets/secret?api-version=7.4
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│  HttpResponseMessage  │ 4. Parse JSON; extract `value`.
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│  Console logging      │ 5. Log *success*.
└───────────────────────┘
```
