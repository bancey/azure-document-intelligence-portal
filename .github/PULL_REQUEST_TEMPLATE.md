## Description

<!-- Provide a clear and concise description of your changes -->

## Related Issue

<!-- Link to the issue this PR addresses -->
Fixes #(issue number)

## Type of Change

<!-- Mark the relevant option with an "x" -->

- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📝 Documentation update
- [ ] 🎨 Code style update (formatting, renaming)
- [ ] ♻️ Code refactoring (no functional changes)
- [ ] ⚡ Performance improvement
- [ ] ✅ Test update
- [ ] 🔧 Configuration change
- [ ] 🏗️ Infrastructure/Build change

## Changes Made

<!-- Describe the changes you made in detail -->

- Change 1
- Change 2
- Change 3

## Testing Performed

<!-- Describe the tests you ran to verify your changes -->

### Unit Tests

- [ ] All existing unit tests pass
- [ ] New unit tests added (if applicable)
- [ ] Test coverage maintained or improved

### Integration Tests

- [ ] All existing integration tests pass
- [ ] New integration tests added (if applicable)

### Manual Testing

<!-- Describe manual testing performed -->

- [ ] Tested locally with `dotnet run`
- [ ] Tested with Azure services
- [ ] Tested edge cases
- [ ] Tested error handling

**Test Environment:**
- .NET Version: 
- Azure Region: 
- Browser (if UI change): 

### Test Results

<!-- Share test results, screenshots, or logs -->

```
Paste test output or logs here
```

## Screenshots

<!-- If applicable, add screenshots to demonstrate changes -->

### Before
<!-- Screenshot of the behavior before your changes -->

### After
<!-- Screenshot of the behavior after your changes -->

## Breaking Changes

<!-- List any breaking changes and migration steps -->

- [ ] This PR introduces breaking changes
- [ ] Migration guide provided
- [ ] CHANGELOG.md updated

**Breaking Changes Description:**
<!-- Describe breaking changes and how to migrate -->

## Checklist

<!-- Verify you have completed these items -->

### Code Quality

- [ ] My code follows the project's coding standards
- [ ] I have performed a self-review of my code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] My changes generate no new warnings
- [ ] I have removed any console.log or debug statements

### Documentation

- [ ] I have updated the README.md (if needed)
- [ ] I have updated CONTRIBUTING.md (if needed)
- [ ] I have updated other relevant documentation
- [ ] I have added/updated code comments
- [ ] I have updated API documentation/Swagger annotations (if applicable)

### Testing

- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
- [ ] I have verified integration tests pass
- [ ] I have tested error handling

### Security

- [ ] I have not introduced any security vulnerabilities
- [ ] I have not hardcoded any credentials or secrets
- [ ] I have validated and sanitized user inputs (if applicable)
- [ ] I have used Azure Managed Identity for authentication (if applicable)

### Azure Services

<!-- Mark the Azure services affected by your changes -->

- [ ] Azure Storage
- [ ] Azure Document Intelligence
- [ ] Azure Managed Identity
- [ ] Azure App Service
- [ ] Infrastructure (Bicep templates)
- [ ] None

**Azure Configuration Changes:**
<!-- Describe any changes to Azure service configuration -->

## Performance Impact

<!-- Describe any performance implications -->

- [ ] No performance impact
- [ ] Performance improved
- [ ] Potential performance impact (described below)

**Performance Notes:**
<!-- Describe performance testing and results -->

## Dependencies

<!-- List any new dependencies or dependency updates -->

- [ ] No new dependencies
- [ ] New NuGet packages added (listed below)
- [ ] Existing packages updated (listed below)

**New/Updated Dependencies:**
- Package 1: version
- Package 2: version

## Deployment Notes

<!-- Any special instructions for deployment? -->

- [ ] No special deployment steps required
- [ ] Requires Azure resource updates
- [ ] Requires environment variable changes
- [ ] Requires database migrations

**Special Instructions:**
<!-- Provide deployment instructions -->

## Rollback Plan

<!-- How can this change be rolled back if needed? -->

## Additional Context

<!-- Add any other context about the PR here -->

## Reviewer Notes

<!-- Notes for code reviewers -->

**Areas to Focus On:**
- 
- 

**Questions for Reviewers:**
- 
- 

---

## Post-Merge Checklist

<!-- For maintainers to complete after merging -->

- [ ] Update CHANGELOG.md
- [ ] Create/update release notes
- [ ] Update version numbers
- [ ] Tag release (if applicable)
- [ ] Notify stakeholders
- [ ] Update documentation site (if applicable)
