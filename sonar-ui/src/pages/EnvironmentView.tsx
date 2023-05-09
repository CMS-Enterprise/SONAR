import * as React from 'react';
import { useState } from 'react';
import { Accordion, Spinner } from '@cmsgov/design-system';
import { EnvironmentHealth } from 'api/data-contracts';
import EnvironmentItem from 'components/Environment/EnvironmentItem';
import { getHealthStatusIndicator } from 'helpers/ServiceHierarchyHelper';
import { createSonarClient } from 'helpers/ApiHelper';
import { useQuery } from 'react-query';


const EnvironmentView = () => {
  const sonarClient = createSonarClient();
  const [open, setOpen] = useState<string | null>(null);

  const { isLoading, isError, data, error } = useQuery<EnvironmentHealth[], Error>(
    ["environments"],
    () =>  sonarClient.getEnvironments()
      .then((res) => {
        return res.data;
      })
  );

  return (
    <section className="ds-l-container">
      <div className="ds-l-row">
        {isLoading ? (<Spinner />) :
          data?.map(e => (
            <div className="ds-l-sm-col--6 ds-l-md-col--4" key={e.environmentName} style={{ marginTop: 10, marginBottom: 10 }}>
                <EnvironmentItem environment={e}
                                 open={open}
                                 selected={e.environmentName === open}
                                 setOpen={setOpen}
                                 statusColor={getHealthStatusIndicator(e.aggregateStatus ? e.aggregateStatus : undefined)} />
            </div>
        ))}
      </div>
    </section>
  )
}

export default EnvironmentView;
