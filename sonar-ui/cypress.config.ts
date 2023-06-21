import { defineConfig } from "cypress";
import { Client } from "pg";

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      // implement node event listeners here
      on("task", {
        async connectDB(query){
          const client = new Client({
            user: "root",
            password: "password",
            host: "localhost",
            database: "sonar",
            ssl: false,
            port: 5432
          })
          await client.connect()
          const res = await client.query(query)
          await client.end()
          return res.rows;
        }
      })
    },
  },
});
