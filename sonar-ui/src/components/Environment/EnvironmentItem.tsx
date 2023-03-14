import React from 'react';
import { AccordionItem, Spinner } from "@cmsgov/design-system";
import { useEffect, useState } from "react";
import { Environment } from "../../api/data-contracts";
import { getHealthStatusClass } from "../../helpers/ServiceHierarchyHelper";

const EnvironmentItem: React.FC<{
  environment: Environment,
  open: string | null,
  selected: boolean,
  setOpen: (value: string | null) => void,
  statusColor: string
}> =
  ({environment, open, selected, setOpen, statusColor}) => {
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (selected) {
      // Timeout to mock fetching tenant data.
      // TODO: add api call to tenant endpoint once finished.
      const timer = setTimeout(() => {
        console.log('Fetching env data...!');
        setLoading(false);
      }, 4000);
      return () => clearTimeout(timer);
    }
  }, [selected]);

  const handleToggle = () => {
    let expanded =
      open === environment.id || environment.id === undefined ?
          null : environment.id;
    setOpen(expanded);
  }

  return (
    <AccordionItem
      heading={environment.name}
      isControlledOpen={selected}
      onChange={handleToggle}
      buttonClassName={getHealthStatusClass(environment.status)}
    >
      {loading ? (<Spinner />) : environment.name}
    </AccordionItem>
  );
}
export default EnvironmentItem;
