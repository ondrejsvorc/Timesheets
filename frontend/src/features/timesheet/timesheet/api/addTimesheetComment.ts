import { ApiUrl, customFetch } from "@/constants/api";
import type { TimesheetComment } from "../comments/Comment";
import type { TimesheetCommentItem } from "./getTimesheetComments";

export interface AddTimesheetCommentRequest {
  employeeId: string;
  year: number;
  month: number;
  text: string;
}

export const addTimesheetComment = async (request: AddTimesheetCommentRequest, signal?: AbortSignal): Promise<TimesheetComment> => {
  const response = await customFetch<TimesheetCommentItem>(`${ApiUrl}/timesheets/combined/comments`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
    signal,
  });

  if (response.type !== "message" || !response.author || !response.text) {
    throw new Error("Unexpected comment response.");
  }

  return {
    id: response.id,
    type: "message",
    createdAt: response.createdAt,
    text: response.text,
    author: {
      id: response.author.id,
      name: response.author.name,
    },
  };
};
