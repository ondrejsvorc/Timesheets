import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectContractManagerItem } from "./getProjectContractsManagers";

export type AddContractManagerResponse = {
  contractId: string;
  employeeId: string;
  contractRegistrationNumber: string;
  employeePersonalNumber: number;
  employeeFullName: string;
  employeeEmail: string;
};

export const addContractManager = async (contractId: string, employeeId: string, signal: AbortSignal): Promise<AddContractManagerResponse> => {
  return withOptionalDelay("fast", () =>
    customFetch<AddContractManagerResponse>(`${ApiUrl}/contracts/${contractId}/managers`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ contractId, employeeId }),
      signal,
    }),
  );
};

export function toProjectContractManagerItem(response: AddContractManagerResponse): ProjectContractManagerItem {
  return {
    contractId: response.contractId,
    employeeId: response.employeeId,
    contractRegistrationNumber: response.contractRegistrationNumber,
    employeePersonalNumber: response.employeePersonalNumber,
    employeeFullName: response.employeeFullName,
    employeeEmail: response.employeeEmail,
  };
}
