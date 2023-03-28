import * as React from 'react';
import { useEffect, useState } from 'react';
import { Accordion } from '@cmsgov/design-system';

import { Environment, HealthStatus } from 'api/data-contracts';
import EnvironmentItem from 'components/Environment/EnvironmentItem';
// import { createSonarClient } from 'helpers/ApiHelper';
import { getHealthStatusIndicator } from 'helpers/ServiceHierarchyHelper';

const initialEnvs: Environment[] = [
  { id: 'test1', name: 'testEnv1', status: HealthStatus.Online },
  { id: 'test2', name: 'testEnv2', status: HealthStatus.Degraded },
  { id: 'test3', name: 'testEnv3', status: HealthStatus.Offline },
  { id: 'test4', name: 'testEnv4', status: HealthStatus.Unknown }
];

const EnvironmentView = () => {
  const [environments, setEnvironments] = useState<Environment[]>(initialEnvs)
  const [open, setOpen] = useState<string | null>(null);

  useEffect(() => {
    // const sonarClient = createSonarClient();
    // sonarClient.getEnvironments()
    //   .then((res) => {
    //     console.log(res.data);
    //     setEnvironments([...environments, ...res.data]);
    //   })
    //   .catch(e => console.log(`Error fetching environments: ${e.message}`));
  }, []);

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        {environments.map(e => (
          <div className="ds-l-sm-col--6 ds-l-md-col--4" key={e.id} style={{ marginTop: 10, marginBottom: 10 }}>
            <Accordion bordered>
              <EnvironmentItem environment={e}
                               open={open}
                               selected={e.id === open}
                               setOpen={setOpen}
                               statusColor={getHealthStatusIndicator(e.status ? e.status : undefined)} />
            </Accordion>
          </div>
        ))}
      </div>
    </section>
  )
}

export default EnvironmentView;
