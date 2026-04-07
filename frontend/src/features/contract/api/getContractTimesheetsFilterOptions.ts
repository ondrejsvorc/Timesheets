import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface GetContractTimesheetsFilterOptionsResponse {
  years: number[];
  months: number[];
  statuses: string[];
}

export function getContractTimesheetsFilterOptions(contractId: string): Promise<GetContractTimesheetsFilterOptionsResponse> {
  const url = `${ApiUrl}/contracts/${contractId}/timesheets/filter-options`;
  return withOptionalDelay("slow", () => customFetch<GetContractTimesheetsFilterOptionsResponse>(url));
}
