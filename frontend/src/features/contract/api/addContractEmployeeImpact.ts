import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import { Texts } from "@/constants/texts";

export interface AddContractEmployeeImpactRequest {
  employeeId: string;
  startDate: string;
  endDate?: string | null;
}

export interface AddContractEmployeeImpactResponse {
  canAdd: boolean;
  blockReason: string | null;
  submittedTimesheetCount: number;
  approvedTimesheetCount: number;
}

const formatCount = (template: string, count: number) => template.replace("{count}", String(count));

export const formatAddImpactConsequences = (impact: AddContractEmployeeImpactResponse): string[] => {
  if (!impact.canAdd) {
    return impact.blockReason ? [impact.blockReason] : [Texts.addImpactBlocked];
  }

  const consequences: string[] = [];
  if (impact.submittedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactSubmitted, impact.submittedTimesheetCount));
  }
  if (impact.approvedTimesheetCount > 0) {
    consequences.push(formatCount(Texts.updateImpactApproved, impact.approvedTimesheetCount));
  }

  return consequences.length > 0 ? consequences : [Texts.addImpactOk];
};

export const getAddContractEmployeeImpact = (contractId: string, request: AddContractEmployeeImpactRequest, signal?: AbortSignal) =>
  withDelay("fast", () =>
    customFetch<AddContractEmployeeImpactResponse>(`${ApiUrl}/contracts/${contractId}/employees/add-impact`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    }),
  );
