import { ApiUrl, customFetch } from "@/constants/api";

export interface DeleteTimesheetCommentRequest {
  commentId: string;
  employeeId: string;
  year: number;
  month: number;
}

export const deleteTimesheetComment = async (request: DeleteTimesheetCommentRequest, signal?: AbortSignal) => {
  const params = new URLSearchParams({
    employeeId: request.employeeId,
    year: String(request.year),
    month: String(request.month),
  });

  await customFetch<void>(`${ApiUrl}/timesheets/combined/comments/${request.commentId}?${params.toString()}`, {
    method: "DELETE",
    signal,
  });
};
