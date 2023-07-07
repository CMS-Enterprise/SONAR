import { Api as SonarApi } from 'api/sonar-api.generated';

export function createSonarClient() {
  return new SonarApi({
    /* eslint-disable  @typescript-eslint/no-explicit-any */
    baseUrl: process.env.REACT_APP_API_URL || (window as any).API_URL
  });
}
