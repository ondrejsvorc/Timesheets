import { useMemo, useState } from "react";
import type { EmployeePositionItem } from "../api/getEmployeePositions";

export interface PositionFilterState {
  query: string;
}

const initialFilters: PositionFilterState = {
  query: "",
};

const byStartsWith =
  (query: string) =>
  (item: EmployeePositionItem): boolean =>
    item.position.toLowerCase().startsWith(query);

const byIncludes =
  (query: string) =>
  (item: EmployeePositionItem): boolean =>
    item.position.toLowerCase().includes(query);

const applyFilters = (items: EmployeePositionItem[], filters: PositionFilterState): EmployeePositionItem[] => {
  const base = items;
  const query = filters.query.trim().toLowerCase();
  if (!query) {
    return base;
  }
  const startsWith = base.filter(byStartsWith(query));
  return startsWith.length > 0 ? startsWith : base.filter(byIncludes(query));
};

export const usePositionFilters = (items: EmployeePositionItem[]) => {
  const [filters, setFilters] = useState<PositionFilterState>(initialFilters);
  const filtered = useMemo(() => applyFilters(items, filters), [items, filters]);
  return { filters, setFilters, filtered };
};
