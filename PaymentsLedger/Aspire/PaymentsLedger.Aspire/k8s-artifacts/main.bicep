targetScope = 'subscription'

param resourceGroupName string

param location string

param principalId string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
}

module messaging 'messaging/messaging.bicep' = {
  name: 'messaging'
  scope: rg
  params: {
    location: location
  }
}

module messaging_roles 'messaging-roles/messaging-roles.bicep' = {
  name: 'messaging-roles'
  scope: rg
  params: {
    location: location
    messaging_outputs_name: messaging.outputs.name
    principalType: ''
    principalId: principalId
  }
}