import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { ProjectItem } from "../api/shared/projectItem";
import { isProjectActive } from "../utils/isProjectActive";

export interface ProjectsFilterCriteria extends FilterCriteria {
  onlyActive: boolean;
  onlyArchived: boolean;
}

const initialFilter: ProjectsFilterCriteria = {
  query: "",
  onlyActive: true,
  onlyArchived: false,
};

export const useProjectsFilter = (items: ProjectItem[]) =>
  useFilter<ProjectItem, ProjectsFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.name, (item) => item.registrationNumber],
    predicates: [
      (item, filter) => {
        if (filter.onlyArchived) {
          return Boolean(item.archivedAt);
        }
        if (filter.onlyActive) {
          return isProjectActive(item);
        }
        return true;
      },
    ],
  });
