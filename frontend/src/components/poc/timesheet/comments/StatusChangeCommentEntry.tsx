import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { Texts } from "@/constants/texts";
import type { TimesheetCommentAuthorRole, TimesheetStatusChangeDetails } from "./Comment";

const roleLabel = (role: TimesheetCommentAuthorRole) => {
  switch (role) {
    case "Employee":
      return Texts.roleEmployee;
    case "Manager":
      return Texts.roleManager;
    case "Controller":
      return Texts.roleController;
    default:
      return role;
  }
};

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
        <div className="text-sm font-medium text-foreground">
          {changedBy.name}
          <span className="ml-2 text-xs font-normal text-muted-foreground">({roleLabel(changedBy.role)})</span>
        </div>
        <div className="text-xs text-muted-foreground tabular-nums">{new Date(createdAt).toLocaleString("cs-CZ")}</div>
      </div>

      <div className="mt-2 space-y-2 text-sm leading-6">
        <div className="flex flex-wrap items-center gap-x-2 gap-y-1.5">
          <span className="text-muted-foreground">{Texts.statusChangeLabel}</span>
          <span className="font-medium text-foreground">{timesheetLabel}</span>
          {fromStatus && fromStatus !== toStatus ? (
            <>
              <TimesheetStatusBadge status={fromStatus} />
              <span className="text-muted-foreground" aria-hidden>
                →
              </span>
              <TimesheetStatusBadge status={toStatus} />
            </>
          ) : (
            <TimesheetStatusBadge status={toStatus} />
          )}
        </div>

        {trimmedComment && (
          <p className="text-foreground/90 whitespace-pre-wrap">
            <span className="text-muted-foreground">{Texts.comment}: </span>
            {trimmedComment}
          </p>
        )}
      </div>
    </div>
  );
};
