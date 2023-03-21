# SONAR UI

## Run locally with sonar-api

Have sonar-api running locally on `localhost:8081`.
**Ensure that there is recent test data in your local Prometheus instance.**

## Available Scripts

In the project directory, you can run:

### `npm start`

Runs the app in the development mode.\
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

The page will reload if you make edits.\
You will also see any lint errors in the console.

### `npm test`

Launches the test runner in the interactive watch mode.\
See the section about [running tests](https://facebook.github.io/create-react-app/docs/running-tests) for more
information.

### `npm run lint`

Runs [eslint](https://eslint.org/) on the project. All eslint warnings and errors should be fixed before opening a merge request.

### `npm run build`

Builds the app for production to the `build` folder.\
It correctly bundles React in production mode and optimizes the build for the best performance.

The build is minified and the filenames include the hashes.\
Your app is ready to be deployed!

See the section about [deployment](https://facebook.github.io/create-react-app/docs/deployment) for more information.

### `npm run generate-api-client`

Regenerates the SONAR API client using [swagger-typescript-api](https://github.com/acacode/swagger-typescript-api). The
generated sources can be found in the [`src/api`](./src/api) folder.

## Regenerate `node_modules` and `package-lock.json`

Delete the `node_modules` directory and the `package-lock.json` file, then run `npm i` to regenerate both.

## About Create React App

This project was bootstrapped with [Create React App](https://github.com/facebook/create-react-app).
You can learn more in
the [Create React App documentation](https://facebook.github.io/create-react-app/docs/getting-started).
