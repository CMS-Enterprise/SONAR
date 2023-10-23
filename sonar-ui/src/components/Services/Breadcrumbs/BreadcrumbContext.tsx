import { ServiceHierarchyConfiguration } from 'api/data-contracts';
import { createContext } from 'react';

interface BreadcrumbContextType {
  serviceHierarchyConfiguration: ServiceHierarchyConfiguration | null,
  errorReportsCount: number
}

export const BreadcrumbContext = createContext<BreadcrumbContextType | null | undefined>(null);
