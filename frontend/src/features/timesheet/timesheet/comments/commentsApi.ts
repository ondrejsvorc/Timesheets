import { addTimesheetComment as addTimesheetCommentRequest } from "../api/addTimesheetComment";
import { getTimesheetComments } from "../api/getTimesheetComments";
import type { TimesheetComment } from "./Comment";

export interface TimesheetCommentsScope {
  employeeId: string;
  year: number;
  month: number;
}

export const listTimesheetComments = async (scope: TimesheetCommentsScope, signal?: AbortSignal): Promise<TimesheetComment[]> => {
  const comments = await getTimesheetComments(scope.employeeId, scope.year, scope.month);
  if (signal?.aborted) {
    throw new DOMException("Aborted", "AbortError");
  }
  return comments;
};

export const addTimesheetComment = async (
  scope: TimesheetCommentsScope,
  input: { text: string },
  signal?: AbortSignal,
): Promise<TimesheetComment> => {
  return addTimesheetCommentRequest(
    {
      employeeId: scope.employeeId,
      year: scope.year,
      month: scope.month,
      text: input.text,
    },
    signal,
  );
};
