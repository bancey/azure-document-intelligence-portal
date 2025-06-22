# Deployment Guide

This guide walks you through deploying the Document Intelligence Portal to Azure.

## Prerequisites

Before you begin, ensure you have:

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- [Azure Developer CLI (azd)](https://docs.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) installed
- An Azure subscription with appropriate permissions
- Owner or Contributor role on the target subscription
- .NET 8 SDK installed locally (for development)

## Quick Deployment with Azure Developer CLI

### 1. Initialize the Project

```bash
# Clone or navigate to the project directory
cd document-intelligence-portal

# Initialize Azure Developer CLI
azd init
```

### 2. Deploy to Azure

```bash
# Deploy infrastructure and application
azd up
```

This command will:
- Create a new resource group
- Deploy Azure Storage Account
- Deploy Azure Document Intelligence service
- Deploy App Service with managed identity
- Configure role assignments
- Deploy the web application

### 3. Post-Deployment Setup

After deployment, you'll need to upload some test documents:

```bash
# Get the storage account name from azd output
STORAGE_ACCOUNT=$(azd env get-values | grep AZURE_STORAGE_ACCOUNT_NAME | cut -d'=' -f2 | tr -d '"')

# Create a test container
az storage container create \
  --name "test-documents" \
  --account-name $STORAGE_ACCOUNT \
  --auth-mode login

# Upload sample documents (optional)
az storage blob upload \
  --file "path/to/your/document.pdf" \
  --container-name "test-documents" \
  --name "sample.pdf" \
  --account-name $STORAGE_ACCOUNT \
  --auth-mode login
```

## Manual Deployment (Alternative)

If you prefer to deploy manually or customize the deployment:

### 1. Create Resource Group

```bash
az group create --name rg-document-intelligence --location eastus
```

### 2. Deploy Infrastructure

```bash
az deployment group create \
  --resource-group rg-document-intelligence \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json \
  --parameters environmentName=prod
```

### 3. Deploy Application

```bash
# Build and publish the application
dotnet publish -c Release -o ./publish

# Create a deployment package
cd publish
zip -r ../app.zip .
cd ..

# Deploy to App Service
az webapp deployment source config-zip \
  --resource-group rg-document-intelligence \
  --name your-webapp-name \
  --src app.zip
```

## Configuration

### Environment Variables

The application requires these configuration values:

| Variable | Description | Example |
|----------|-------------|---------|
| `Azure__StorageAccountName` | Name of the storage account | `mystorageaccount` |
| `Azure__DocumentIntelligence__Endpoint` | Document Intelligence endpoint | `https://myservice.cognitiveservices.azure.com/` |
| `AZURE_CLIENT_ID` | Managed Identity client ID | Set automatically by Azure |

### Storage Account Setup

1. **Create Containers**: Create containers in your storage account for different document types:
   ```bash
   az storage container create --name "invoices" --account-name $STORAGE_ACCOUNT
   az storage container create --name "receipts" --account-name $STORAGE_ACCOUNT
   az storage container create --name "contracts" --account-name $STORAGE_ACCOUNT
   ```

2. **Upload Documents**: Upload sample documents to test the application:
   ```bash
   az storage blob upload --file document.pdf --container-name invoices --name sample-invoice.pdf --account-name $STORAGE_ACCOUNT
   ```

### Security Configuration

The deployment automatically configures:

- **Managed Identity**: User-assigned managed identity for secure authentication
- **RBAC**: Least-privilege role assignments
- **HTTPS**: Forces HTTPS-only communication
- **CORS**: Configured for the web interface

## Verification

After deployment, verify the setup:

### 1. Check Application Health

```bash
# Get the web app URL
WEB_APP_URL=$(azd env get-values | grep WEB_APP_URL | cut -d'=' -f2 | tr -d '"')

# Test the health endpoint
curl "$WEB_APP_URL/health"
```

### 2. Test the API

```bash
# List storage containers
curl "$WEB_APP_URL/api/storage/containers"

# Get available models
curl "$WEB_APP_URL/api/documentanalysis/models"
```

### 3. Test the Web Interface

Open the web application URL in your browser and verify:

- Containers are listed correctly
- Documents appear in selected containers
- Document analysis works with test documents

## Troubleshooting

### Common Issues

1. **Permission Errors**
   - Ensure the managed identity has correct role assignments
   - Check if the user deploying has sufficient permissions

2. **Configuration Issues**
   - Verify storage account name and Document Intelligence endpoint
   - Check environment variables in the App Service

3. **Network Issues**
   - Ensure public network access is enabled for services
   - Check firewall settings if using private endpoints

### Debugging

Enable detailed logging by updating the App Service configuration:

```bash
az webapp config appsettings set \
  --resource-group rg-document-intelligence \
  --name your-webapp-name \
  --settings "Logging__LogLevel__Azure=Debug"
```

View application logs:

```bash
az webapp log tail \
  --resource-group rg-document-intelligence \
  --name your-webapp-name
```

## Cost Optimization

To minimize costs:

1. **Development Environment**:
   - Use B1 App Service plan for development
   - Use Standard_LRS storage account
   - Use F0 (Free) tier for Document Intelligence during development

2. **Production Environment**:
   - Scale App Service plan based on usage
   - Use appropriate storage tier (Hot/Cool)
   - Monitor and set up cost alerts

## Cleanup

To remove all resources:

```bash
# Using Azure Developer CLI
azd down

# Or manually delete the resource group
az group delete --name rg-document-intelligence --yes
```

## Next Steps

After successful deployment:

1. Set up monitoring and alerting
2. Configure backup and disaster recovery
3. Implement CI/CD pipelines
4. Add custom authentication if required
5. Scale based on usage patterns
