# DocumentIntelligencePortal Tests

This directory contains comprehensive tests for the Document Intelligence Portal application.

## Test Structure

### Unit Tests
- **Controllers/**: Tests for MVC controllers (HTTP handling, validation, routing)
- **Services/**: Tests for business logic services (Azure integration, error handling)
- **Models/**: Tests for data models and DTOs (validation, serialization)
- **Fixtures/**: Shared test utilities and data factories

### Integration Tests
- **Integration/**: End-to-end tests using TestServer
- Tests complete request pipeline including middleware and routing
- Can be configured to use real Azure services or mocks

## Test Categories

### 1. Unit Tests
- **Fast**: Run in milliseconds
- **Isolated**: Mock all external dependencies
- **Focused**: Test single units of functionality
- **Reliable**: Deterministic results

### 2. Integration Tests
- **Realistic**: Test complete workflows
- **Service Integration**: Can test with real Azure services
- **Pipeline Testing**: Verify entire request/response cycle
- **Configuration Testing**: Test different environment configurations

### 3. Performance Tests (Planned)
- Load testing with Azure Load Testing
- Concurrent request handling
- Memory and CPU usage validation
- Response time requirements

### 4. Security Tests
- Input validation and sanitization
- Error message security (no internal details exposed)
- Authentication and authorization (when implemented)
- CORS handling

## Running Tests

### Prerequisites
```bash
# Install .NET 8 SDK
dotnet --version  # Should be 8.0 or higher
```

### Run All Tests
```bash
# From the project root
dotnet test

# From the test project directory
cd Tests
dotnet test
```

### Run Specific Test Categories
```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests only
dotnet test --filter "Category=Integration"

# Exclude integration tests that require Azure services
dotnet test --filter "Category!=RequiresAzure"
```

### Run Tests with Coverage
```bash
# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage"

# Install ReportGenerator for HTML reports
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML coverage report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:TestResults/CoverageReport -reporttypes:Html
```

### Run Tests in Watch Mode
```bash
# Automatically re-run tests when files change
dotnet watch test
```

## Test Configuration

### Environment Variables for Integration Tests
```bash
# For tests that require actual Azure services
export AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT="https://your-doc-intel.cognitiveservices.azure.com/"
export AZURE_STORAGE_ACCOUNT_NAME="yourstorageaccount"

# For end-to-end tests
export E2E_TEST_BASE_URL="https://your-app.azurewebsites.net"
export E2E_TEST_DOCUMENT_URI="https://test-storage.blob.core.windows.net/test-documents/sample.pdf"
```

### Test Settings
- **appsettings.Test.json**: Test-specific configuration
- **TestFixture.cs**: Shared test setup and utilities
- **TestDataFactory.cs**: Factory methods for creating test data

## Azure Services Testing

### Using Azurite (Azure Storage Emulator)
```bash
# Install Azurite
npm install -g azurite

# Start Azurite
azurite --silent --location ./azurite --debug ./azurite/debug.log

# Tests can use connection string: "UseDevelopmentStorage=true"
```

### Using Real Azure Services
1. Create test Azure resources (separate from production)
2. Configure authentication (Managed Identity, Service Principal, or User credentials)
3. Set environment variables or configuration
4. Run integration tests: `dotnet test --filter "Category=Integration"`

## Test Patterns and Best Practices

### AAA Pattern (Arrange, Act, Assert)
```csharp
[Fact]
public async Task Method_Scenario_ExpectedResult()
{
    // Arrange
    var service = CreateService();
    var input = CreateTestInput();

    // Act
    var result = await service.DoSomething(input);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

### Mocking Azure Services
```csharp
// Mock Azure SDK clients
var mockBlobClient = new Mock<BlobServiceClient>();
mockBlobClient.Setup(x => x.GetBlobContainersAsync())
    .Returns(CreateAsyncPageable(containerItems));

// Use dependency injection to inject mocks
services.AddSingleton(mockBlobClient.Object);
```

### Test Data Factories
```csharp
// Use factories for consistent test data
var request = TestDataFactory.CreateAnalyzeDocumentRequest();
var document = TestDataFactory.CreateStorageDocument();
```

## Continuous Integration

### Azure Pipelines / GitHub Actions
```yaml
- name: Run Unit Tests
  run: dotnet test --filter "Category=Unit" --logger trx --results-directory TestResults

- name: Run Integration Tests
  run: dotnet test --filter "Category=Integration" --logger trx --results-directory TestResults
  env:
    AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT: ${{ secrets.TEST_DOC_INTEL_ENDPOINT }}
    AZURE_STORAGE_ACCOUNT_NAME: ${{ secrets.TEST_STORAGE_ACCOUNT }}
```

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   - Ensure Azure credentials are configured
   - Check Managed Identity permissions
   - Verify service principal roles

2. **Timeout Issues**
   - Increase test timeouts for Azure service calls
   - Use mocks for unit tests to avoid Azure dependencies

3. **Rate Limiting**
   - Use different Azure resources for different test runs
   - Implement retry policies in tests
   - Consider running integration tests serially

### Test Debugging
```bash
# Run tests with verbose output
dotnet test --verbosity detailed

# Debug specific test
dotnet test --filter "TestMethodName" --logger "console;verbosity=detailed"
```

## Coverage Goals

- **Unit Tests**: 90%+ code coverage
- **Integration Tests**: Cover all critical user journeys
- **API Tests**: All endpoints and error scenarios
- **Security Tests**: Input validation and error handling

## Adding New Tests

1. Follow naming conventions: `ClassName_Method_Scenario_ExpectedResult`
2. Use appropriate test attributes: `[Fact]`, `[Theory]`, `[Skip]`
3. Add to appropriate category (Unit/Integration/Performance/Security)
4. Include both positive and negative test cases
5. Mock external dependencies for unit tests
6. Use realistic data for integration tests

## Performance Testing

For load testing, consider using:
- Azure Load Testing service
- NBomber for .NET load testing
- Custom concurrent request tests
- Memory and performance profiling

Example load test configuration would be added to a separate `LoadTests` directory with appropriate tooling.
