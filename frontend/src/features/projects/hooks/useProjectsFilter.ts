import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { ProjectItem } from "../api";
import { getProjectStatus } from "../utils/getProjectStatus";

export type ProjectStatusFilter = "active" | "archived" | "all";

export interface ProjectsFilterCriteria extends FilterCriteria {
  status: ProjectStatusFilter;
}

const initialFilter: ProjectsFilterCriteria = {
  query: "",
  status: "active",
};

export const useProjectsFilter = (items: ProjectItem[]) =>
  useFilter<ProjectItem, ProjectsFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.name, (item) => item.registrationNumber],
    predicates: [
      (item, filter) => {
        if (filter.status === "all") {
          return true;
        }
        return getProjectStatus(item) === filter.status;
      },
    ],
  });
