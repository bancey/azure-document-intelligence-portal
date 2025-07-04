using Azure.Core;
using Azure.Identity;

namespace DocumentIntelligencePortal.Services;

/// <summary>
/// Provides Azure credentials based on environment configuration.
/// Supports different authentication modes for production, development, and testing.
/// </summary>
public class AzureCredentialProvider : IAzureCredentialProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureCredentialProvider> _logger;

    public AzureCredentialProvider(IConfiguration configuration, ILogger<AzureCredentialProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets the appropriate TokenCredential for the current environment
    /// </summary>
    public TokenCredential GetCredential()
    {
        var authMode = _configuration["Azure:AuthenticationMode"] ?? "DefaultCredential";
        
        return authMode.ToLowerInvariant() switch
        {
            "developmentstorage" => GetDevelopmentCredential(),
            "managedidentity" => GetManagedIdentityCredential(),
            "serviceprincipal" => GetServicePrincipalCredential(),
            "defaultcredential" => GetDefaultCredential(),
            _ => GetDefaultCredential()
        };
    }

    private TokenCredential GetDevelopmentCredential()
    {
        _logger.LogInformation("Using development authentication mode - this should only be used for local development with Azurite");
        
        // For development/testing, we can use a mock credential that doesn't actually authenticate
        // This is useful when using Azurite (Azure Storage Emulator) or other local services
        return new MockTokenCredential();
    }

    private TokenCredential GetManagedIdentityCredential()
    {
        _logger.LogInformation("Using managed identity authentication");
        
        var clientId = _configuration["AZURE_CLIENT_ID"];
        if (!string.IsNullOrEmpty(clientId))
        {
            return new ManagedIdentityCredential(clientId);
        }
        
        return new ManagedIdentityCredential();
    }

    private TokenCredential GetServicePrincipalCredential()
    {
        _logger.LogInformation("Using service principal authentication");
        
        var tenantId = _configuration["AZURE_TENANT_ID"];
        var clientId = _configuration["AZURE_CLIENT_ID"];
        var clientSecret = _configuration["AZURE_CLIENT_SECRET"];
        
        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Service principal authentication requires AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET to be configured.");
        }
        
        return new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    private TokenCredential GetDefaultCredential()
    {
        _logger.LogInformation("Using default Azure credential chain");
        
        var options = new DefaultAzureCredentialOptions
        {
            // Exclude some credential types that might cause issues in testing/development
            ExcludeVisualStudioCredential = true,
            ExcludeAzurePowerShellCredential = true,
            ExcludeInteractiveBrowserCredential = true
        };
        
        return new DefaultAzureCredential(options);
    }
}

/// <summary>
/// Mock credential for development/testing scenarios where no actual authentication is needed
/// </summary>
internal class MockTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
    }
}