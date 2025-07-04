# Azure Document Intelligence Portal - Authentication Configuration

This document describes the authentication configuration options available for different deployment scenarios.

## Authentication Modes

The application supports multiple authentication modes via the `Azure:AuthenticationMode` configuration setting:

### 1. DevelopmentStorage (Local Development/Testing)
```json
{
  "Azure": {
    "AuthenticationMode": "DevelopmentStorage"
  },
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  }
}
```

Use this mode for:
- Local development with Azurite (Azure Storage Emulator)
- Unit and integration testing
- CI/CD pipelines without Azure resources

### 2. ManagedIdentity (Production - Recommended)
```json
{
  "Azure": {
    "AuthenticationMode": "ManagedIdentity"
  },
  "AZURE_CLIENT_ID": "your-managed-identity-client-id"
}
```

Use this mode for:
- Production deployments on Azure App Service
- Azure Container Instances
- Azure Virtual Machines with managed identity

### 3. ServicePrincipal (CI/CD Scenarios)
```json
{
  "Azure": {
    "AuthenticationMode": "ServicePrincipal"
  }
}
```

Environment variables required:
- `AZURE_TENANT_ID`
- `AZURE_CLIENT_ID` 
- `AZURE_CLIENT_SECRET`

Use this mode for:
- GitHub Actions or Azure DevOps pipelines
- Automated deployments
- Cross-tenant scenarios

### 4. DefaultCredential (Fallback)
```json
{
  "Azure": {
    "AuthenticationMode": "DefaultCredential"
  }
}
```

This uses Azure's DefaultAzureCredential chain:
1. Environment variables
2. Workload Identity
3. Managed Identity
4. Azure CLI
5. Visual Studio

## Local Development Setup

1. **Install Azurite** (Azure Storage Emulator):
   ```bash
   npm install -g azurite
   azurite --silent --location ./azurite
   ```

2. **Configure authentication** in `appsettings.Development.json`:
   ```json
   {
     "Azure": {
       "AuthenticationMode": "DevelopmentStorage",
       "StorageAccountName": "devstoreaccount1"
     },
     "ConnectionStrings": {
       "AzureStorage": "UseDevelopmentStorage=true"
     }
   }
   ```

3. **Run the application**:
   ```bash
   dotnet run --project src/DocumentIntelligencePortal
   ```

## Testing Configuration

Tests use the `DevelopmentStorage` authentication mode by default. See `tests/DocumentIntelligencePortal.Tests/appsettings.Test.json` for configuration.

## Troubleshooting

### "DefaultAzureCredential failed to retrieve a token"
- Ensure the correct authentication mode is configured
- For local development, use `DevelopmentStorage` mode
- For production, ensure managed identity is properly configured

### "Azure:StorageAccountName configuration is missing"
- Add the storage account name to your configuration
- For development, use `devstoreaccount1` with Azurite

### "The logger is already frozen"
- This indicates a configuration issue in testing
- Ensure tests properly mock Azure services or use development configuration