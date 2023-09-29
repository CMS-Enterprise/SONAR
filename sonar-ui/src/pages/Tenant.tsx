import { useParams } from 'react-router-dom';

const Tenant = () => {
  const params = useParams();
  const environmentName = params.environment as string;
  const tenantName = params.tenant as string;
  return <h1>To do BATAPI-433  {environmentName} {tenantName}</h1>
}
export default Tenant;
