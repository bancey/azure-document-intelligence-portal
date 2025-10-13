# Contributing to Document Intelligence Portal

Thank you for your interest in contributing to the Document Intelligence Portal! This document provides guidelines and instructions for contributing to this project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Pull Request Process](#pull-request-process)
- [Reporting Issues](#reporting-issues)
- [Questions and Support](#questions-and-support)

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## Getting Started

Before you begin:
- Ensure you have read the [README.md](README.md) for project overview
- Review the [DEPLOYMENT.md](DEPLOYMENT.md) for deployment guidelines
- Check existing [issues](https://github.com/bancey/azure-document-intelligence-portal/issues) and [pull requests](https://github.com/bancey/azure-document-intelligence-portal/pulls) to avoid duplicates

## Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Developer CLI (azd)](https://docs.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)
- Azure subscription with sufficient permissions
- Git for version control

### Local Development Setup

1. **Fork and Clone the Repository**
   ```bash
   git clone https://github.com/YOUR-USERNAME/azure-document-intelligence-portal.git
   cd azure-document-intelligence-portal
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure Application Settings**
   
   Update `src/DocumentIntelligencePortal/appsettings.Development.json`:
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

4. **Build the Project**
   ```bash
   dotnet build
   ```

5. **Run Tests**
   ```bash
   dotnet test
   ```

6. **Run the Application**
   ```bash
   cd src/DocumentIntelligencePortal
   dotnet run
   ```
   
   The application will be available at `https://localhost:7000` and `http://localhost:5000`.

## How to Contribute

### Types of Contributions

We welcome various types of contributions:

- **Bug Fixes**: Fix issues or bugs in the codebase
- **New Features**: Add new functionality to the application
- **Documentation**: Improve or add documentation
- **Tests**: Add or improve test coverage
- **Performance**: Optimize existing code
- **Security**: Address security vulnerabilities

### Workflow

1. **Create an Issue**: Before starting work, create an issue describing what you plan to do
2. **Fork the Repository**: Create your own fork of the project
3. **Create a Branch**: Create a feature branch from `main`
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. **Make Changes**: Implement your changes following our coding standards
5. **Test**: Ensure all tests pass and add new tests as needed
6. **Commit**: Commit your changes with clear, descriptive messages
7. **Push**: Push your changes to your fork
8. **Submit PR**: Open a pull request against the `main` branch

## Coding Standards

### General Guidelines

- Follow [.NET coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Write self-documenting code; add comments only when necessary
- Keep methods focused and concise (Single Responsibility Principle)
- Follow SOLID principles

### Architecture Principles

- Use **Dependency Injection** for Azure service clients
- Implement proper **error handling** and **logging**
- Use **async/await** patterns for Azure service calls
- Follow **clean architecture** principles
- Implement **retry policies** for Azure service calls

### Security Best Practices

- Always use **Azure Managed Identity** for authentication when possible
- Never hardcode credentials or connection strings
- Use Azure Key Vault for sensitive configuration
- Implement proper RBAC for Azure resources
- Validate and sanitize all user inputs

### Code Style

- Use 4 spaces for indentation (no tabs)
- Place opening braces on a new line
- Use `var` when the type is obvious
- Prefer explicit types for clarity when needed
- Maximum line length: 120 characters (recommended)

## Testing Guidelines

### Test Structure

Tests are organized in the `tests/DocumentIntelligencePortal.Tests` directory:

- **Unit Tests**: Fast, isolated tests with mocked dependencies
- **Integration Tests**: Tests that interact with Azure services
- **Security Tests**: Input validation and error handling tests

### Writing Tests

1. **Naming Convention**: `MethodName_Scenario_ExpectedResult`
   ```csharp
   [Fact]
   public void AnalyzeDocument_WithValidInput_ReturnsSuccessResult()
   {
       // Arrange
       // Act
       // Assert
   }
   ```

2. **Test Coverage**: Aim for 90%+ code coverage for new code
3. **Mock External Dependencies**: Use mocking frameworks for unit tests
4. **Integration Tests**: Use test attributes to categorize tests
5. **Test Data**: Use realistic test data

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific category
dotnet test --filter Category=Unit

# Run in watch mode
dotnet watch test
```

## Commit Message Guidelines

We follow conventional commit format for clear and structured commit history:

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Code style changes (formatting, missing semicolons, etc.)
- **refactor**: Code refactoring without changing functionality
- **perf**: Performance improvements
- **test**: Adding or updating tests
- **chore**: Changes to build process or auxiliary tools
- **ci**: Changes to CI configuration files and scripts

### Examples

```
feat(analysis): add support for table extraction

Implements table extraction feature using Document Intelligence API.
Includes error handling and retry logic.

Closes #123
```

```
fix(storage): resolve blob access permission issue

Fixed managed identity role assignment for storage access.

Fixes #456
```

### Best Practices

- Use the imperative mood ("add" not "added")
- Keep the subject line under 50 characters
- Capitalize the subject line
- Do not end the subject line with a period
- Separate subject from body with a blank line
- Wrap the body at 72 characters
- Reference issues and pull requests in the footer

## Pull Request Process

### Before Submitting

1. ✅ Ensure all tests pass locally
2. ✅ Update documentation if needed
3. ✅ Add tests for new functionality
4. ✅ Follow coding standards
5. ✅ Ensure CI/CD pipeline passes
6. ✅ Update CHANGELOG if applicable

### PR Description

Use the pull request template to provide:

- Clear description of changes
- Related issue(s)
- Type of change (bug fix, feature, etc.)
- Testing performed
- Screenshots (for UI changes)
- Breaking changes (if any)

### Review Process

1. **Automated Checks**: CI/CD pipeline must pass (build, tests, linting)
2. **Code Review**: At least one maintainer must approve
3. **Testing**: All tests must pass, including new tests
4. **Documentation**: Updated if needed
5. **Conflicts**: Resolve any merge conflicts

### After Approval

- Maintainers will merge your PR
- Your contribution will be included in the next release
- You'll be added to the contributors list

## Reporting Issues

### Bug Reports

Use the bug report template and include:

- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, Azure region)
- Error messages or logs
- Screenshots if applicable

### Feature Requests

Use the feature request template and include:

- Clear description of the feature
- Use case and motivation
- Proposed implementation (if applicable)
- Alternatives considered
- Additional context

### Security Vulnerabilities

**Do not create public issues for security vulnerabilities.** Instead:

- Contact the maintainers privately
- Provide detailed information about the vulnerability
- Allow time for a fix before public disclosure

## Questions and Support

- **Questions**: Open a [discussion](https://github.com/bancey/azure-document-intelligence-portal/discussions)
- **Issues**: Use [GitHub Issues](https://github.com/bancey/azure-document-intelligence-portal/issues)
- **Documentation**: Check [README.md](README.md), [DEPLOYMENT.md](DEPLOYMENT.md), and [AUTHENTICATION.md](AUTHENTICATION.md)

## Additional Resources

- [Azure Document Intelligence Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/form-recognizer/)
- [Azure Storage Documentation](https://docs.microsoft.com/en-us/azure/storage/)
- [Azure Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)

## License

By contributing, you agree that your contributions will be licensed under the same MIT License that covers this project.

---

Thank you for contributing to the Document Intelligence Portal! 🎉
