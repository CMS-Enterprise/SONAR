import { Spinner } from '@cmsgov/design-system';
import { EnvironmentHealth } from 'api/data-contracts';
import EnvironmentItem from 'components/Environments/EnvironmentItem';
import { createSonarClient } from 'helpers/ApiHelper';
import * as React from 'react';
import { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import AccordionToggleAllButton from 'components/Environments/AccordionToggleAllButton';

const Environments = () => {
  const sonarClient = createSonarClient();
  const [allPanelsOpen, setAllPanelsOpen] = useState<boolean>(true);
  const [openPanels, setOpenPanels] = useState<string[]>([]);
  const { isLoading, isError, data, error } = useQuery<EnvironmentHealth[], Error>(
    ["environments"],
    () =>  sonarClient.getEnvironments()
      .then((res) => {
        return res.data;
      })
  );

  // update open panels when data is refreshed.
  useEffect(() => {
    updateOpenPanels();
  }, [data]);

  // evaluate if all panels are open/closed when openPanels changes.
  useEffect(() => {
    if (openPanels.length === 0) {
      setAllPanelsOpen(false)
    } else if (openPanels.length === data?.length) {
      setAllPanelsOpen(true);
    }
  }, [openPanels]);

  const updateOpenPanels = () => {
    if (data) {
      setOpenPanels(data.map(e => e.environmentName));
    } else {
      setOpenPanels([]);
    }
  }

  const handleToggleAll = () => {
    if (allPanelsOpen) {
      setOpenPanels([]);
    } else {
      updateOpenPanels()
    }
    setAllPanelsOpen(!allPanelsOpen)
  }

  return (
    <section className="ds-l-container">
      <AccordionToggleAllButton allPanelsOpen={allPanelsOpen} handleToggle={handleToggleAll} />
      <div className="ds-l-row">
        {isLoading ? (<Spinner />) :
          data?.map(e => (
            <EnvironmentItem
              key={e.environmentName}
              environment={e}
              openPanels={openPanels}
              setOpenPanels={setOpenPanels}
            />
        ))}
      </div>
    </section>
  )
}

export default Environments;
