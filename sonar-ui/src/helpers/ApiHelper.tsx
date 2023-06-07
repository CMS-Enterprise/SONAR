import { Api as SonarApi } from 'api/sonar-api.generated';

export function createSonarClient() {
  return new SonarApi({
    baseUrl: (window as any).API_URL
  });
}
