import * as React from 'react';
import { useState } from 'react';
import { Spinner } from '@cmsgov/design-system';
import { EnvironmentHealth } from 'api/data-contracts';
import EnvironmentItem from 'components/Environments/EnvironmentItem';
import { getHealthStatusIndicator } from 'helpers/ServiceHierarchyHelper';
import { createSonarClient } from 'helpers/ApiHelper';
import { useQuery } from 'react-query';


const Environments = () => {
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
            <EnvironmentItem
              key={e.environmentName}
              environment={e}
              open={open}
              selected={e.environmentName === open}
              setOpen={setOpen}
              statusColor={getHealthStatusIndicator(e.aggregateStatus ? e.aggregateStatus : undefined)} />
        ))}
      </div>
    </section>
  )
}

export default Environments;
