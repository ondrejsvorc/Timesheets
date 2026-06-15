import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { appendTimesheetDeleteImpactConsequences } from "@/utils/deleteImpactConsequences";

export interface DeleteProjectImpactResponse {
  contractCount: number;
  positionCount: number;
  draftProjectTimesheetCount: number;
  submittedProjectTimesheetCount: number;
  approvedProjectTimesheetCount: number;
  hasProtectedTimesheets: boolean;
  canDelete: boolean;
}

export const formatProjectDeleteImpactConsequences = (impact: DeleteProjectImpactResponse): string[] => {
  const consequences: string[] = [];
  appendTimesheetDeleteImpactConsequences(consequences, impact, { contractCount: impact.contractCount });
  return consequences;
};

export const getProjectDeleteImpact = (projectId: string, signal?: AbortSignal) =>
  withDelay("fast", () => customFetch<DeleteProjectImpactResponse>(`${ApiUrl}/projects/${projectId}/delete-impact`, { signal }));
