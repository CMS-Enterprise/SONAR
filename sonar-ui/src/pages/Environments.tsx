import { Spinner } from '@cmsgov/design-system';
import { EnvironmentHealth } from 'api/data-contracts';
import EnvironmentItem from 'components/Environments/EnvironmentItem';
import { useSonarApi } from 'components/SonarApi/Provider';
import * as React from 'react';
import { useCallback, useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import AccordionToggleAllButton from 'components/Environments/AccordionToggleAllButton';
import ThemedFab from '../components/Common/ThemedFab';
import ThemedModalDialog from '../components/Common/ThemedModalDialog';
import CreateEnvironmentForm from '../components/Environments/CreateEnvironmentForm';

const Environments = () => {
  const sonarClient = useSonarApi();
  const [createEnvOpen, setCreateEnvOpen] = useState(false);
  const [allPanelsOpen, setAllPanelsOpen] = useState<boolean>(true);
  const [openPanels, setOpenPanels] = useState<string[]>([]);
  const { isLoading, data } = useQuery<EnvironmentHealth[], Error>(
    ["environments"],
    () =>  sonarClient.getEnvironments()
      .then((res) => {
        return res.data;
      })
  );

  const updateOpenPanels = useCallback(() => {
    if (data) {
      setOpenPanels(data.map(e => e.environmentName));
    } else {
      setOpenPanels([]);
    }
  }, [data]);

  useEffect(() => {
    updateOpenPanels();
  }, [updateOpenPanels]);

  // evaluate if all panels are open/closed when openPanels changes.
  useEffect(() => {
    if (openPanels.length === 0) {
      setAllPanelsOpen(false)
    } else if (openPanels.length === data?.length) {
      setAllPanelsOpen(true);
    }
  }, [data, openPanels]);

  const handleToggleAll = () => {
    if (allPanelsOpen) {
      setOpenPanels([]);
    } else {
      updateOpenPanels()
    }
    setAllPanelsOpen(!allPanelsOpen)
  }

  const handleModalToggle = () => {
    setCreateEnvOpen(!createEnvOpen);
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
      <ThemedFab action={handleModalToggle} />
      {createEnvOpen ? (
        <ThemedModalDialog
          heading={'Add Environment'}
          onClose={handleModalToggle}
          onExit={handleModalToggle}
          actions={<CreateEnvironmentForm handleModalToggle={handleModalToggle} />}
        />
      ) : null}


    </section>
  )
}

export default Environments;
