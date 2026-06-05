import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface EmployeePositionItem {
  projectId: string;
  projectName: string;
  contractId: string;
  contractRegistrationNumber: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export interface GetEmployeePositionsResponse {
  employeeId: string;
  positions: EmployeePositionItem[];
}

export const getEmployeePositions = (employeeId: string) => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetEmployeePositionsResponse>(`${ApiUrl}/employees/${employeeId}/positions`)),
  };
};
