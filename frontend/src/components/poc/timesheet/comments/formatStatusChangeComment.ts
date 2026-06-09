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

const applyTemplate = (template: string, values: Record<string, string>) =>
  Object.entries(values).reduce((result, [key, value]) => result.replace(`{${key}}`, value), template);

export const formatStatusChangeComment = (statusChange: TimesheetStatusChangeDetails): string => {
  const { changedBy, timesheetLabel, fromStatus, toStatus, comment } = statusChange;
  const who = `${changedBy.name} (${roleLabel(changedBy.role)})`;
  const trimmedComment = comment?.trim() ?? "";

  if (fromStatus === toStatus) {
    if (trimmedComment) {
      return `${who}: ${applyTemplate(Texts.statusChangeCommentToTimesheet, {
        timesheet: timesheetLabel,
        status: toStatus,
        comment: trimmedComment,
      })}`;
    }

    return `${who}: ${applyTemplate(Texts.statusChangeNoteToTimesheet, {
      timesheet: timesheetLabel,
      status: toStatus,
    })}`;
  }

  const transition = fromStatus
    ? applyTemplate(Texts.statusChangeTransition, {
        timesheet: timesheetLabel,
        fromStatus,
        toStatus,
      })
    : applyTemplate(Texts.statusChangeSet, {
        timesheet: timesheetLabel,
        toStatus,
      });

  let message = `${who}: ${transition}`;
  if (trimmedComment) {
    message += ` ${applyTemplate(Texts.statusChangeCommentSuffix, { comment: trimmedComment })}`;
  }

  return message;
};
