namespace DocumentIntelligencePortal.Tests.Services;

/// <summary>
/// Integration tests for DocumentIntelligenceService
/// These tests verify the actual integration with Azure Document Intelligence service
/// </summary>
public class DocumentIntelligenceServiceIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public DocumentIntelligenceServiceIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires actual Azure Document Intelligence service")]
    public async Task AnalyzeDocumentAsync_WithRealService_ShouldAnalyzeDocument()
    {
        // This test would be enabled in integration test environments
        // with actual Azure services configured
        
        // Arrange
        // var service = CreateRealDocumentIntelligenceService();
        // var request = TestDataFactory.CreateAnalyzeDocumentRequest();

        // Act
        // var result = await service.AnalyzeDocumentAsync(request);

        // Assert
        // result.Should().NotBeNull();
        // result.Success.Should().BeTrue();
        // result.Result.Should().NotBeNull();
    }

    [Fact(Skip = "Requires actual Azure Document Intelligence service")]
    public async Task GetAvailableModelsAsync_WithRealService_ShouldReturnModels()
    {
        // This test would verify integration with actual Azure Document Intelligence
        // to ensure available models are correctly retrieved
        
        // Arrange
        // var service = CreateRealDocumentIntelligenceService();

        // Act
        // var models = await service.GetAvailableModelsAsync();

        // Assert
        // models.Should().NotBeNull();
        // models.Should().NotBeEmpty();
        // models.Should().Contain("prebuilt-document");
    }
}
