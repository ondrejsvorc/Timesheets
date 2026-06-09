import { Texts } from "@/constants/texts";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";

export const areAllProjectsApproved = (overview: GetCombinedTimesheetOverviewResponse): boolean => {
  const projects = overview.items.filter((item) => item.kind === "project");
  if (projects.length === 0) {
    return true;
  }
  return projects.every((item) => item.status === Texts.statusApproved);
};
