import { Texts } from "@/constants/texts";

const formatCount = (template: string, count: number) => template.replace("{count}", String(count));

/** Fields shared by project and contract delete-impact API responses. */
export interface TimesheetDeleteImpactCounts {
  positionCount: number;
  draftProjectTimesheetCount: number;
  submittedProjectTimesheetCount: number;
  approvedProjectTimesheetCount: number;
  submittedAttendanceTimesheetCount: number;
  approvedAttendanceTimesheetCount: number;
  hasProtectedTimesheets: boolean;
  canDelete: boolean;
}

/** Builds the standard delete-impact consequence lines; callers add entity-specific lines first. */
export const appendTimesheetDeleteImpactConsequences = (consequences: string[], impact: TimesheetDeleteImpactCounts, options?: { contractCount?: number }): void => {
  if (options?.contractCount !== undefined && options.contractCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactContracts, options.contractCount));
  }

  if (impact.positionCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactPositions, impact.positionCount));
  }

  if (impact.draftProjectTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactProjectTimesheetsDraft, impact.draftProjectTimesheetCount));
  }

  if (impact.submittedProjectTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactProjectTimesheetsSubmitted, impact.submittedProjectTimesheetCount));
  }

  if (impact.approvedProjectTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactProjectTimesheetsApproved, impact.approvedProjectTimesheetCount));
  }

  if (impact.submittedAttendanceTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactAttendanceTimesheetsSubmitted, impact.submittedAttendanceTimesheetCount));
  }

  if (impact.approvedAttendanceTimesheetCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactAttendanceTimesheetsApproved, impact.approvedAttendanceTimesheetCount));
  }

  consequences.push(Texts.deleteImpactAttendancePreserved);

  if (impact.hasProtectedTimesheets) {
    consequences.push(Texts.deleteImpactProtectedBlocked);
  }
};

export const canConfirmDelete = (impact: { canDelete: boolean }) => impact.canDelete;
