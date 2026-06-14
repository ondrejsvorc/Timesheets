import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { Texts } from "@/constants/texts";
import { formatDate } from "@/utils/formatDate";
import type { UpdateContractEmployeeRequest } from "./updateContractEmployee";

export interface ContractEmployeeUpdateImpactResponse {
  canUpdate: boolean;
  createsNewAssignment: boolean;
  blockReason: string | null;
  currentAssignmentEndDate: string | null;
  newAssignmentStartDate: string | null;
  newTimesheetMonthCount: number;
  draftTimesheetsOnOldAssignment: number;
  draftDaysToRemove: number;
  submittedTimesheetCount: number;
  approvedTimesheetCount: number;
}

const formatCount = (template: string, count: number) => template.replace("{count}", String(count));

export const formatUpdateImpactConsequences = (impact: ContractEmployeeUpdateImpactResponse): string[] => {
  if (!impact.canUpdate) {
    return impact.blockReason ? [impact.blockReason] : [Texts.updateImpactBlocked];
  }

  const consequences: string[] = [];

  if (impact.createsNewAssignment) {
    if (impact.currentAssignmentEndDate) {
      consequences.push(Texts.updateImpactEndOld.replace("{date}", formatDate(impact.currentAssignmentEndDate)));
    }

    if (impact.newAssignmentStartDate) {
      consequences.push(Texts.updateImpactStartNew.replace("{date}", formatDate(impact.newAssignmentStartDate)));
    }
  } else if (impact.currentAssignmentEndDate) {
    consequences.push(Texts.updateImpactNewEnd.replace("{date}", formatDate(impact.currentAssignmentEndDate)));
  }

  if (impact.newTimesheetMonthCount > 0) {
    consequences.push(formatCount(Texts.updateImpactNewMonths, impact.newTimesheetMonthCount));
  }

  if (impact.draftDaysToRemove > 0) {
    consequences.push(formatCount(Texts.updateImpactDraftDaysRemove, impact.draftDaysToRemove));
  }

  if (impact.draftTimesheetsOnOldAssignment > 0) {
    consequences.push(formatCount(Texts.updateImpactDraftsOnOld, impact.draftTimesheetsOnOldAssignment));
  }

  if (impact.submittedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactSubmitted, impact.submittedTimesheetCount));
  }

  if (impact.approvedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactApproved, impact.approvedTimesheetCount));
  }

  return consequences;
};

export const getContractEmployeeUpdateImpact = (
  contractId: string,
  contractEmployeeId: string,
  request: UpdateContractEmployeeRequest,
  signal?: AbortSignal,
) =>
  withDelay("fast", () =>
    customFetch<ContractEmployeeUpdateImpactResponse>(`${ApiUrl}/contracts/${contractId}/employees/${contractEmployeeId}/update-impact`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    }),
  );
