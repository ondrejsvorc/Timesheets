import { useMemo, useState } from "react";
import type { ProjectContractManagerItem } from "../api/getProjectContractsManagers";

export interface ContractsManagersFilterState {
  query: string;
}

const initialFilters: ContractsManagersFilterState = {
  query: "",
};

const byStartsWith =
  (query: string) =>
  (item: ProjectContractManagerItem): boolean =>
    item.contractName.toLowerCase().startsWith(query);

const byIncludes =
  (query: string) =>
  (item: ProjectContractManagerItem): boolean =>
    item.contractName.toLowerCase().includes(query);

const applyFilters = (items: ProjectContractManagerItem[], filters: ContractsManagersFilterState): ProjectContractManagerItem[] => {
  const base = items;
  const query = filters.query.trim().toLowerCase();
  if (!query) {
    return base;
  }
  const startsWith = base.filter(byStartsWith(query));
  return startsWith.length > 0 ? startsWith : base.filter(byIncludes(query));
};

export const useContractsManagersFilters = (items: ProjectContractManagerItem[]) => {
  const [filters, setFilters] = useState<ContractsManagersFilterState>(initialFilters);
  const filtered = useMemo(() => applyFilters(items, filters), [items, filters]);
  return { filters, setFilters, filtered };
};
