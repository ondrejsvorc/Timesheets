import { isAfter, isSameDay, parseISO, startOfDay } from "date-fns";
import type { ProjectItem } from "../api/shared/projectItem";

export const isProjectActive = (project: ProjectItem): boolean => {
  if (!project.endDate) {
    return true;
  }
  const end = startOfDay(parseISO(project.endDate));
  const today = startOfDay(new Date());
  return isSameDay(end, today) || isAfter(end, today);
};
