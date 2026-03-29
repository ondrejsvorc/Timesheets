import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { ProjectContractManagerItem } from "../api/getProjectContractsManagers";

export interface ContractsManagersFilterCriteria extends FilterCriteria {}

const initialFilter: ContractsManagersFilterCriteria = {
  query: "",
};

export const useContractsManagersFilter = (items: ProjectContractManagerItem[]) =>
  useFilter<ProjectContractManagerItem, ContractsManagersFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.employeeFullName, (item) => item.contractRegistrationNumber],
  });
