
const rootOnlyConfig = require('../fixtures/testRootConfig')
const environmentName = 'foo'
const tenantName = 'bar'
const statusCodeCreated = 201

describe('Create Tenant configuration with only root service', () => {
   /// TODO - reset database

  it('Tenant configuration created', () => {
    /// send Tenant configuration
    cy.request({
      method : 'POST',
      url: 'localhost:8081/api/v2/config/' + environmentName + '/tenants/' + tenantName,
      headers: {
        'Accept' : 'application/json',
        'ApiKey' : 'test+api+key+do+not+use+in+production+xxxxx='
      },
      body: {
        "services" : rootOnlyConfig.services,
        "rootServices" : rootOnlyConfig.rootServices
      }
    }).then((res) => {
      expect(res.status).to.eq(statusCodeCreated)
    })

    /// Environments view
    cy.visit('localhost:3000')
    cy.get('[data-test="env-view-accordion"]').contains(environmentName)
    cy.get('[data-test="env-view-tenant"]').contains(tenantName)
    cy.get('[data-test="env-view-tenant"]').contains(rootOnlyConfig.services[0].displayName)
      .click()

    /// Service view
    const rootServiceName = rootOnlyConfig.services[0].name
    const rootServicePath = '/' + environmentName +
                            '/tenants/' + tenantName +
                            '/services/' + rootServiceName

    cy.location('pathname').should('eq', rootServicePath)
    cy.get('[data-test="breadcrumbs"]').contains(environmentName)
    cy.get('[data-test="breadcrumbs"]').contains(tenantName)
    cy.get('[data-test="breadcrumbs"]').contains(rootServiceName)

    cy.contains("Status History")

    cy.contains("Health Checks")
    cy.get('[data-test="health-check-name"]')
      .contains(rootOnlyConfig.services[0].healthChecks[0].name)
  })
})
