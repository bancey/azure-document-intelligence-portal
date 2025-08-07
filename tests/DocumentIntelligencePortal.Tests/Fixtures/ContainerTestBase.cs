using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Azurite;
using DotNet.Testcontainers.Builders;

namespace DocumentIntelligencePortal.Tests.Fixtures;

/// <summary>
/// Base class for tests that require real container services (like Azurite)
/// This handles the container lifecycle and provides configured services
/// </summary>
public abstract class ContainerTestBase : IAsyncLifetime, IDisposable
{
    protected AzuriteContainer AzuriteContainer { get; private set; } = null!;
    protected string ConnectionString { get; private set; } = string.Empty;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        // Start Azurite container with faster timeouts
        AzuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();

        await AzuriteContainer.StartAsync();
        ConnectionString = AzuriteContainer.GetConnectionString();

        // Set up service provider with container-based configuration
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add any additional services
        ConfigureServices(services, configuration);
        
        ServiceProvider = services.BuildServiceProvider();
    }

    protected virtual IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:AuthenticationMode"] = "DevelopmentStorage",
                ["Azure:StorageAccountName"] = "devstoreaccount1",
                ["Azure:StorageRetryOptions:MaxRetries"] = "2",
                ["Azure:StorageRetryOptions:DelayMs"] = "100",
                ["Azure:StorageRetryOptions:MaxDelayMs"] = "1000",
                ["ConnectionStrings:AzureStorage"] = ConnectionString,
                ["Logging:LogLevel:Default"] = "Warning"
            })
            .Build();
    }

    protected virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Override in derived classes to add specific services
    }

    protected async Task<BlobContainerClient> CreateTestContainerAsync(string containerName = "test-container")
    {
        var blobServiceClient = new BlobServiceClient(ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();
        return containerClient;
    }

    protected async Task CreateTestBlobAsync(string containerName, string blobName, string content = "Test content")
    {
        var containerClient = await CreateTestContainerAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public virtual async Task DisposeAsync()
    {
        if (AzuriteContainer != null)
        {
            await AzuriteContainer.DisposeAsync();
        }
        
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public virtual void Dispose()
    {
        // IAsyncLifetime handles cleanup
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Fast-failing test base for tests that should use mocks instead of containers
/// This base class provides quick-failing behavior for misconfigured tests
/// </summary>
public abstract class MockedTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IConfiguration Configuration { get; private set; }

    protected MockedTestBase()
    {
        var services = new ServiceCollection();
        
        // Configure fast-failing test configuration
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:AuthenticationMode"] = "Mock",
                ["Azure:StorageAccountName"] = "mockedstorageaccount",
                ["Azure:StorageRetryOptions:MaxRetries"] = "0", // No retries for mocked tests
                ["Azure:StorageRetryOptions:DelayMs"] = "1",
                ["Azure:StorageRetryOptions:MaxDelayMs"] = "1",
                ["ConnectionStrings:AzureStorage"] = "UseMockStorage=true",
                ["Logging:LogLevel:Default"] = "Warning"
            })
            .Build();

        services.AddSingleton(Configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes to add mocked services
    }

    protected Mock<ILogger<T>> CreateMockLogger<T>() => new Mock<ILogger<T>>();

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}