import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import { Texts } from "@/constants/texts";

export interface ProjectDeleteImpactResponse {
  contractCount: number;
  positionCount: number;
  draftProjectTimesheetCount: number;
  submittedProjectTimesheetCount: number;
  approvedProjectTimesheetCount: number;
  hasProtectedTimesheets: boolean;
  canDelete: boolean;
  canForceDelete: boolean;
}

const formatCount = (template: string, count: number) => template.replace("{count}", String(count));

export const formatDeleteImpactConsequences = (impact: ProjectDeleteImpactResponse): string[] => {
  const consequences: string[] = [];

  if (impact.contractCount > 0) {
    consequences.push(formatCount(Texts.deleteImpactContracts, impact.contractCount));
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

  consequences.push(Texts.deleteImpactAttendancePreserved);

  if (impact.hasProtectedTimesheets) {
    if (impact.canForceDelete) {
      consequences.push(Texts.deleteImpactForceDelete);
    } else {
      consequences.push(Texts.deleteImpactProtectedBlocked);
    }
  }

  return consequences;
};

export const getProjectDeleteImpact = (projectId: string, signal?: AbortSignal) =>
  withOptionalDelay("fast", () => customFetch<ProjectDeleteImpactResponse>(`${ApiUrl}/projects/${projectId}/delete-impact`, { signal }));

export const getProjectContractDeleteImpact = (projectId: string, contractId: string, signal?: AbortSignal) =>
  withOptionalDelay("fast", () =>
    customFetch<ProjectDeleteImpactResponse>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}/delete-impact`, {
      signal,
    }),
  );
