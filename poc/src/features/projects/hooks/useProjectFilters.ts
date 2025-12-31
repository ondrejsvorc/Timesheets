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
  (project: ProjectItem): boolean =>
    !onlyActive || isProjectActive(project);

const byStartsWith =
  (query: string) =>
  (project: ProjectItem): boolean =>
    project.name.toLowerCase().startsWith(query);

const byIncludes =
  (query: string) =>
  (project: ProjectItem): boolean =>
    project.name.toLowerCase().includes(query);

const applyFilters = (projects: ProjectItem[], filters: ProjectsFilterState): ProjectItem[] => {
  const base = projects.filter(byActive(filters.onlyActive));
  const query = filters.query.trim().toLowerCase();
  if (!query) {
    return base;
  }
  const startsWith = base.filter(byStartsWith(query));
  return startsWith.length > 0 ? startsWith : base.filter(byIncludes(query));
};

export const useProjectFilters = (projects: ProjectItem[]) => {
  const [filters, setFilters] = useState<ProjectsFilterState>(initialFilters);
  const filteredProjects = useMemo(() => applyFilters(projects, filters), [projects, filters]);
  return { filters, setFilters, filteredProjects };
};
