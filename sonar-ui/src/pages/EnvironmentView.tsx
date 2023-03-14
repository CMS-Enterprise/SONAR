import * as React from 'react';
import { createSonarClient } from "../helpers/ApiHelper";
import { Environment, HealthStatus } from "../api/data-contracts";
import { Accordion } from "@cmsgov/design-system";
import { AccordionItem }  from "@cmsgov/design-system";
import { useEffect, useState } from "react";
import EnvironmentItem from "../components/Environment/EnvironmentItem";
import { getHealthStatusIndicator } from "../helpers/ServiceHierarchyHelper";

const initialEnvs: Environment[] = [
  {id: "test1", name: "testEnv1", status: HealthStatus.Online},
  {id: "test2", name: "testEnv2", status: HealthStatus.Degraded},
  {id: "test3", name: "testEnv3", status: HealthStatus.Offline}
];

const EnvironmentView = () => {
  const [environments, setEnvironments] = useState<Environment[]>(initialEnvs)
  const [open, setOpen] = useState<string | null>(null);
  const sonarClient = createSonarClient();

  useEffect(() => {
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
              <div className="ds-l-sm-col--6 ds-l-md-col--4" key={e.id} style={{ marginTop: 10, marginBottom: 10}}>
                <Accordion bordered>
                  <EnvironmentItem environment={e} open={open} selected={e.id === open} setOpen={setOpen} statusColor={getHealthStatusIndicator(e.status ? e.status : undefined)} />
                </Accordion>
              </div>
            ))}
      </div>
    </section>
  )
}

export default EnvironmentView;
