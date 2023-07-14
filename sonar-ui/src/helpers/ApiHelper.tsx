import { Api as SonarApi } from 'api/sonar-api.generated';
import { apiUrl as baseUrl } from 'config';

export function createSonarClient() {
  return new SonarApi({ baseUrl });
}
