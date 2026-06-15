import { ApiUrl, customFetch } from "@/constants/api";
import type { Timesheet, TimesheetEvaluation } from "../../Timesheet";
import { type ApiTimesheetEvaluation, buildTimesheetDraft, mapTimesheetEvaluation } from "./timesheetDraft";

export const reviewTimesheet = async (timesheet: Timesheet, signal?: AbortSignal): Promise<TimesheetEvaluation> => {
  const evaluation = await customFetch<ApiTimesheetEvaluation>(`${ApiUrl}/timesheets/${timesheet.id}/review`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildTimesheetDraft(timesheet)),
    signal,
  });
  return mapTimesheetEvaluation(evaluation);
};
