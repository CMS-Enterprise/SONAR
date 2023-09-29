import { useParams } from 'react-router-dom';

const Environment = () => {
  const params = useParams();
  const environmentName = params.environment as string;
  return <h1>To do BATAPI-433  {environmentName}</h1>
}
export default Environment;
