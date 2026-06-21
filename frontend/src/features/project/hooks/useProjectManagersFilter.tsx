import { type FilterCriteria, useFilter } from "@/hooks/useFilter";
import type { ProjectManagerItem } from "../api";

export interface ProjectManagersFilterCriteria extends FilterCriteria {}

const initialFilter: ProjectManagersFilterCriteria = {
  query: "",
};

export const useProjectManagersFilter = (items: ProjectManagerItem[]) =>
  useFilter<ProjectManagerItem, ProjectManagersFilterCriteria>({
    items,
    initialFilter,
    keys: [(item) => item.employeeFullName, (item) => item.employeePersonalNumber],
  });
