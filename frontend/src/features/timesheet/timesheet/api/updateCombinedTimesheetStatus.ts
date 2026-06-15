import { ApiUrl, customFetch } from "@/constants/api";

export type TimesheetStatusAction = "submit" | "approve" | "return";

export interface UpdateCombinedTimesheetStatusRequest {
  employeeId: string;
  year: number;
  month: number;
  action: TimesheetStatusAction;
  comment?: string | null;
  timesheetIds: string[];
}

export const updateCombinedTimesheetStatus = async (request: UpdateCombinedTimesheetStatusRequest, signal?: AbortSignal) => {
  await customFetch<void>(`${ApiUrl}/timesheets/combined/status`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      employeeId: request.employeeId,
      year: request.year,
      month: request.month,
      action: request.action,
      comment: request.comment?.trim() ? request.comment.trim() : null,
      timesheetIds: request.timesheetIds,
    }),
    signal,
  });
};
