export type TimesheetCommentAuthorRole = "Employee" | "Manager" | "Controller";

export interface TimesheetCommentAuthor {
  name: string;
  role: TimesheetCommentAuthorRole;
}

export type TimesheetComment =
  | {
      id: string;
      type: "message";
      createdAt: string; // ISO
      author: TimesheetCommentAuthor;
      text: string;
    }
  | {
      id: string;
      type: "system";
      createdAt: string; // ISO
      text: string;
    };

