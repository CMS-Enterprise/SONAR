# SONAR UI

## Run locally with sonar-api

Have sonar-api running locally on `localhost:8081`.
**Ensure that there is recent test data in your local Prometheus instance.**

## Available Scripts

In the project directory, you can run:

### Before you start
Install any dependencies using `npm install` \
Auto generate the api `npm run generate-api-client` while having the sonar-api running.

### `npm start`

Runs the app in the development mode.\
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

The page will reload if you make edits.\
You will also see any lint errors in the console.

If you want to test the SONAR UI against an alternative API URL (such as our dev environment), you can use the `REACT_APP_API_URL` environment variable:

```
REACT_APP_API_URL=https://sonar-dev.batcave-ispg-nonprod.internal.cms.gov npm start
```

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
generated sources can be found in the [`src/api`](./src/api) folder. Will require the sonar-api to be built.

### `npm run cypress`

Opens the Cypress app, whereby you will see the Launchpad. Choose the "E2E Testing" option.
Before running any e2e test, ensure that:
1. sonar-api is running locally
2. you have executed `npm start` so that the UI is viewable at [http://localhost:3000](http://localhost:3000)

### `npm run e2e`

Runs End-to-end tests using Cypress in headless mode. Similar to running e2e tests in the Cypress app, you will need to
have `sonar-api` and the `sonar-ui` already running and listening on ports 8081 and 3000 respectively.

## Regenerate `node_modules` and `package-lock.json`

Delete the `node_modules` directory and the `package-lock.json` file, then run `npm i` to regenerate both.

## Custom URL within Docker
The URL in which the UI determines the API endpoint can be configured within the public/config/settings.js. \
During deployment the settings.js will be configured based on the environment in use.

## CSS/Style Standards and Best Practices
### Emotion Guidelines
- Emotion is a CSS/JS library that allows for type-safe style composition.
- Use only [object styles](https://emotion.sh/docs/object-styles) (as opposed to string styles) when styling via the `css` prop.
  - ex: ```{
    backgroundColor: 'hotpink',
    '&:hover': {
    color: 'lightgreen' }}```
- The `css` prop is type safe, ```css({ someUnknownCssProperty: blue })``` won't compile.

  #### Global Styles
  - Global styles, such as colors and fonts, should be kept in the [theme](https://emotion.sh/docs/theming) interface.
    - To update the theme, navigate to `emotion.d.ts` and add the new properties needed.
    - To update the values of the new properties, navigate to `themes.ts`

  #### Component-Specific Style Modules
  - A style module file can be created if a style object is applicable to a single component.
  - AVOID anonymous inline styles. ex: `style={{ margin: 10 }}`
  - If style module can be applied to multiple related components, the module file can be placed one level
    higher in the file tree.
  - Example of style module export for `SomeComponent`:
    - ```export function someComponentStyle(theme: Theme) { return css({ color: white, backgroundColor: black }}```

    ##### Style Module File Naming Convention
    - Component: `Foo.tsx`
    - Style Module File: `Foo.Style.ts`
      - Note that the style module file is a `.ts` file, not `.tsx` since there is no `jsx` needed.

### When To Use CMS Design Classes
- [CMS Design](https://design.cms.gov/v/6.0.1/components/overview/?theme=core) classes should primarily be used for layout and typography.

## About Create React App

This project was bootstrapped with [Create React App](https://github.com/facebook/create-react-app).
You can learn more in
the [Create React App documentation](https://facebook.github.io/create-react-app/docs/getting-started).
