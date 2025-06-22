// Target subscription scope to allow resource group creation
targetScope = 'subscription'

// Deployment metadata - required for subscription-scoped deployments
metadata description = 'Document Intelligence Portal - Main deployment template'
metadata author = 'Document Intelligence Portal'

@description('The name of the resource group')
param resourceGroupName string

@description('The location for all resources')
param location string = 'uksouth'

@description('The name of the storage account')
param storageAccountName string

@description('The name of the Document Intelligence service')
@minLength(2)
@maxLength(30)
param documentIntelligenceAccountName string

@description('The SKU name for the Document Intelligence service')
@allowed(['F0', 'S0'])
param documentIntelligenceSkuName string = 'F0' // F0: Free tier (20 pages/month), S0: Standard tier (paid)

@description('The name of the App Service plan')
param appServicePlanName string = 'asp-document-intelligence-portal'

@description('The name of the web app')
param webAppName string

@description('The environment name (used for tagging)')
param environmentName string = 'dev'

@description('A unique token to append to resource names for uniqueness')
param resourceToken string = uniqueString(subscription().id, environmentName)

// Create the resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: {
    'azd-env-name': environmentName
    purpose: 'document-intelligence-portal'
  }
}

// Module to deploy all resources within the resource group
module resourcesModule 'resources.bicep' = {
  scope: resourceGroup
  name: 'resources'
  params: {
    location: location
    storageAccountName: storageAccountName
    documentIntelligenceAccountName: documentIntelligenceAccountName
    documentIntelligenceSkuName: documentIntelligenceSkuName
    appServicePlanName: appServicePlanName
    webAppName: webAppName
    environmentName: environmentName
    resourceToken: resourceToken
  }
}

// Outputs
output AZURE_STORAGE_ACCOUNT_NAME string = resourcesModule.outputs.storageAccountName
output AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT string = resourcesModule.outputs.documentIntelligenceEndpoint
output WEB_APP_URL string = resourcesModule.outputs.webAppUrl
output RESOURCE_GROUP_NAME string = resourceGroup.name
output RESOURCE_GROUP_ID string = resourceGroup.id
output USER_ASSIGNED_IDENTITY_CLIENT_ID string = resourcesModule.outputs.userAssignedIdentityClientId
output VIRTUAL_NETWORK_ID string = resourcesModule.outputs.virtualNetworkId
