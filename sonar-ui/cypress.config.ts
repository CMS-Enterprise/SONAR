import { defineConfig } from "cypress";

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      // implement node event listeners here
    },
    // remove this if helper functions are added to cypress/support/e2e.ts
    supportFile: false,
  },
});
