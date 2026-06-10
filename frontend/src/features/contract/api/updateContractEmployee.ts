import { ApiUrl, customFetch } from "@/constants/api";

export interface UpdateContractEmployeeRequest {
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export interface UpdateContractEmployeeResponse {
  id: string;
  contractId: string;
  employeeId: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export const updateContractEmployee = async (
  contractId: string,
  contractEmployeeId: string,
  request: UpdateContractEmployeeRequest,
  signal?: AbortSignal,
) =>
  customFetch<UpdateContractEmployeeResponse>(`${ApiUrl}/contracts/${contractId}/employees/${contractEmployeeId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
    signal,
  });
