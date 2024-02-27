ARG NODE_VERSION=20

FROM node:${NODE_VERSION}-alpine

ENV NODE_ENV production

WORKDIR /usr/src/app

COPY test/alerting-test-app/package.json .
COPY test/alerting-test-app/package-lock.json .
RUN npm ci
COPY test/alerting-test-app/app.js .

USER node

COPY . .

EXPOSE 8083

CMD node app.js
