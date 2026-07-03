import type { TimesheetStatusChangeDetails } from "./Comment";
import { describeStatusChange, formatCommentDateTime } from "./commentActivity";

interface StatusChangeCommentEntryProps {
  createdAt: string;
  statusChange: TimesheetStatusChangeDetails;
}

export const StatusChangeCommentEntry = ({ createdAt, statusChange }: StatusChangeCommentEntryProps) => {
  const { changedBy, timesheetLabel, fromStatus, toStatus, comment } = statusChange;
  const trimmedComment = comment?.trim() ?? "";

  return (
    <div className="rounded-md border bg-muted/40 px-3 py-3 md:px-4 md:py-3.5">
      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
        <div className="text-sm font-medium text-foreground">{changedBy.name}</div>
        <div className="text-xs text-muted-foreground tabular-nums">{formatCommentDateTime(createdAt)}</div>
      </div>
      <p className="mt-2 text-sm text-foreground">{describeStatusChange(fromStatus, toStatus, timesheetLabel)}</p>
      {trimmedComment && <p className="mt-2 border-l-2 pl-3 text-sm text-muted-foreground whitespace-pre-wrap">{trimmedComment}</p>}
    </div>
  );
};
