import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { appendTimesheetDeleteImpactConsequences } from "@/utils/deleteImpactConsequences";

export interface DeleteContractImpactResponse {
  positionCount: number;
  draftProjectTimesheetCount: number;
  submittedProjectTimesheetCount: number;
  approvedProjectTimesheetCount: number;
  submittedAttendanceTimesheetCount: number;
  approvedAttendanceTimesheetCount: number;
  hasProtectedTimesheets: boolean;
  canDelete: boolean;
}

export const formatContractDeleteImpactConsequences = (impact: DeleteContractImpactResponse): string[] => {
  const consequences: string[] = [];
  appendTimesheetDeleteImpactConsequences(consequences, impact);
  return consequences;
};

export const getContractDeleteImpact = (contractId: string, signal?: AbortSignal) =>
  withDelay("fast", () => customFetch<DeleteContractImpactResponse>(`${ApiUrl}/contracts/${contractId}/delete-impact`, { signal }));
