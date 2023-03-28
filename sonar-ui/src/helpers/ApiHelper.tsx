import { Api as SonarApi } from 'api/sonar-api.generated';

export function createSonarClient() {
  return new SonarApi({
    baseUrl: 'http://localhost:8081'
  });
}
