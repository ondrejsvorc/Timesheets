import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface PositionItem {
  position: string | null;
  workload: number | null;
  startDate: string;
  endDate: string | null;
}

export interface EmployeeItem {
  id: string;
  personalNumber: number;
  fullName: string;
  employeeType: string;
  positions: PositionItem[];
}

export interface GetContractEmployeesResponse {
  employees: EmployeeItem[];
}

export const getContractEmployees = (_projectId: string, contractId: string) => {
  return {
    promise: withOptionalDelay("slow", () =>
      customFetch<GetContractEmployeesResponse>(
        `${ApiUrl}/contracts/${contractId}/employees`,
      ),
    ),
  };
};
