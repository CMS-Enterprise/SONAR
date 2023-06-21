
const rootOnlyConfig = require('../fixtures/testRootConfig')
const environmentName = 'foo'
const tenantName = 'bar'
const configRequestUrl = 'localhost:8081/api/v2/config/' + environmentName + '/tenants/' + tenantName
const headerWithApiKey = {
  'Accept' : 'application/json',
  'ApiKey' : 'test+api+key+do+not+use+in+production+xxxxx='
}

describe('Create Tenant configuration with only root service', () => {
  it('Tenant configuration created', () => {
    /// send Tenant configuration
    cy.request({
      method : 'POST',
      url : configRequestUrl,
      headers : headerWithApiKey,
      body: {
        'services' : rootOnlyConfig.services,
        'rootServices' : rootOnlyConfig.rootServices
      }
    }).then((res) => {
      expect(res.status).to.eq(201)
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

    cy.contains('Status History')

    cy.contains('Health Checks')
    cy.get('[data-test="health-check-name"]')
      .contains(rootOnlyConfig.services[0].healthChecks[0].name)

    cy.contains('Services').should('not.exist')

    /// cleanup - delete created tenant configuration, navigate back home
    cy.request({
      method: 'DELETE',
      url: configRequestUrl,
      headers : headerWithApiKey
    }).then((res) => {
      expect(res.status).to.eq(204)
      cy.get('[data-test="navbar-home-link"]').click()
      cy.location('pathname').should('eq', '/')
    })
  })
})
