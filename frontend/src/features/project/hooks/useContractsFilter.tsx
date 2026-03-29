import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { ProjectContractItem } from "../api/shared/projectContractItem";

export interface ContractsFilterCriteria extends FilterCriteria {}

const initialFilter: ContractsFilterCriteria = {
  query: "",
};

export const useContractsFilter = (items: ProjectContractItem[]) =>
  useFilter<ProjectContractItem, ContractsFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.name, (item) => item.registrationNumber],
  });
