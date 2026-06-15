import { ApiUrl, customFetch } from "@/constants/api";
import type { Timesheet, TimesheetEvaluation } from "../../Timesheet";
import { type ApiTimesheetEvaluation, buildTimesheetDraft, mapTimesheetEvaluation } from "./timesheetDraft";

interface ApiAllocationDay {
  coreHours: number;
  projectHours: Record<string, number>;
}

interface ApiAllocation {
  days: ApiAllocationDay[];
  evaluation: ApiTimesheetEvaluation;
}

export interface TimesheetAllocation {
  days: ApiAllocationDay[];
  evaluation: TimesheetEvaluation;
}

export const allocateTimesheet = async (timesheet: Timesheet, day?: number): Promise<TimesheetAllocation> => {
  const query = day ? `?day=${day}` : "";
  const allocation = await customFetch<ApiAllocation>(`${ApiUrl}/timesheets/${timesheet.id}/allocate${query}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildTimesheetDraft(timesheet)),
  });
  return { days: allocation.days, evaluation: mapTimesheetEvaluation(allocation.evaluation) };
};
