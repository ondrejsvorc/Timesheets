export interface TimesheetCommentAuthor {
  id: string;
  name: string;
}

export interface TimesheetStatusChangeDetails {
  changedBy: TimesheetCommentAuthor;
  timesheetLabel: string;
  fromStatus: string | null;
  toStatus: string;
  comment: string | null;
}

export type TimesheetComment =
  | {
      id: string;
      type: "message";
      createdAt: string;
      author: TimesheetCommentAuthor;
      text: string;
    }
  | {
      id: string;
      type: "statusChange";
      createdAt: string;
      statusChange: TimesheetStatusChangeDetails;
    };
