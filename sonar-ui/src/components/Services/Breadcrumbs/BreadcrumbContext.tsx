import { ServiceHierarchyConfiguration } from 'api/data-contracts';
import { createContext } from 'react';

interface BreadcrumbContextType {
  serviceHierarchyConfiguration: ServiceHierarchyConfiguration
}

export const BreadcrumbContext = createContext<BreadcrumbContextType | null | undefined>(null);
