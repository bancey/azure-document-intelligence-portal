# Document Intelligence Portal

A secure web application that leverages Azure Document Intelligence API to analyze documents stored in Azure Storage. Built with .NET 8, the portal uses Azure Managed Identity for authentication and provides a modern web interface for document analysis.

## Features

- 🔐 **Secure by Default**: Uses Azure Managed Identity for authentication
- 🧠 **AI-Powered Analysis**: Leverages Azure Document Intelligence pre-built models
- ☁️ **Cloud Native**: Seamlessly integrates with Azure Storage and scales automatically
- 📱 **Modern UI**: Responsive web interface with real-time analysis results
- 📊 **Comprehensive Analysis**: Extracts text, tables, key-value pairs, and entities
- 🔍 **Multiple Models**: Supports various document types (invoices, receipts, business cards, etc.)

## Architecture

The application consists of:

- **Web Application**: .NET 8 Web API with static file serving
- **Azure Storage**: Document storage and management
- **Azure Document Intelligence**: AI-powered document analysis
- **Azure Managed Identity**: Secure authentication without credentials
- **Modern Web UI**: HTML5/CSS3/JavaScript frontend

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Developer CLI (azd)](https://docs.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)
- Azure subscription with sufficient permissions

## Quick Start

### 1. Clone and Setup

```bash
git clone <repository-url>
cd document-intelligence-portal
```

### 2. Deploy to Azure

```bash
# Initialize the Azure Developer CLI
azd init

# Deploy the infrastructure and application
azd up
```

### 3. Configure Storage

After deployment, upload some test documents to your Azure Storage account:

```bash
# Create a test container
az storage container create --name "test-documents" --account-name <your-storage-account>

# Upload sample documents
az storage blob upload --file sample.pdf --container-name "test-documents" --name sample.pdf --account-name <your-storage-account>
```

### 4. Access the Application

Open the web application URL provided by `azd up` and start analyzing documents!

## Local Development

### 1. Install Dependencies

```bash
dotnet restore
```

### 2. Configure Application Settings

Update `appsettings.Development.json`:

```json
{
  "Azure": {
    "StorageAccountName": "your-dev-storage-account",
    "DocumentIntelligence": {
      "Endpoint": "https://your-doc-intel-resource.cognitiveservices.azure.com/"
    }
  }
}
```

### 3. Run the Application

```bash
dotnet run
```

The application will be available at `https://localhost:7000` and `http://localhost:5000`.

## Configuration

### Environment Variables

The application uses these configuration keys:

- `Azure__StorageAccountName`: Name of the Azure Storage account
- `Azure__DocumentIntelligence__Endpoint`: Document Intelligence service endpoint
- `AZURE_CLIENT_ID`: Managed Identity client ID (set automatically when deployed)

### Supported Document Types

The application supports various pre-built models:

- **General Document**: Extract text and layout from any document
- **Layout Analysis**: Detailed layout analysis with reading order
- **Text Extraction**: OCR text extraction
- **Business Card**: Extract contact information from business cards
- **Invoice**: Extract structured data from invoices
- **Receipt**: Extract data from receipts
- **ID Document**: Extract information from identity documents

## API Endpoints

### Storage Operations

- `GET /api/storage/containers` - List all storage containers
- `GET /api/storage/containers/{container}/documents` - List documents in a container
- `GET /api/storage/containers/{container}/documents/{blob}/download` - Download a document

### Document Analysis

- `POST /api/documentanalysis/analyze` - Analyze a document by blob URI (requires SAS token)
- `POST /api/documentanalysis/analyze/{container}/{blob}` - Analyze a document by path (with SAS)
- `POST /api/documentanalysis/analyze/stream` - **[Recommended]** Analyze document by streaming from storage (no SAS required)
- `POST /api/documentanalysis/analyze/stream/{container}/{blob}` - Analyze document by streaming with path parameters
- `GET /api/documentanalysis/models` - Get available analysis models
- `GET /api/documentanalysis/result/{operationId}` - Get analysis result by operation ID

#### Streaming Analysis (Recommended)

The streaming endpoints (`/analyze/stream`) are the recommended approach as they:

- **No SAS Tokens Required**: Direct streaming from storage using managed identity
- **Better Security**: No need to generate and manage temporary access tokens
- **Improved Performance**: Optimized with retry logic and error handling
- **Simplified Integration**: Direct container/blob name specification

Example request to stream and analyze a document:

```bash
curl -X POST "https://your-app.azurewebsites.net/api/documentanalysis/analyze/stream" \
  -H "Content-Type: application/json" \
  -d '{
    "containerName": "documents",
    "blobName": "invoice.pdf",
    "modelId": "prebuilt-invoice",
    "includeFieldElements": true
  }'
```

### API Documentation

Access the Swagger UI at `/swagger` when running the application.

## Security

The application implements several security best practices:

- **Managed Identity**: No stored credentials or connection strings
- **HTTPS Only**: All communication is encrypted
- **RBAC**: Fine-grained role-based access control
- **Least Privilege**: Minimal required permissions for each service

### Required Azure Roles

The managed identity needs these roles:

- **Storage Blob Data Reader**: Read access to storage blobs
- **Cognitive Services User**: Access to Document Intelligence service

## Monitoring and Logging

The application includes:

- **Application Insights**: Performance monitoring and telemetry
- **Log Analytics**: Centralized logging
- **Health Checks**: Built-in health monitoring endpoint at `/health`

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   - Ensure managed identity is properly configured
   - Verify role assignments are in place
   - Check if using correct storage account name

2. **Document Analysis Failures**
   - Verify Document Intelligence endpoint is correct
   - Ensure document is in a supported format
   - Check if the document is accessible from Azure

3. **Storage Access Issues**
   - Confirm storage account exists and is accessible
   - Verify container and blob names are correct
   - Check managed identity has Storage Blob Data Reader role

### Debugging

Enable detailed logging by setting the log level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Azure": "Debug"
    }
  }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Related Documentation

- [Azure Document Intelligence](https://docs.microsoft.com/en-us/azure/cognitive-services/form-recognizer/)
- [Azure Storage](https://docs.microsoft.com/en-us/azure/storage/)
- [Azure Managed Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [.NET 8](https://docs.microsoft.com/en-us/dotnet/)
