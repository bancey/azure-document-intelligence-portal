using Azure.Core;

namespace DocumentIntelligencePortal.Services;

/// <summary>
/// Provides Azure credentials for authentication based on environment configuration
/// </summary>
public interface IAzureCredentialProvider
{
    /// <summary>
    /// Gets the appropriate TokenCredential for the current environment
    /// </summary>
    /// <returns>TokenCredential instance</returns>
    TokenCredential GetCredential();
}