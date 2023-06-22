// import for the global cy declaration
import {} from 'cypress'

describe('Test Navigation Bar', () => {

  beforeEach(() => {
    cy.visit('localhost:3000')
  })

  it('Navigation Bar components exist', () => {
    cy.get('[data-test="navbar-sonar-link"]').should("be.visible")
    cy.get('[data-test="navbar-home-link"]').should("be.visible")
    cy.get('[data-test="navbar-toggle-section"]').should("be.visible")
  })

  it('Navigation Bar links are valid', () => {
    cy.get('[data-test="navbar-sonar-link"]').click()
    cy.location('pathname').should('eq', '/')

    cy.get('[data-test="navbar-home-link"]').click()
    cy.location('pathname').should('eq', '/')
  })

  it('Clicking on light-dark toggle changes colors', () => {
    /// toggle default - light mode
    cy.get('[data-test="app-main"]')
      .should('have.css', 'background-color')
      .and('eq', 'rgb(242, 242, 242)') /// #F2F2F2

    cy.get('[data-test="app-main"]')
      .should('have.css', 'color')
      .and('eq', 'rgb(90, 90, 90)') /// #5A5A5A

    /// toggle - click
    cy.get('[data-test="navbar-toggle-section"]').click()

    /// toggle - dark mode
    cy.get('[data-test="app-main"]')
      .should('have.css', 'background-color')
      .and('eq', 'rgb(57, 62, 70)') /// #393E46

    cy.get('[data-test="app-main"]')
      .should('have.css', 'color')
      .and('eq', 'rgb(242, 242, 242)') /// #F2F2F2
  })
})
