import * as React from 'react';
import { useEffect, useState } from 'react';
import { Accordion } from '@cmsgov/design-system';

import { EnvironmentHealth, HealthStatus } from 'api/data-contracts';
import EnvironmentItem from 'components/Environment/EnvironmentItem';
import { getHealthStatusIndicator } from 'helpers/ServiceHierarchyHelper';
import { createSonarClient } from 'helpers/ApiHelper';


const EnvironmentView = () => {
  const [environments, setEnvironments] = useState<EnvironmentHealth[] | null>(null);
  const [open, setOpen] = useState<string | null>(null);

  useEffect(() => {
    const sonarClient = createSonarClient();
    sonarClient.getEnvironments()
      .then((res) => {
        console.log(res.data);
        setEnvironments(res.data);
      })
      .catch(e => console.log(`Error fetching environments: ${e.message}`));
  }, []);

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        {environments?.map(e => (
          <div className="ds-l-sm-col--6 ds-l-md-col--4" key={e.environmentName} style={{ marginTop: 10, marginBottom: 10 }}>
            <Accordion bordered>
              <EnvironmentItem environment={e}
                               open={open}
                               selected={e.environmentName === open}
                               setOpen={setOpen}
                               statusColor={getHealthStatusIndicator(e.aggregateStatus ? e.aggregateStatus : undefined)} />
            </Accordion>
          </div>
        ))}
      </div>
    </section>
  )
}

export default EnvironmentView;
