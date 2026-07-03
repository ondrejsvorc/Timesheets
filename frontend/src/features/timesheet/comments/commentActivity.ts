import { format, parseISO } from "date-fns";
import { Texts } from "@/constants/texts";

export const formatCommentDateTime = (iso: string) => format(parseISO(iso), "d. M. HH:mm");

const fill = (template: string, values: Record<string, string>) => Object.entries(values).reduce((text, [key, value]) => text.replace(`{${key}}`, value), template);

export const describeStatusChange = (fromStatus: string | null, toStatus: string, timesheetLabel: string) => {
  if (!fromStatus || fromStatus === toStatus) {
    return fill(Texts.statusChangeSet, { timesheet: timesheetLabel, toStatus });
  }

  if (fromStatus === Texts.statusInProgress && toStatus === Texts.statusPendingApproval) {
    return fill(Texts.commentActivitySubmitted, { timesheet: timesheetLabel });
  }

  if (fromStatus === Texts.statusPendingApproval && toStatus === Texts.statusApproved) {
    return fill(Texts.commentActivityApproved, { timesheet: timesheetLabel });
  }

  if (fromStatus === Texts.statusPendingApproval && toStatus === Texts.statusInProgress) {
    return fill(Texts.commentActivityReturned, { timesheet: timesheetLabel });
  }

  if (fromStatus === Texts.statusApproved && toStatus === Texts.statusInProgress) {
    return fill(Texts.commentActivityUnlocked, { timesheet: timesheetLabel });
  }

  return fill(Texts.statusChangeTransition, { timesheet: timesheetLabel, fromStatus, toStatus });
};
