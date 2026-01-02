import { useMemo, useState } from "react";
import type { ProjectContractItem } from "../api/getProjectContracts";

export interface ContractsFilterState {
  query: string;
}

const initialFilters: ContractsFilterState = {
  query: "",
};

const byStartsWith =
  (query: string) =>
  (item: ProjectContractItem): boolean =>
    item.name.toLowerCase().startsWith(query);

const byIncludes =
  (query: string) =>
  (item: ProjectContractItem): boolean =>
    item.name.toLowerCase().includes(query);

const applyFilters = (items: ProjectContractItem[], filters: ContractsFilterState): ProjectContractItem[] => {
  const base = items;
  const query = filters.query.trim().toLowerCase();
  if (!query) {
    return base;
  }
  const startsWith = base.filter(byStartsWith(query));
  return startsWith.length > 0 ? startsWith : base.filter(byIncludes(query));
};

export const useContractFilters = (items: ProjectContractItem[]) => {
  const [filters, setFilters] = useState<ContractsFilterState>(initialFilters);
  const filtered = useMemo(() => applyFilters(items, filters), [items, filters]);
  return { filters, setFilters, filtered };
};
