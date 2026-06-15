import { ApiUrl, customFetch } from "@/constants/api";
import type { Timesheet, TimesheetEvaluation } from "../../Timesheet";
import { type ApiTimesheetEvaluation, buildTimesheetDraft, mapTimesheetEvaluation } from "./timesheetDraft";

interface UpdateTimesheetResponse {
  id: string;
  evaluation: ApiTimesheetEvaluation;
}

export const updateTimesheet = async (timesheet: Timesheet, signal: AbortSignal): Promise<TimesheetEvaluation> => {
  const response = await customFetch<UpdateTimesheetResponse>(`${ApiUrl}/timesheets/${timesheet.id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(buildTimesheetDraft(timesheet)),
    signal,
  });
  return mapTimesheetEvaluation(response.evaluation);
};
