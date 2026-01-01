import { useMemo, useState } from "react";
import type { ProjectItem } from "../api/shared/projectItem";
import { isProjectActive } from "../utils/isProjectActive";

export interface ProjectsFilterState {
  query: string;
  onlyActive: boolean;
}

const initialFilters: ProjectsFilterState = {
  query: "",
  onlyActive: true,
};

const byActive =
  (onlyActive: boolean) =>
  (item: ProjectItem): boolean =>
    !onlyActive || isProjectActive(item);

const byStartsWith =
  (query: string) =>
  (item: ProjectItem): boolean =>
    item.name.toLowerCase().startsWith(query);

const byIncludes =
  (query: string) =>
  (item: ProjectItem): boolean =>
    item.name.toLowerCase().includes(query);

const applyFilters = (items: ProjectItem[], filters: ProjectsFilterState): ProjectItem[] => {
  const base = items.filter(byActive(filters.onlyActive));
  const query = filters.query.trim().toLowerCase();
  if (!query) {
    return base;
  }
  const startsWith = base.filter(byStartsWith(query));
  return startsWith.length > 0 ? startsWith : base.filter(byIncludes(query));
};

export const useProjectFilters = (items: ProjectItem[]) => {
  const [filters, setFilters] = useState<ProjectsFilterState>(initialFilters);
  const filtered = useMemo(() => applyFilters(items, filters), [items, filters]);
  return { filters, setFilters, filtered };
};
