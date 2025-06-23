// This file contains all resources that will be deployed within the resource group
// Target scope: resourceGroup

@description('The location for all resources')
param location string

@description('The name of the storage account')
param storageAccountName string

@description('The name of the Document Intelligence service')
@minLength(2)
@maxLength(30)
param documentIntelligenceAccountName string

@description('The SKU name for the Document Intelligence service')
param documentIntelligenceSkuName string

@description('The name of the App Service plan')
param appServicePlanName string

@description('The name of the web app')
param webAppName string

@description('The environment name (used for tagging)')
param environmentName string

@description('A unique token to append to resource names for uniqueness')
param resourceToken string

// Virtual Network for private networking
resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-document-intelligence-portal-${resourceToken}'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: 'subnet-webapp'
        properties: {
          addressPrefix: '10.0.1.0/24'
          delegations: [
            {
              name: 'Microsoft.Web.serverFarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
          serviceEndpoints: []
        }
      }
      {
        name: 'subnet-privateendpoints'
        properties: {
          addressPrefix: '10.0.2.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'private-networking'
  }
}

// Storage Account with private networking
resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: '${storageAccountName}${resourceToken}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    supportsHttpsTrafficOnly: true
    networkAcls: {
      defaultAction: 'Deny'
      virtualNetworkRules: []
      bypass: 'AzureServices'
    }
    encryption: {
      services: {
        blob: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'document-storage'
  }
}

// Document Intelligence Service with private networking
resource documentIntelligenceAccount 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${documentIntelligenceAccountName}-${resourceToken}'
  location: location
  sku: {
    name: documentIntelligenceSkuName
  }
  kind: 'FormRecognizer'
  properties: {
    customSubDomainName: '${documentIntelligenceAccountName}-${resourceToken}'
    networkAcls: {
      defaultAction: 'Deny'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Disabled'
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'document-intelligence'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: '${appServicePlanName}-${resourceToken}'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: false
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'web-hosting'
  }
}

// User-assigned Managed Identity
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-document-intelligence-portal-${resourceToken}'
  location: location
  tags: {
    'azd-env-name': environmentName
    purpose: 'managed-identity'
  }
}

// Private Endpoint for Storage Account (Blob)
resource storagePrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-storage-${resourceToken}'
  location: location
  properties: {
    subnet: {
      id: virtualNetwork.properties.subnets[1].id // subnet-privateendpoints
    }
    privateLinkServiceConnections: [
      {
        name: 'storage-connection'
        properties: {
          privateLinkServiceId: storageAccount.id
          groupIds: ['blob']
        }
      }
    ]
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'storage-private-endpoint'
  }
}

// Private DNS Zone for Storage Account
resource storageDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.blob.${environment().suffixes.storage}'
  location: 'global'
  tags: {
    'azd-env-name': environmentName
    purpose: 'storage-dns'
  }
}

// Link Private DNS Zone to Virtual Network
resource storageDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: storageDnsZone
  name: 'storage-vnet-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetwork.id
    }
  }
}

// DNS Zone Group for Storage Private Endpoint
resource storageDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: storagePrivateEndpoint
  name: 'storage-dns-zone-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config1'
        properties: {
          privateDnsZoneId: storageDnsZone.id
        }
      }
    ]
  }
}

// Private Endpoint for Document Intelligence
resource documentIntelligencePrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-docint-${resourceToken}'
  location: location
  properties: {
    subnet: {
      id: virtualNetwork.properties.subnets[1].id // subnet-privateendpoints
    }
    privateLinkServiceConnections: [
      {
        name: 'docint-connection'
        properties: {
          privateLinkServiceId: documentIntelligenceAccount.id
          groupIds: ['account']
        }
      }
    ]
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'docint-private-endpoint'
  }
}

// Private DNS Zone for Document Intelligence
resource documentIntelligenceDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.cognitiveservices.azure.com'
  location: 'global'
  tags: {
    'azd-env-name': environmentName
    purpose: 'docint-dns'
  }
}

// Link Document Intelligence DNS Zone to Virtual Network
resource documentIntelligenceDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: documentIntelligenceDnsZone
  name: 'docint-vnet-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetwork.id
    }
  }
}

// DNS Zone Group for Document Intelligence Private Endpoint
resource documentIntelligenceDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: documentIntelligencePrivateEndpoint
  name: 'docint-dns-zone-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config1'
        properties: {
          privateDnsZoneId: documentIntelligenceDnsZone.id
        }
      }
    ]
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-document-intelligence-portal-${resourceToken}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'logging'
  }
}

// Application Insights for monitoring
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-document-intelligence-portal-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
  tags: {
    'azd-env-name': environmentName
    purpose: 'monitoring'
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${webAppName}-${resourceToken}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: virtualNetwork.properties.subnets[0].id // subnet-webapp
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      appSettings: [
        {
          name: 'Azure__StorageAccountName'
          value: storageAccount.name
        }
        {
          name: 'Azure__DocumentIntelligence__Endpoint'
          value: documentIntelligenceAccount.properties.endpoint
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: userAssignedIdentity.properties.clientId
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__ApplicationInsights'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'WEBSITE_VNET_ROUTE_ALL'
          value: '1'
        }
        {
          name: 'WEBSITE_DNS_SERVER'
          value: '168.63.129.16'
        }
      ]
      cors: {
        allowedOrigins: ['*']
        supportCredentials: false
      }
    }
  }
  tags: {
    'azd-env-name': environmentName
    'azd-service-name': 'web'
    purpose: 'web-application'
  }
}

// Role assignments for the managed identity
// Storage Blob Data Reader role
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, userAssignedIdentity.id, 'Storage Blob Data Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1') // Storage Blob Data Reader
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services User role for Document Intelligence
resource documentIntelligenceRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: documentIntelligenceAccount
  name: guid(documentIntelligenceAccount.id, userAssignedIdentity.id, 'Cognitive Services User')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908') // Cognitive Services User
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output documentIntelligenceEndpoint string = documentIntelligenceAccount.properties.endpoint
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output userAssignedIdentityClientId string = userAssignedIdentity.properties.clientId
output virtualNetworkId string = virtualNetwork.id
