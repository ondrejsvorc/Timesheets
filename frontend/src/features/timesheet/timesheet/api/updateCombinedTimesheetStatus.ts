import { ApiUrl, customFetch } from "@/constants/api";

export interface UpdateCombinedTimesheetStatusRequest {
  employeeId: string;
  year: number;
  month: number;
  statusId: string;
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
      statusId: request.statusId,
      comment: request.comment?.trim() ? request.comment.trim() : null,
      timesheetIds: request.timesheetIds,
    }),
    signal,
  });
};
