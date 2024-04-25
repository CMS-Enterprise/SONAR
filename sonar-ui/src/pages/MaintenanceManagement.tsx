import { TabPanel } from '@cmsgov/design-system';
import React from 'react';
import { pageTitleStyle, parentContainerStyle } from '../App.Style';
import ThemedTabs from '../components/Common/ThemedTabs';
import AdHocMaintenanceTable from '../components/MaintenanceManagement/AdHocMaintenanceTable';
import ScheduledMaintenanceTable from '../components/MaintenanceManagement/ScheduledMaintenanceTable';


const MaintenanceManagement = () => {

  return (
    <section className="ds-l-container" css={parentContainerStyle}>
      <div className="ds-l-row ds-u-margin-bottom--3 ds-u-justify-content--end">
        <div
          className="ds-l-col--4 ds-u-margin-right--auto ds-u-margin-left--0"
          css={pageTitleStyle}
        >
          Maintenance Management
        </div>
      </div>
      <ThemedTabs>
        <TabPanel id="adhoc" key="adhoc" tab="Ad-hoc">
          <AdHocMaintenanceTable/>
        </TabPanel>
        <TabPanel id="scheduled" key="scheduled" tab="Scheduled">
          <ScheduledMaintenanceTable/>
        </TabPanel>
      </ThemedTabs>
    </section>
)
}

export default MaintenanceManagement;
