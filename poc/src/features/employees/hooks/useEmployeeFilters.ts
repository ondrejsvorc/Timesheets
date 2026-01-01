import { useMemo, useState } from "react";
import type { EmployeeItem } from "../api/getEmployees";

export interface EmployeesFilterState {
  query: string;
}

const initialFilters: EmployeesFilterState = {
  query: "",
};

const byStartsWith =
  (query: string) =>
  (item: EmployeeItem): boolean =>
    item.fullName.toLowerCase().startsWith(query);

const byIncludes =
  (query: string) =>
  (item: EmployeeItem): boolean =>
    item.fullName.toLowerCase().includes(query);

const applyFilters = (items: EmployeeItem[], filters: EmployeesFilterState): EmployeeItem[] => {
  const base = items;
  const query = filters.query.trim().toLowerCase();
  if (!query) {
    return base;
  }
  const startsWith = base.filter(byStartsWith(query));
  return startsWith.length > 0 ? startsWith : base.filter(byIncludes(query));
};

export const useEmployeeFilters = (items: EmployeeItem[]) => {
  const [filters, setFilters] = useState<EmployeesFilterState>(initialFilters);
  const filtered = useMemo(() => applyFilters(items, filters), [items, filters]);
  return { filters, setFilters, filtered };
};
