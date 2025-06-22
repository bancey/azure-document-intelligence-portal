<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Document Intelligence Portal - Copilot Instructions

This is a .NET 8 Web API project that integrates with Azure Document Intelligence and Azure Storage services.

## Project Guidelines

### Security & Authentication
- Always use Azure Managed Identity for authentication when possible
- Never hardcode credentials or connection strings
- Use Azure Key Vault for sensitive configuration
- Implement proper RBAC for Azure resources

### Code Structure
- Follow clean architecture principles
- Use dependency injection for Azure service clients
- Implement proper error handling and logging
- Use async/await patterns for Azure service calls

### Azure Services
- **Azure Document Intelligence**: For document analysis and extraction
- **Azure Storage**: For blob storage and file management
- **Azure Identity**: For managed identity authentication

### Best Practices
- Implement retry policies for Azure service calls
- Use proper HTTP status codes in API responses
- Follow RESTful API conventions
- Include comprehensive error handling
- Add proper logging and monitoring
