import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { TimesheetComment, TimesheetCommentAuthorRole } from "../comments/Comment";

export interface TimesheetCommentAuthor {
  name: string;
  role: TimesheetCommentAuthorRole;
}

export interface TimesheetStatusChangeDetails {
  changedBy: TimesheetCommentAuthor;
  timesheetLabel: string;
  fromStatus: string | null;
  toStatus: string;
  comment: string | null;
}

export interface TimesheetCommentItem {
  id: string;
  type: "message" | "statusChange";
  createdAt: string;
  text: string | null;
  author: TimesheetCommentAuthor | null;
  statusChange: TimesheetStatusChangeDetails | null;
}

const mapComment = (item: TimesheetCommentItem): TimesheetComment => {
  if (item.type === "statusChange") {
    if (!item.statusChange) {
      throw new Error("Status change comment is missing details.");
    }

    return {
      id: item.id,
      type: "statusChange",
      createdAt: item.createdAt,
      statusChange: item.statusChange,
    };
  }

  if (!item.author || !item.text) {
    throw new Error("Message comment is missing author or text.");
  }

  return {
    id: item.id,
    type: "message",
    createdAt: item.createdAt,
    text: item.text,
    author: {
      name: item.author.name,
      role: item.author.role,
    },
  };
};

export const getTimesheetComments = (employeeId: string, year: number, month: number) => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return {
    promise: withOptionalDelay("fast", async () => {
      const response = await customFetch<TimesheetCommentItem[]>(`${ApiUrl}/timesheets/combined/comments?${params.toString()}`);
      return response.map(mapComment);
    }),
  };
};
