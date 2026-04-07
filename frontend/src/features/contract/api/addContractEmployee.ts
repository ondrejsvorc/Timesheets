import { ApiUrl, customFetch } from "@/constants/api";

export interface AddContractEmployeeRequest {
  employeeId: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate?: string | null;
}

export interface AddContractEmployeeResponse {
  contractId: string;
  employeeId: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
  personalNumber: number;
  fullName: string;
  employeeTypeId: string | null;
}

export const addContractEmployee = async (contractId: string, request: AddContractEmployeeRequest, signal?: AbortSignal) => {
  return await customFetch<AddContractEmployeeResponse>(`${ApiUrl}/contracts/${contractId}/employees`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
    signal,
  });
};
