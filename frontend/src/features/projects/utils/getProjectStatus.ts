import { isAfter, isSameDay, parseISO, startOfDay } from "date-fns";
import type { ProjectItem } from "../api/shared/projectItem";

export type ProjectStatus = "archived" | "active" | "inactive";

export const getProjectStatus = (project: ProjectItem): ProjectStatus => {
  if (project.archivedAt) {
    return "archived";
  }

  if (!project.endDate) {
    return "active";
  }

  const end = startOfDay(parseISO(project.endDate));
  const today = startOfDay(new Date());
  return isSameDay(end, today) || isAfter(end, today) ? "active" : "inactive";
};
