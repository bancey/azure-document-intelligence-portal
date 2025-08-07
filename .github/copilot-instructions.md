# Document Intelligence Portal - Copilot Instructions

A .NET 8 Web API application that integrates with Azure Document Intelligence and Azure Storage services to analyze documents stored in Azure Storage. Built with Azure Managed Identity for secure authentication and provides a modern web interface for document analysis.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap, Build, and Test
- `dotnet restore --locked-mode` - Restore NuGet packages using lock file (~400ms)
- `dotnet build --no-restore` - Build the solution (~11 seconds) 
- `dotnet test --verbosity minimal` - Run all tests (~6m 45s). NEVER CANCEL - includes integration tests with Azure service timeouts. Set timeout to 8+ minutes.
- `dotnet format --verify-no-changes` - Verify code formatting is correct
- `dotnet format` - Fix code formatting issues

### Running the Application
- ALWAYS run the bootstrapping steps first (restore, build)
- Development: `dotnet run --project src/DocumentIntelligencePortal`
- Application runs on: `http://localhost:5162` (port may vary)
- Health check: `GET http://localhost:5162/health` → Returns "Healthy"
- Swagger UI: `http://localhost:5162/swagger` - API documentation
- Main UI: `http://localhost:5162/` - Web interface for document analysis

### Testing Options
- **All tests**: `dotnet test` (6+ minutes - includes integration tests)
- **Fast unit tests**: `dotnet test --filter "FullyQualifiedName!~Integration"` (~30 seconds)
- **With coverage**: `dotnet test --collect:"XPlat Code Coverage"`
- **Test runner script**: `tests/DocumentIntelligencePortal.Tests/run-tests.sh --help`

## Validation Scenarios

ALWAYS manually validate changes by running through these complete user scenarios after making modifications:

### Scenario 1: Basic Application Health
1. Build and start the application: `dotnet run --project src/DocumentIntelligencePortal`
2. Verify health endpoint: `curl http://localhost:5162/health` → Should return "Healthy"
3. Test API models endpoint: `curl http://localhost:5162/api/documentanalysis/models` → Should return JSON array of model names
4. Access web UI: Open `http://localhost:5162/` in browser → Should load Document Intelligence Portal interface

### Scenario 2: Web Interface Functionality
1. Start the application and open `http://localhost:5162/`
2. Click "Refresh Containers" button → Should attempt to load containers (will fail without Azure config - expected)
3. Navigate to `/swagger` → Should display OpenAPI documentation
4. Verify all API endpoints are documented and accessible via Swagger UI

### Scenario 3: Code Quality and CI Validation
1. Check code formatting: `dotnet format --verify-no-changes` → Should pass or show specific formatting issues
2. Run tests: `dotnet test --verbosity minimal` → All 137 tests should pass
3. Simulate CI build: `dotnet restore --locked-mode && dotnet build --no-restore`

## Authentication and Azure Services

### Local Development Configuration
The application uses different authentication modes via `Azure:AuthenticationMode` setting:

- **DevelopmentStorage**: For local testing with Azurite (Azure Storage Emulator)
  - Set `"Azure:AuthenticationMode": "DevelopmentStorage"` in `appsettings.Development.json`
  - Use connection string: `"UseDevelopmentStorage=true"`
  - Storage account name: `"devstoreaccount1"`

- **ManagedIdentity**: For production Azure deployments
- **ServicePrincipal**: For CI/CD scenarios  
- **DefaultCredential**: Fallback using Azure credential chain

### Testing with Azurite (Optional)
```bash
# Install and start Azurite for local Azure Storage emulation
npm install -g azurite
azurite --silent --location ./azurite
```

## Project Structure

### Solution Layout
```
/
├── src/DocumentIntelligencePortal/          # Main Web API project
│   ├── Controllers/                         # API controllers (Storage, DocumentAnalysis)
│   ├── Services/                           # Business logic services
│   ├── Models/                             # DTOs and data models
│   ├── wwwroot/                            # Static web files (HTML, CSS, JS)
│   ├── Program.cs                          # Application entry point
│   └── appsettings.*.json                  # Configuration files
├── tests/DocumentIntelligencePortal.Tests/ # Test project
│   ├── Controllers/                        # Controller unit tests
│   ├── Services/                           # Service unit tests
│   ├── Integration/                        # Integration tests (slow)
│   └── run-tests.sh                        # Test runner script
├── infra/                                  # Bicep infrastructure templates
└── .github/workflows/                      # CI/CD pipelines
```

### Key Files
- **Solution file**: `document-inteligence-portal.sln` (note typo in filename)
- **Main project**: `src/DocumentIntelligencePortal/DocumentIntelligencePortal.csproj`
- **Test project**: `tests/DocumentIntelligencePortal.Tests/DocumentIntelligencePortal.Tests.csproj`
- **Dockerfile**: Multi-stage build with .NET 8 SDK/runtime
- **Azure deployment**: `azure.yaml` for Azure Developer CLI

## API Endpoints

### Storage Operations
- `GET /api/storage/containers` - List storage containers
- `GET /api/storage/containers/{container}/documents` - List documents in container

### Document Analysis (Recommended: Streaming API)
- `POST /api/documentanalysis/analyze/stream` - Analyze document by streaming from storage (no SAS required)
- `POST /api/documentanalysis/analyze/stream/{container}/{blob}` - Analyze by path (streaming)
- `GET /api/documentanalysis/models` - Get available analysis models
- `GET /api/documentanalysis/result/{operationId}` - Get analysis results

### Health and Documentation
- `GET /health` - Application health check
- `GET /swagger` - API documentation (Swagger UI)

## Common Tasks

### Building and Running
```bash
# Clean build from scratch
dotnet clean
dotnet restore --locked-mode
dotnet build --no-restore

# Run application (development mode)
cd src/DocumentIntelligencePortal
dotnet run

# Build Docker image
docker build -t document-intelligence-portal .
```

### Testing
```bash
# Run all tests (NEVER CANCEL - takes ~6m 45s)
dotnet test --verbosity minimal

# Run only unit tests (fast)
dotnet test --filter "FullyQualifiedName!~Integration"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Use test runner script with options
cd tests/DocumentIntelligencePortal.Tests
./run-tests.sh --unit           # Unit tests only
./run-tests.sh --coverage       # With coverage
./run-tests.sh --watch          # Watch mode
```

### Code Quality
```bash
# Check formatting
dotnet format --verify-no-changes

# Fix formatting issues
dotnet format

# Check for build warnings
dotnet build --verbosity normal
```

## Deployment

### Azure Developer CLI (Recommended)
```bash
# Deploy to Azure
azd init
azd up

# Environment management
azd env get-values
azd env set AZURE_LOCATION eastus
```

### Manual Azure Deployment
```bash
# Deploy infrastructure
az deployment group create \
  --resource-group rg-document-intelligence \
  --template-file infra/main.bicep

# Build and deploy application
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Common Issues
1. **Build Failures**: Ensure .NET 8 SDK is installed (`dotnet --version` should show 8.x.x)
2. **Test Timeouts**: Integration tests can take 20-30 seconds each due to Azure service timeouts - this is normal
3. **Format Errors**: Run `dotnet format` to fix whitespace and style issues
4. **Authentication Errors**: Check `appsettings.Development.json` for correct `AuthenticationMode` setting

### Application Not Starting
- Check port conflicts (default is dynamic, usually 5162)
- Verify configuration in `appsettings.Development.json`
- Check logs for Azure service connection issues (expected in development)

### Test Failures
- Some integration tests may fail without proper Azure configuration - this is expected
- Use `dotnet test --filter` to run specific test categories
- Check `tests/DocumentIntelligencePortal.Tests/README.md` for detailed test documentation

## CI/CD Integration

### GitHub Actions
The repository includes CI pipeline (`.github/workflows/ci.yml`) that:
- Builds with .NET 8
- Runs all tests with coverage collection
- Integrates with SonarCloud for code analysis
- Caches NuGet packages and SonarQube tools

### Required Environment Variables
- `SONAR_TOKEN`: For SonarCloud integration
- Azure credentials for deployment pipelines

## Performance Notes

### Build and Test Times
- **Restore**: ~400ms (with cache)
- **Build**: ~11 seconds
- **Unit tests**: ~30 seconds
- **All tests**: ~6m 45s (NEVER CANCEL - includes integration tests with timeouts)
- **Code formatting**: ~5 seconds

### Known Timing Considerations
- Integration tests connect to Azure services with 20-30 second timeouts each
- SonarCloud analysis adds ~2-3 minutes to CI builds
- Docker builds take ~3-5 minutes depending on cache

## Dependencies and Technologies

### Core Technologies
- **.NET 8**: Web API framework
- **Azure SDK**: Document Intelligence, Storage, Identity
- **Serilog**: Structured logging
- **Swashbuckle**: OpenAPI/Swagger documentation

### Testing Stack
- **xUnit**: Test framework  
- **FluentAssertions**: Assertion library
- **Moq**: Mocking framework
- **Testcontainers.Azurite**: Azure Storage emulation

### Frontend
- **Bootstrap 5**: CSS framework
- **Font Awesome**: Icons
- **Vanilla JavaScript**: Web interface functionality

Always run `dotnet format` and full test suite before submitting changes. The CI pipeline will fail if code formatting is incorrect or if tests don't pass.
