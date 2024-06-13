import { Spinner } from '@cmsgov/design-system';
import EnvironmentItem from 'components/Environments/EnvironmentItem';
import * as React from 'react';
import { useCallback, useEffect, useState } from 'react';
import { useUserContext } from 'components/AppContext/AppContextProvider';
import AccordionToggleAllButton from 'components/Environments/AccordionToggleAllButton';
import { EnvironmentHealth } from '../api/data-contracts';
import ThemedFab from '../components/Common/ThemedFab';
import ThemedInlineTooltip from '../components/Common/ThemedInlineTooltip';
import ThemedModalDialog from '../components/Common/ThemedModalDialog';
import CreateEnvironmentForm from '../components/Environments/CreateEnvironmentForm';
import EnvironmentFilterBar from '../components/Environments/EnvironmentFilterBar';
import { useGetEnvironments } from '../components/Environments/Environments.Hooks';
import {useSearchParams} from 'react-router-dom';
import ToggleSwitch from '../components/Environments/ToggleSwitch';
import { ToolTipText } from '../utils/constants';

const Environments = () => {
  const [createEnvOpen, setCreateEnvOpen] = useState(false);
  const [allPanelsOpen, setAllPanelsOpen] = useState<boolean>(true);
  const [openPanels, setOpenPanels] = useState<string[]>([]);
  const { isLoading, data } = useGetEnvironments();
  const [searchParams, setSearchParams] = useSearchParams();
  const [filter, setFilter] = useState(searchParams.get("environmentName") ?? "");
  const [filteredEnvs, setFilteredEnvs] = useState<EnvironmentHealth[]>([]);
  const { userIsAuthenticated, userInfo } = useUserContext();
  const [showNonProdEnvs, setNonProdEnvsFlag] = useState(true);

  // useEffect triggered by user input for filter. Will perform filtering on every keystroke
  // and update search params.
  useEffect(() => {
    if (filter !== "" && data) {
      // split query by space, every element in query should be present in result set.
      const filterArr = filter.split(" ");
      setFilteredEnvs(data.filter(e =>
        filterArr.every(q =>
          e.environmentName.includes(q)
        ))
      );
      setSearchParams({environmentName: filter});
    } else {
      setFilteredEnvs(data ? data : []);
      setSearchParams({});
    }
  }, [data, filter, setSearchParams])

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
      <EnvironmentFilterBar setFilter={setFilter} filter={filter} />

      {(filteredEnvs.length > 0 && (data && data?.length > 0)) ? (
        <div className="ds-l-sm-col--10 ds-u-margin-left--auto ds-u-margin-right--auto ds-u-margin-top--2 ds-u-display--flex ds-u-justify-content--between">
            <ToggleSwitch switchFlag={showNonProdEnvs} setSwitchFlag={setNonProdEnvsFlag} />
            <AccordionToggleAllButton allPanelsOpen={allPanelsOpen} handleToggle={handleToggleAll} />
        </div>
      ) : null}

      <div className="ds-l-row">
        {isLoading ? (<Spinner />) :
          filteredEnvs.map(e => (
            ((showNonProdEnvs) || (e.isNonProd === showNonProdEnvs)) ?
            <EnvironmentItem
              key={e.environmentName}
              environment={e}
              openPanels={openPanels}
              setOpenPanels={setOpenPanels}
            /> : null
        ))}
      </div>
      { (userIsAuthenticated && userInfo?.isAdmin) ?
        <span title='Create New Environment'>
          <ThemedFab action={handleModalToggle} />
        </span> : null}
      {createEnvOpen ? (
        <ThemedModalDialog
          heading={
            <div>
              Add Environment <ThemedInlineTooltip title={ToolTipText.environmentTip} placement={'bottom'} />
            </div>
          }
          onClose={handleModalToggle}
          onExit={handleModalToggle}
          actions={<CreateEnvironmentForm handleModalToggle={handleModalToggle} />}
        />
      ) : null}


    </section>
  )
}

export default Environments;
